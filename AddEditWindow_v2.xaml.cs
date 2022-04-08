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
    /// Interaction logic for AddEditWindow_v2.xaml
    /// </summary>
    public partial class AddEditWindow_v2 : Window
    {
        public ViewModel.AddEditViewModel model { get; set; }
        public AddEditWindow_v2()
        {
            InitializeComponent();
            model = new ViewModel.AddEditViewModel(new Action(() => { Close(); }));
            DataContext = model;
            this.ShowDialog();
        }
        public AddEditWindow_v2(Model.BackupItemModel item)
        {
            InitializeComponent();
            model = new ViewModel.AddEditViewModel(new Action(() => { Close(); }), item);
            DataContext = model;
            this.ShowDialog();
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {

            //System.Windows.Data.CollectionViewSource backupItemModelViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("backupItemModelViewSource")));
            // Load data by setting the CollectionViewSource.Source property:
            //backupItemModelViewSource.Source = DataLayer.
            //System.Windows.Data.CollectionViewSource addEditViewModelViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("addEditViewModelViewSource")));
            //// Load data by setting the CollectionViewSource.Source property:
            //addEditViewModelViewSource.Source = model;
        }
    }
}
