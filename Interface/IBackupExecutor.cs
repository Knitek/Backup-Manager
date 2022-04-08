using Backup_Manager.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backup_Manager.Interface
{
    public interface IBackupExecutor
    {
        bool Check(BackupItemModel item, Action<string> StatusList);
        bool DoBackup(BackupItemModel source, Action<string> StatusList, Action<double> ProgressUpdate);
    }
}
