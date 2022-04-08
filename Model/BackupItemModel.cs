using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Backup_Manager.Model
{
    public class BackupItemModel : INotifyPropertyChanged
    {
        DateTime lastBackup { get; set; }
        BackupItemTypeModel itemType { get; set; }
        string name { get; set; }

        public string Name
        {
            get { return name; }
            set
            {
                if (value != name)
                {
                    name = value;
                    RaisePropertyChanged("Name");
                }
            }
        }
        public string DataPath { get; set; }
        public string BackupPath { get; set; }        
        public bool Replace { get; set; }
        public BackupItemTypeModel ItemType
        {
            get { return itemType; }
            set
            {
                if (itemType != value)
                {
                    itemType = value;
                    RaisePropertyChanged("ItemType");
                }
            }
        }
        
        public FTPConnectionDataModel FTPConnectionData { get; set; }
        public DateTime LastBackup
        {
            get { return lastBackup; }
            set { lastBackup = value; RaisePropertyChanged("LastBackup"); }
        }

        public void FromOtherItem(BackupItemModel n)
        {
            Name = n.Name;
            DataPath = n.DataPath;
            BackupPath = n.BackupPath;
            Replace = n.Replace;
            LastBackup = n.LastBackup;
            ItemType = n.ItemType;
            if (n.FTPConnectionData != null)
            {
                var f = new FTPConnectionDataModel();
                f.Address = n.FTPConnectionData?.Address;
                f.Username = n.FTPConnectionData?.Username;
                f.Password = n.FTPConnectionData?.Password;
                f.Port = n.FTPConnectionData?.Port ?? 0;
                FTPConnectionData = f;
            }
        }

        [XmlIgnore]
        public string Description
        {
            get
            {
                string tmp = "DataPath: " + DataPath + "\r\nBackupPath: " + BackupPath + "\r\nReplace: " + Replace.ToString();
                if (ItemType == BackupItemTypeModel.ToFTPType && FTPConnectionData != null)
                    tmp += "\r\nFTPAddress: " + FTPConnectionData.Address + "\r\nFTPUsername: " + FTPConnectionData.Username;
                return tmp;
            }
        }

        public bool Equals(BackupItemModel obj)
        {
            if (obj == null) return true;
            if (this.Name.Equals(obj.Name) &&
                this.DataPath.Equals(obj.DataPath) &&
                this.BackupPath.Equals(obj.BackupPath) &&
                this.Replace.Equals(obj.Replace))
                return true;
            else
                return false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
