using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using Backup_Manager.Model;
using Backup_Manager.Interface;

namespace Backup_Manager.Executor
{
    public class BackupDiskTypeExecutor : IBackupExecutor
    {
        static int counter = 0;

        public bool Check(BackupItemModel item,Action<string> StatusList)
        {
            StatusList("DiskTypeCheck started on item name = " + item.Name);
            bool result = true;

            if(!PathCheck.DiskTypePath(item.DataPath,StatusList,"DataPath"))
            {
                result = false;
            }
            if(!PathCheck.DiskTypePath(item.BackupPath,StatusList,"BackupPath"))
            {
                result = false;
            }

            if (result)
            {
                StatusList("Check result OK");
                List<BackupPair> list = new List<BackupPair>();
                if (!MakeList(item, list, StatusList,true))
                {
                    StatusList("Can't make list of files to copy");
                    return false;
                }
                if (list.Count > 0)
                {
                    StatusList(list.Count.ToString() + " files ready for copy");
                }
                else
                {
                    StatusList("No files to copy.");
                    return true;
                }
                return true;
            }
            else
            {
                StatusList("Check result - FAIL!!!");
                return false;
            }
        }               

        public bool DoBackup(BackupItemModel backupItem, Action<string> StatusList,Action<double> ProgressUpdate)
        {
            if (backupItem == null)
            {
                StatusList("Can't backup null?!");
                return false;
            }
            StatusList("Start backup for: " + backupItem.Name);
            StatusList("Data path: " + backupItem.DataPath);
            StatusList("Backup path: " + backupItem.BackupPath);
            if (!Check(backupItem,new Action<string>((string s)=> { })))
            {
                StatusList("Check Error");
                return false;
            }

            List<BackupPair> list = new List<BackupPair>();
            if(!MakeList(backupItem,list,StatusList,false))
            {
                StatusList("Can't make list of files to copy");
                return false;
            }
            if (list.Count > 0)
            {
                StatusList(list.Count.ToString() + " files ready for copy");
            }
            else
            {
                StatusList("No files to copy.");
                return true;
            }
            if (!DiskCopyFiles(list, StatusList, ProgressUpdate))
                return false;

            return true;
        }
        private bool MakeList(BackupItemModel backupItem, List<BackupPair> list, Action<string> StatusList,bool checkMode)
        {
            if (backupItem == null) return false;
            #region data_dir
            if (!Directory.Exists(backupItem.DataPath))
            {
                StatusList("Error: Directory " + backupItem + " Not exists.");
                return false;
            }
            string[] Folders = Directory.GetDirectories(backupItem.DataPath);
            foreach (var f in Folders)
            {
                BackupItemModel tmpItem = new BackupItemModel();
                string folder = f.Substring(f.LastIndexOf(Path.DirectorySeparatorChar) + 1, f.Length - f.LastIndexOf(Path.DirectorySeparatorChar) - 1);
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
                if (checkMode)
                {
                    string[] DataItemstmp = Directory.GetFiles(backupItem.DataPath);
                    list.AddRange(DataItemstmp.Select(x => new BackupPair(x, "checkMode", backupItem.Replace)));
                    counter += DataItemstmp.Length;
                    return true;
                }
                try
                {
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
            string[] DataItems = Directory.GetFiles(backupItem.DataPath);
            string[] BackupItems = Directory.GetFiles(backupItem.BackupPath);
            foreach (var DItem in DataItems)
            {
                DateTime lastModifyTimeFile = File.GetLastWriteTime(DItem);
                var DItemName = Path.GetFileName(DItem);
                if (BackupItems.Any(x => Path.GetFileName(x).Equals(DItemName)))
                {
                    string destfile = BackupItems.First(x => Path.GetFileName(x).Equals(DItemName));
                    DateTime lastModifyTimeCopy = File.GetLastWriteTime(destfile);
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
                    list.Add(new BackupPair(DItem, Path.Combine(backupItem.BackupPath, Path.GetFileName(DItem)), false));
                    counter++;
                }
            }
            #endregion
            return true;
        }
        private bool DiskCopyFiles(List<BackupPair> list, Action<string> StatusList, Action<double> ProgressUpdate)
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
                        File.Copy(pair.DataPath, pair.BackupPath, pair.Replace);
                        StatusList("File: " + Path.GetFileName(pair.DataPath) + " copied ("+((int)current).ToString()+"/"+((int)total).ToString()+")");
                    }
                    catch (Exception) { StatusList("Can't copy: " + Path.GetFileName(pair.DataPath)); }                                       
                    
                    ProgressUpdate(((current * 100) / total));
                }
                return true;
            }
            catch (Exception exc)
            {
                ToolsLib.Tools.ExceptionLogAndShow(exc, "DiskCopyFiles");
                return false;
            }
        }
    }
}
