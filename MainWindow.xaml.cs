using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ToolsLib;

namespace Backup_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViewModel.BackupManagerViewModel viewModel = new ViewModel.BackupManagerViewModel();
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = viewModel;
#if DEBUG
            Tools.WriteAppSetting("DoUpdateCheck", "false");
#endif
            ToolsLib.Tools.CheckForUpdates(viewModel.title, viewModel.version);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            viewModel.SaveLogsToFile();
        }
    }
}
