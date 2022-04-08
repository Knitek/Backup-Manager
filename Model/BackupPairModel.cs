using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backup_Manager.Model
{
    public class BackupPair
    {
        public string DataPath { get; set; }
        public string BackupPath { get; set; }
        public bool Replace { get; set; }
        public BackupPair(string d, string b, bool r)
        {
            this.DataPath = d;
            this.BackupPath = b;
            this.Replace = r;
        }
    }
}
