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

namespace Backup_Manager.ViewModel
{
    public class AddEditViewModel : INotifyPropertyChanged
    {
        BackupItemModel selectedItem { get; set; }
        string title { get; set; }

        public BackupItemModel SelectedItem
        {
            get { return selectedItem; }
            set
            {
                if (value != selectedItem)
                {
                    selectedItem = value;
                    RaisePropertyChanged("SelectedItem");
                }
            }
        }
        public string Title
        {
            get { return title; }
            set
            {
                if(value!=title)
                {
                    title = value;
                    RaisePropertyChanged("Title");
                }
            }
        }        
        public bool IsFtp
        {
            get
            {
                if (SelectedItem.ItemType.ToString().ToLower().Contains("ftp"))
                    return true;
                else
                    return false;
            }
        }

        public string ReturnCommand { get; set; }

        public IEnumerable<BackupItemTypeModel> ItemTypes
        {
            get
            {
                return Enum.GetValues(typeof(BackupItemTypeModel)).Cast<BackupItemTypeModel>();
            }
        }
        public BackupItemTypeModel ItemType
        {
            get { return SelectedItem.ItemType; }
            set { if(value != SelectedItem.ItemType)
                {
                    SelectedItem.ItemType = value;
                    RaisePropertyChanged("ItemType");
                    RaisePropertyChanged("IsFtp");
                }
            }
        }

        public CommandBase OkCommand { get; set; }
        public CommandBase CancelCommand { get; set; }

        public AddEditViewModel(Action action)
        {
            Title = "Add";
            SelectedItem = new BackupItemModel();
            SelectedItem.FTPConnectionData = new FTPConnectionDataModel();
            SetUpCommands();
            exit = action;
        }
        public AddEditViewModel(Action action,BackupItemModel item)
        {
            exit = action;
            if(item != null)
            {
                SelectedItem = new BackupItemModel();
                SelectedItem.FromOtherItem(item);
                if(SelectedItem.FTPConnectionData==null)
                {
                    SelectedItem.FTPConnectionData = new FTPConnectionDataModel();
                }
                Title = "Edit";
                SetUpCommands();
            }
            else
            {
                exit.Invoke();
            }
            
        }

        private Action exit { get; set; }

        private void SetUpCommands()
        {
            OkCommand = new CommandBase(Ok);
            CancelCommand = new CommandBase(Cancel);
        }

        private void Ok()
        {
            ReturnCommand = "Ok";
            exit.Invoke();
        }
        private void Cancel()
        {
            ReturnCommand = "Cancel";
            exit.Invoke();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
