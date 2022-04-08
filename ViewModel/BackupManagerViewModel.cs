using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Backup_Manager.Model;
using Backup_Manager.Controls;
using System.Security;
using System.Security.Permissions;
using System.Security.AccessControl;
using System.Windows;
using Backup_Manager.Interface;

namespace Backup_Manager.ViewModel
{
    class BackupManagerViewModel : INotifyPropertyChanged
    {
        public string title = "Backup Manager";
        public string version = "20220408 v1.0.4";
        //public string version = "20181217 v1.0.3";
        private BackupDataModel backupData { get; set; }
        private BackupItemModel selectedBackupItem { get; set; }
        private string statusList { get; set; }
        private Action<string> StatusListChange { get; set; }
        private Action<double> ProgressUpdate { get; set; }
        private string statusLogPath { get; set; }
        private double progress { get; set; }

        public BackupDataModel BackupData
        {
            get { return backupData; }
            set { backupData = value; RaisePropertyChanged("BackupData"); }
        }
        public BackupItemModel SelectedBackupItem
        {
            get { return selectedBackupItem; }
            set
            {
                if (selectedBackupItem == value) return;
                selectedBackupItem = value;
                iProgress.Report(0);
                RaisePropertyChanged("SelectedBackupItem");
                RaisePropertyChanged("ItemIsSelected");
            }
        }
        public string StatusList
        {
            get { return statusList; }
            set
            {
                try
                {
                    string line = DateTime.Now.ToString("HH:mm:ss: ") + value + System.Environment.NewLine;                    
                    statusList = statusList.Insert(0, line);
                    RaisePropertyChanged("StatusList");
                }
                catch(Exception exc)
                {
                    ToolsLib.Tools.ExceptionLogAndShow(exc, "StatusList SET");
                }
            }
        }
        public IProgress<double> iProgress { get; set; }
        public double Progress
        {
            get { return progress; }
            set
            {
                if(progress!=value)
                {
                    progress = value;
                    RaisePropertyChanged("Progress");
                }
            }
        }
        

        public CommandBase RunCommand { get; set; }
        public CommandBase RunAllCommand { get; set; }
        public CommandBase CheckCommand { get; set; }
        public CommandBase CheckAllCommand { get; set; }

        public CommandBase AddCommand { get; set; }
        public CommandBase EditCommand { get; set; }
        public CommandBase CloneCommand { get; set; }
        public CommandBase DeleteCommand { get; set; }

        public CommandBase OpenDefaultDirectoryCommand { get; set; }
        public CommandBase OpenChangelogCommand { get; set; }
        public CommandBase AboutWindowCommand { get; set; }
        public CommandBase ExitCommand { get; set; }

        public bool ItemIsSelected
        {
            get
            {
                return SelectedBackupItem == null ? false : true;
            }
        }
        public bool ListNotEmpty
        {
            get
            {
                if(BackupData!=null && BackupData.BackupItems!=null)
                {
                    if(BackupData.BackupItems.Count>0)
                    {
                        return true;
                    }
                }               
               return false;                
            }
        }

