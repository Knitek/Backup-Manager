using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Backup_Manager.Model;
using Backup_Manager.Interface;

namespace Backup_Manager.Executor
{
    class BackupToFTPTypeExecutor : IBackupExecutor
    {
        FluentFTP.FtpClient ftpClient { get; set; }
        static int counter = 0;
        public  bool Check(BackupItemModel item, Action<string> StatusList)
        {
            try
            {
                bool result = true;
                StatusList("FromFTPTypeCheck started on item name = " + item.Name);

                if (!PathCheck.DiskTypePath(item.DataPath, StatusList, "DataPath"))
                {
                    result = false;
                }

                if (!PathCheck.FTPTypePath(item.BackupPath, item.FTPConnectionData, StatusList))
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
            catch (Exception exc)
            {
                ToolsLib.Tools.ExceptionLogAndShow(exc, "ToFtpTypeExecutorCheck");
                return false;
            }
        }
        public  bool DoBackup(BackupItemModel backupItem, Action<string> StatusList, Action<double> ProgressUpdate)
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

            if (!ToFTPCopyFiles(list, StatusList, ProgressUpdate))
            {
                ftpClient.Disconnect();
                return false;
            }
            ftpClient.Disconnect();
            return true;
        }
        private  bool MakeList(BackupItemModel backupItem, List<BackupPair> list, Action<string> StatusList,bool checkMode)
        {
            if (backupItem == null) return false;
            #region data_dir
            if (!Directory.Exists(backupItem.DataPath))
            {
                StatusList("Error: Directory " + backupItem + " Not exists.");
                return false;
            }
            //var fold = ftpClient.GetListing(backupItem.BackupPath);
            string[] Folders = Directory.GetDirectories(backupItem.DataPath);
            foreach (var f in Folders)
            {
                BackupItemModel tmpItem = new BackupItemModel();
                string folder = f.Substring(f.LastIndexOf(Path.DirectorySeparatorChar) + 1, f.Length - f.LastIndexOf(Path.DirectorySeparatorChar) - 1);
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
            if (!ftpClient.DirectoryExists(backupItem.BackupPath))
            {
                if(checkMode)
                {
                    string[] DataItemstmp = Directory.GetFiles(backupItem.DataPath);
                    list.AddRange(DataItemstmp.Select(x => new BackupPair(x, "checkMode", backupItem.Replace)));
                    counter += DataItemstmp.Length;
                    return true;
                }
                try
                {
                    ftpClient.CreateDirectory(backupItem.BackupPath);
                }
                catch (Exception exc)
                {
                    ToolsLib.Tools.ExceptionLogAndShow(exc, "MakeList()");
                    return false;
                }
            }
            #endregion
            #region files
            var bait = ftpClient.GetListing(backupItem.BackupPath);
            // = dait.Where(x => x.Type == FluentFTP.FtpFileSystemObjectType.File).Select(z => z.FullName).ToArray();
            string[] DataItems = Directory.GetFiles(backupItem.DataPath);            
            string[] BackupItems = bait.Where(x => x.Type == FluentFTP.FtpFileSystemObjectType.File).Select(z => z.FullName).ToArray();
            foreach (var DItem in DataItems)
            {
                DateTime lastModifyTimeFile = File.GetLastWriteTime(DItem).ToUniversalTime();
                    //dait.FirstOrDefault(x => x.FullName.Contains(Path.GetFileName(DItem))).Modified;
                var DItemName = Path.GetFileName(DItem);
                if (BackupItems.Any(x => Path.GetFileName(x).Equals(DItemName)))
                {
                    string destfile = BackupItems.First(x => Path.GetFileName(x).Equals(DItemName));
                    DateTime lastModifyTimeCopy = bait.FirstOrDefault(x=>x.Name.Equals(DItemName)).Modified.ToUniversalTime();

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
        private  bool ToFTPCopyFiles(List<BackupPair> list, Action<string> StatusList, Action<double> ProgressUpdate)
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
                        if (pair.Replace)
                            ftpClient.UploadFile(pair.DataPath, pair.BackupPath, FluentFTP.FtpExists.Overwrite);
                        else
                        {
                            pair.BackupPath = Path.Combine(Path.GetDirectoryName(pair.BackupPath), (Guid.NewGuid().ToString().Substring(0, 6) + Path.GetFileName(pair.BackupPath)));
                            ftpClient.UploadFile(pair.DataPath, pair.BackupPath, FluentFTP.FtpExists.Overwrite);
                        }
                        string fileSize = PathCheck.GetSizeReadable((new FileInfo(pair.DataPath)).Length);
                        StatusList("File: " + Path.GetFileName(pair.DataPath) + " uploaded, size: "+fileSize+" (" + ((int)current).ToString() + "/" + ((int)total).ToString() + ")");
                    }
                    catch (Exception) { StatusList("Can't upload: " + Path.GetFileName(pair.DataPath)); }

                    ProgressUpdate(((current * 100) / total));
                }
                return true;
            }
            catch (Exception exc)
            {
                ToolsLib.Tools.ExceptionLogAndShow(exc, "ToFTPCopyFiles");
                return false;
            }
        }
    }
}
