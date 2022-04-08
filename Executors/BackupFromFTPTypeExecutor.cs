using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Backup_Manager.Model;
using System.Threading.Tasks;
using System.IO;
using Backup_Manager.Interface;

namespace Backup_Manager.Executor
{
    public class BackupFromFTPTypeExecutor : IBackupExecutor
    {
        FluentFTP.FtpClient ftpClient { get; set; }
        static int counter = 0;
        public bool Check(BackupItemModel item, Action<string> StatusList)
        {
            try
            {
                bool result = true;
                StatusList("FromFTPTypeCheck started on item name = " + item.Name);

                if (!PathCheck.FTPTypePath(item.DataPath, item.FTPConnectionData, StatusList))
                {
                    result = false;
                }
                if (!PathCheck.DiskTypePath(item.BackupPath, StatusList,"BackupPath"))
                {
                    result = false;
                }

                if (result)
                {
                    ftpClient = new FluentFTP.FtpClient(item.FTPConnectionData.Address, item.FTPConnectionData.Username, item.FTPConnectionData.Password);
                    ftpClient.Connect();
                    List<BackupPair> list = new List<BackupPair>();
                    if (!MakeList(item, list, StatusList,true))
                    {
                        StatusList("Can't make list of files to copy");
                        ftpClient.Disconnect();
                        return false;
                    }
                    if (list.Count > 0)
                    {
                        StatusList(list.Count.ToString() + " files ready for copy");
                    }
                    else
                    {
                        StatusList("No files to copy.");
                    }
                    ftpClient.Disconnect();
                    StatusList("Check result OK");
                    return true;
                }
                else
                {
                    StatusList("Check result - FAIL!!!");
                    return false;
                }
            }
            catch(Exception exc)
            {
                ToolsLib.Tools.ExceptionLogAndShow(exc, "FromFtpTypeExecutorCheck");
                return false;
            }
        }
        public bool DoBackup(BackupItemModel backupItem, Action<string> StatusList, Action<double> ProgressUpdate)
        {
            if (backupItem == null)
            {
                StatusList("Can't backup null?!");
                return false;
            }
            StatusList("Start backup for: " + backupItem.Name);
            StatusList("Data path: " + backupItem.DataPath);
            StatusList("Backup path: " + backupItem.BackupPath);
            if (!Check(backupItem, new Action<string>((string s) => { })))
            {
                StatusList("Check Error");
                return false;
            }

            ftpClient = new FluentFTP.FtpClient(backupItem.FTPConnectionData.Address, backupItem.FTPConnectionData.Username, backupItem.FTPConnectionData.Password);
            ftpClient.Connect();

            List<BackupPair> list = new List<BackupPair>();
            if (!MakeList(backupItem, list, StatusList,false))
            {
                StatusList("Can't make list of files to copy");
                ftpClient.Disconnect();
                return false;
            }
            if (list.Count > 0)
            {
                StatusList(list.Count.ToString() + " files ready for copy");
            }
            else
            {
                StatusList("No files to copy.");
                ftpClient.Disconnect();
                return true;
            }
            if (!FromFTPCopyFiles(list, StatusList, ProgressUpdate))
            {
                ftpClient.Disconnect();
                return false;
            }
            ftpClient.Disconnect();
            return true;
        }
        private bool MakeList(BackupItemModel backupItem, List<BackupPair> list, Action<string> StatusList,bool checkMode)
        {
            if (backupItem == null) return false;
            #region data_dir
            if (!ftpClient.DirectoryExists(backupItem.DataPath))
            {
                StatusList("Error: Directory " + backupItem + " Not exists.");
                return false;
            }
            var fold = ftpClient.GetListing(backupItem.DataPath);
            string[] Folders = fold.Where(x => x.Type == FluentFTP.FtpFileSystemObjectType.Directory).Select(z => z.FullName).ToArray();
            foreach (var f in Folders)
            {
                BackupItemModel tmpItem = new BackupItemModel();
                string folder = f.Substring(f.LastIndexOf(Path.AltDirectorySeparatorChar) + 1, f.Length - f.LastIndexOf(Path.AltDirectorySeparatorChar) - 1);
                if (folder.StartsWith(".")) continue;//dla ignorowania folderow typu '.git'
                tmpItem.Replace = backupItem.Replace;
                tmpItem.DataPath = f;
                tmpItem.BackupPath = Path.Combine(backupItem.BackupPath, folder);
                if (!MakeList(tmpItem, list, StatusList,checkMode))
                    return false;
            }
            #endregion
            #region backup_dir
            //backup directory section
            if (!Directory.Exists(backupItem.BackupPath))
            {
                try
                {
                    if (checkMode)
                    {
                        var daittmp = ftpClient.GetListing(backupItem.DataPath);
                        string[] DataItemstmp = daittmp.Where(x => x.Type == FluentFTP.FtpFileSystemObjectType.File).Select(z => z.FullName).ToArray();
                        list.AddRange(DataItemstmp.Select(x => new BackupPair(x, "checkMode", backupItem.Replace)));
                        counter += DataItemstmp.Length;
                        return true;
                    }
                    Directory.CreateDirectory(backupItem.BackupPath);
                }
                catch (Exception exc)
                {
                    ToolsLib.Tools.ExceptionLogAndShow(exc, "MakeList()");
                    return false;
                }
            }
            #endregion
            #region files
            var dait = ftpClient.GetListing(backupItem.DataPath);
            string[] DataItems = dait.Where(x => x.Type == FluentFTP.FtpFileSystemObjectType.File).Select(z => z.FullName).ToArray();
            string[] BackupItems = Directory.GetFiles(backupItem.BackupPath);
            foreach (var DItem in DataItems)
            {
                DateTime lastModifyTimeFile = dait.FirstOrDefault(x => Path.GetFileName(x.FullName).Equals(Path.GetFileName(DItem))).Modified.ToUniversalTime();
                var DItemName = Path.GetFileName(DItem);
                if (BackupItems.Any(x => Path.GetFileName(x).Equals(DItemName)))
                {
                    string destfile = BackupItems.FirstOrDefault(x => Path.GetFileName(x).Equals(DItemName));
                    DateTime lastModifyTimeCopy = File.GetLastWriteTime(destfile).ToUniversalTime();
                    if (lastModifyTimeFile > lastModifyTimeCopy) // file is newer
                    {
                        if (backupItem.Replace)
                        {
                            list.Add(new BackupPair(DItem, destfile, true));
                            counter++;
                        }
                        else
                        {
                            destfile = Path.Combine(Path.GetDirectoryName(destfile), (Guid.NewGuid().ToString().Substring(0, 8) + DItemName));
                            list.Add(new BackupPair(DItem, destfile, false));
                            counter++;
                        }
                    }
                }
                else
                {
                    list.Add(new BackupPair(DItem, Path.Combine(backupItem.BackupPath, Path.GetFileName(DItem)), backupItem.Replace));
                    counter++;
                }
            }
            #endregion
            return true;
        }
        private bool FromFTPCopyFiles(List<BackupPair> list, Action<string> StatusList, Action<double> ProgressUpdate)
        {
            try
            {
                double total = list.Count;
                double current = 0;
                foreach (var pair in list)
                {
                    try
                    {
                        current++;
                        ftpClient.DownloadFile(pair.BackupPath, pair.DataPath, pair.Replace);
                        string fileSize = PathCheck.GetSizeReadable(ftpClient.GetFileSize(pair.DataPath));
                        StatusList("File: " + Path.GetFileName(pair.DataPath) + " downloaded, size: "+fileSize+" (" + ((int)current).ToString() + "/" + ((int)total).ToString() + ")");
                    }
                    catch (Exception) { StatusList("Can't download: " + Path.GetFileName(pair.DataPath)); }

                    ProgressUpdate(((current * 100) / total));
                }
                return true;
            }
            catch (Exception exc)
            {
                ToolsLib.Tools.ExceptionLogAndShow(exc, "FromFTPCopyFiles");
                return false;
            }
        }
    }
}
