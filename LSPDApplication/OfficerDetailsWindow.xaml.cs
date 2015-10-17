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
using GalaSoft.MvvmLight.Ioc;
using LSPDApplication.ViewModel;
using MahApps.Metro.Controls;

namespace LSPDApplication
{
    /// <summary>
    /// Interaction logic for OfficerDetailsWindow.xaml
    /// </summary>
    public partial class OfficerDetailsWindow : MetroWindow
    {
        public OfficerDetailsWindow()
        {
            InitializeComponent();
            var vm = SimpleIoc.Default.GetInstance<OfficerDetailsWindowViewModel>();
            this.DataContext = vm;
        }
    }
}
