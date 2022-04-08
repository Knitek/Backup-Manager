using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backup_Manager.Model
{
    public class BackupDataModel : INotifyPropertyChanged
    {
        private ObservableCollection<BackupItemModel> backupItems { get; set; }

        public ObservableCollection<BackupItemModel> BackupItems
        {
            get { return backupItems; }
            set
            {
                backupItems = value;
                RaisePropertyChanged("BackupItems");
            }
        }

        public BackupDataModel()
        {
            BackupItems = new ObservableCollection<BackupItemModel>();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }
    }
}
