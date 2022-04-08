﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using Backup_Manager.Model;
using Backup_Manager.Interface;

namespace Backup_Manager.Executor
{
    public class BackupZipToDiskTypeExecutor : IBackupExecutor
    {
        public bool Check(BackupItemModel item, Action<string> StatusList)
        {
            try
            {
                bool result = true;
                StatusList("FromFTPTypeCheck started on item name = " + item.Name);

                if (!PathCheck.DiskTypePath(item.DataPath, StatusList, "DataPath"))
                {
                    result = false;
                }

                if (!PathCheck.DiskTypePath(Path.GetDirectoryName(item.BackupPath), StatusList, "BackupPath"))
                {
                    result = false;
                }
                else if(!Path.GetFileName(item.BackupPath).EndsWith(".zip"))
                {
                    StatusList("Error in Backup Path! Please set a filename ended with '.zip'");
                    return false;
                }

                if (result)
                {
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
                ToolsLib.Tools.ExceptionLogAndShow(exc, "ZipToFTP_Check");
                return false;
            }
        }
        public bool DoBackup(BackupItemModel source, Action<string> StatusList, Action<double> ProgressUpdate)
        {
            BackupItemModel backupItem = new BackupItemModel();
            backupItem.FromOtherItem(source);

            #region CHECK
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
            #endregion
            #region ZIP_DIR
            string file = ZipDirectory(backupItem, StatusList);
            if (string.IsNullOrWhiteSpace(file))
            {
                StatusList("Error while try zip directory");
                return false;
            }
            #endregion       

            

            IProgress<double> ProgressUpd = new Progress<double>((double d) => { ProgressUpdate(d); });
            #region COPY
            string destFile = Path.Combine(Path.GetDirectoryName(backupItem.BackupPath), Path.GetFileName(file));
            File.Copy(file,destFile);
            if(!File.Exists(destFile))
            {
                StatusList("Error while copy file");
                DeleteTMP(file, StatusList);
                return false;
            }
            #endregion
            #region DeleteTEMP
            DeleteTMP(file,StatusList);
            #endregion

            return true;
        }
        private string ZipDirectory(BackupItemModel item, Action<string> StatusList)
        {
            string outDir = ToolsLib.Tools.ReadAppSettingPath("defaultDataDirectory");
            string fileName = Path.GetFileName(item.BackupPath);
            if (!item.Replace)
            {
                fileName = fileName.Insert(0, Guid.NewGuid().ToString().Substring(0, 8)+"_");
                item.BackupPath = Path.Combine(Path.GetDirectoryName(item.BackupPath), fileName);
            }
            string fileOut = Path.Combine(outDir, fileName);

            ZipFile.CreateFromDirectory(item.DataPath, fileOut);
            if (File.Exists(fileOut))
            {
                StatusList(fileName + " was temporary created.");
                return fileOut;                
            }
            return string.Empty;
        }
        private bool DeleteTMP(string file,Action<string> StatusList)
        {
            if (File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                    if(File.Exists(file)==false)
                    {
                        StatusList("Succesfuly deleted TEMPfile");
                        return true;
                    }
                    else
                    {
                        StatusList("Error: TEMPfile not deleted");
                        return false;
                    }
                }
                catch(Exception exc)
                {
                    StatusList("Exception: " + exc.Message);
                    return false;
                }
            }
            else
            {
                StatusList("Error can't deleteTEMP file. It run away!");
                return false;
            }
        }
    }
}
