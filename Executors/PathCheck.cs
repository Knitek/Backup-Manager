using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using Backup_Manager.Model;

namespace Backup_Manager.Executor
{
    public static class PathCheck
    {
        public static bool DiskTypePath(string path, Action<string> StatusList, string pathType = "DataPath")
        {
            bool result = true;
            if (System.IO.Directory.Exists(path))
            {
                if (!PermissionCheck(path, StatusList,pathType))
                    result = false;
            }
            else
            {
                StatusList(path + " not exists!");
                result = false;
            }
            return result;
        }
        public static bool FTPTypePath(string path,FTPConnectionDataModel model,Action<string> StatusList)
        {
            bool result = true;
            FluentFTP.FtpClient client = new FluentFTP.FtpClient(model.Address, model.Username, model.Password);
            try
            {
                try
                {
                    client.Connect();
                    StatusList("Connected to "+model.Address);
                }catch(Exception e)
                { StatusList(e.Message); return false; }

                StatusList("Path: " + path);
                if (!client.DirectoryExists(path))
                {
                    StatusList("Path not exists");
                    client.Disconnect();
                    return false;
                }
                else
                {
                    StatusList("Path ok");
                }
                client.Disconnect();
            }
            catch(Exception exc)
            {
                ToolsLib.Tools.ExceptionLogAndShow(exc, "FromFTPPathCheck");
                client.Disconnect();
                result = false;
            }
            return result;
        }
        private static bool PermissionCheck(string path, Action<string> StatusList, string type)
        {
            try
            {
                AuthorizationRuleCollection rules = System.IO.Directory.GetAccessControl(path)
                    .GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
                var rulesCast = rules.Cast<FileSystemAccessRule>();

                if (rulesCast.Any(rule => rule.AccessControlType == AccessControlType.Deny)
                    || !rulesCast.Any(rule => rule.AccessControlType == AccessControlType.Allow))
                {
                    StatusList(path + " No Permission!");
                    return false;
                }
                else
                {
                    StatusList(type + " write permission Ok");
                    return true;
                }
            }
            catch (Exception)
            { //Posible UnauthorizedAccessException
                StatusList(path + " No Permission!");
                return false;
            }
        }
        // Returns the human-readable file size for an arbitrary, 64-bit file size
        //  The default format is "0.### XB", e.g. "4.2 KB" or "1.434 GB"
        public static string GetSizeReadable(long i)
        {
            string sign = (i < 0 ? "-" : "");
            double readable = (i < 0 ? -i : i);
            string suffix;
            if (i >= 0x1000000000000000) // Exabyte
            {
                suffix = "EB";
                readable = (double)(i >> 50);
            }
            else if (i >= 0x4000000000000) // Petabyte
            {
                suffix = "PB";
                readable = (double)(i >> 40);
            }
            else if (i >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = (double)(i >> 30);
            }
            else if (i >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = (double)(i >> 20);
            }
            else if (i >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = (double)(i >> 10);
            }
            else if (i >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = (double)i;
            }
            else
            {
                return i.ToString(sign + "0 B"); // Byte
            }
            readable = readable / 1024;

            return sign + readable.ToString("0.### ") + suffix;
        }
    }


}