        #region StartUp
        public BackupManagerViewModel()
        {
            CheckConfigSetup();
            statusList = string.Empty;
            StatusListChange = new Action<string>((string x) => { this.StatusList = x; });
            ProgressUpdate = new Action<double>((double d) => { this.Progress=d; });
            iProgress = new Progress<double>(ProgressUpdate);
            BackupData = new BackupDataModel();

            RunCommand = new CommandBase(Run);
            RunAllCommand = new CommandBase(RunAll);
            CheckCommand = new CommandBase(Check);
            CheckAllCommand = new CommandBase(CheckAll);

            AddCommand = new CommandBase(Add);
            EditCommand = new CommandBase(Edit);
            CloneCommand = new CommandBase(Clone);
            DeleteCommand = new CommandBase(Delete);

            OpenDefaultDirectoryCommand = new CommandBase(OpenDefaultDirectory);
            OpenChangelogCommand = new CommandBase(OpenChangelog);
            AboutWindowCommand = new CommandBase(AboutWindow);
            ExitCommand = new CommandBase(Exit);

            LoadBackupItems();
            StatusList = "Program started";
        }
        private void CheckConfigSetup()
        {
            try
            {
                string dataPath = ToolsLib.Tools.ReadAppSettingPath("defaultDataDirectory");
                if (dataPath == null)
                {
                    ToolsLib.Tools.WriteAppSetting("defaultDataDirectory", "Data\\");
                    dataPath = ToolsLib.Tools.ReadAppSettingPath("defaultDataDirectory");
                }
                string errorPath = System.IO.Path.Combine(dataPath, "ErrorLogs\\");
                string statusPath = System.IO.Path.Combine(dataPath, "StatusLogs\\");
                if (ToolsLib.Tools.ReadAppSettingPath("errorLogPath") == null || !System.IO.Directory.Exists(ToolsLib.Tools.ReadAppSettingPath("errorLogPath")))
                    ToolsLib.Tools.WriteAppSetting("errorLogPath", errorPath);
                else
                    errorPath = ToolsLib.Tools.ReadAppSettingPath("errorLogPath");

                if (!System.IO.Directory.Exists(dataPath))
                    System.IO.Directory.CreateDirectory(dataPath);
                if (!System.IO.Directory.Exists(errorPath))
                    System.IO.Directory.CreateDirectory(errorPath);
                if (!System.IO.Directory.Exists(statusPath))
                    System.IO.Directory.CreateDirectory(statusPath);

                statusLogPath = System.IO.Path.Combine(statusPath, DateTime.Now.ToString("yyyyMMdd") + "_log.txt");
            }
            catch(Exception exc)
            {
                MessageBox.Show(exc.Message);
            }            
        }        
        #endregion
        public void SaveLogsToFile()
        {
            try
            {
                SaveBackupItems();
                System.IO.File.AppendAllText(statusLogPath, statusList);                
            }
            catch(Exception exc)
            {
                ToolsLib.Tools.ExceptionLogAndShow(exc, "SaveLogsToFile");
            }
        }
             
        private async void Run()
        {
            if (!ItemIsSelected) { StatusList = "Item not selected"; return; }
            bool result = false;
            result = await BackupExecutorDoBackup(SelectedBackupItem);
            if (result)
            {
                SelectedBackupItem.LastBackup = DateTime.Now;
                RaisePropertyChanged("SelectedBackupItem");
                StatusList = "Backup ends with success";
            }
            else
            {
                StatusList = "Backup ends with errors";
            }
        }
        private void RunAll()
        {
            StatusList = "Function not set yet";
        }
        private async void Check()
        {
            if (!ItemIsSelected) { StatusList = "Item not selected"; return; }
            await BackupExecutorCheck(SelectedBackupItem);
        }
        private async void CheckAll()
        {
            if (BackupData == null || BackupData.BackupItems == null || BackupData.BackupItems.Count == 0) return;
            StatusList = "CheckAll started";
            foreach(var item in BackupData.BackupItems)
            {
                SelectedBackupItem = item;
                await BackupExecutorCheck(SelectedBackupItem);
            }
            SelectedBackupItem = null;
            StatusList = "CheckAll ended";
            SaveBackupItems();
        }
        private void Add()
        {
            var window = new AddEditWindow_v2();
            if(window.model.ReturnCommand == "Ok")
            {
                BackupData.BackupItems.Add(window.model.SelectedItem);
                StatusList = window.model.SelectedItem.Name + " was added to list";
                RaisePropertyChanged("BackupData");
            }
        }
        private void Edit()
        {
            var window = new AddEditWindow_v2(SelectedBackupItem);
            if(window.model.ReturnCommand == "Ok")
            {
                SelectedBackupItem.FromOtherItem(window.model.SelectedItem);
                RaisePropertyChanged("SelectedBackupItem");
                StatusList = SelectedBackupItem.Name + " was edited";
            }            
        }
        private void Clone()
        {
            if (SelectedBackupItem == null) return;
            string name = SelectedBackupItem.Name + "(cloned)";
            BackupItemModel item = new BackupItemModel();
            item.FromOtherItem(SelectedBackupItem);
            item.Name = name;
            BackupData.BackupItems.Add(item);
            StatusList = item.Name + " was added to list";
            RaisePropertyChanged("BackupData");
        }
        private void Delete()
        {
            if (SelectedBackupItem == null) return;
            string name = SelectedBackupItem.Name;
            BackupData.BackupItems.Remove(SelectedBackupItem);
            StatusList = name + " deleted";            
        }
        
