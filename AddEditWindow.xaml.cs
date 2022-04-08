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
using System.Windows.Shapes;

namespace Backup_Manager
{
    /// <summary>
    /// Interaction logic for AddEditWindow.xaml
    /// </summary>
    public partial class AddEditWindow : Window
    {
        public ViewModel.AddEditViewModel model { get; set; }
        public AddEditWindow()
        {            
            InitializeComponent();
            model = new ViewModel.AddEditViewModel(new Action(()=> { Close(); }));
            DataContext = model;
            this.ShowDialog();
        }
        public AddEditWindow(Model.BackupItemModel item)
        {
            InitializeComponent();
            model = new ViewModel.AddEditViewModel(new Action(() => { Close(); }),item);
            DataContext = model;
            this.ShowDialog();
        }
    }
}