        private void LoadBackupItems()
        {
            var path = System.IO.Path.Combine(ToolsLib.Tools.ReadAppSettingPath("defaultDataDirectory"), "default.xml");
            var tmp = new BackupDataModel();
            try
            {                
                tmp = ToolsLib.Tools.Deserialize<BackupDataModel>(path);    
                if(tmp == null)
                {
                    tmp = new BackupDataModel();
                    tmp.BackupItems = new System.Collections.ObjectModel.ObservableCollection<BackupItemModel>();
                    ToolsLib.Tools.Serialize(tmp, path);
                }
            }
            catch(Exception)
            {
                tmp = new BackupDataModel
                {
                    BackupItems = new System.Collections.ObjectModel.ObservableCollection<BackupItemModel>()
                };
                ToolsLib.Tools.Serialize(tmp, path);
            }
            BackupData = tmp;
        }
        private void SaveBackupItems()
        {
            try
            {
                var path = System.IO.Path.Combine(ToolsLib.Tools.ReadAppSettingPath("defaultDataDirectory"), "default.xml");
                ToolsLib.Tools.Serialize(BackupData, path);
            }
            catch(Exception exc)
            {
                ToolsLib.Tools.ExceptionLogAndShow(exc, "SaveBackupItems");
            }
        }
        private void OpenDefaultDirectory()
        {
            string dir = ToolsLib.Tools.ReadAppSettingPath("defaultDataDirectory");
            System.Diagnostics.Process.Start(dir);
        }
        private void OpenChangelog()
        {
            if(System.IO.File.Exists("Changelog.txt"))
            {
                System.Diagnostics.Process.Start("Changelog.txt");
            }
        }
        private void AboutWindow()
        {
            var aboutWindow = new ToolsLib.Wpf.AboutWindow(title, version, "Application for backup your data to other disk or ftp server");
        }
        private void Exit()
        {
            App.Current.MainWindow.Close();
        }

        private async Task<bool> BackupExecutorCheck(BackupItemModel item)
        {
            try
            {
                return await Task.Run(() => 
                {
                    string typeName = "Backup_Manager.Executor.Backup" + item.ItemType.ToString() + "Executor";
                    Type backupType = Type.GetType(typeName);
                    object backupInstance = Activator.CreateInstance(backupType);
                    IBackupExecutor backupExecutor = backupInstance as IBackupExecutor;
                    return backupExecutor.Check(item, StatusListChange);
                });
            }
            catch (Exception exc)
            {
                ToolsLib.Tools.ExceptionLogAndShow(exc, "DiskTypeCheck");
                return false;
            }
        }
        
        private async Task<bool> BackupExecutorDoBackup(BackupItemModel item)
        {
            try
            {
                return await Task.Run(() => 
                {
                    string typeName = "Backup_Manager.Executor.Backup" + item.ItemType.ToString() + "Executor";
                    Type backupType = Type.GetType(typeName);
                    object backupInstance = Activator.CreateInstance(backupType);
                    IBackupExecutor backupExecutor = backupInstance as IBackupExecutor;
                    return backupExecutor.DoBackup(item, StatusListChange, ProgressUpdate);
                });
            }
            catch (Exception exc)
            {
                ToolsLib.Tools.ExceptionLogAndShow(exc, "DiskTypeDoBackup");
                return false;
            }
        }
        
              
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
