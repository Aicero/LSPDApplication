using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Security.RightsManagement;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using LSPDApplication.Classes;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Windows;

namespace LSPDApplication.ViewModel
{
    public class OfficerDetailsWindowViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public OfficerDetailsWindowViewModel()
        {
        }

        #region ICommands
        #endregion

        #region Fields
        #endregion

        #region INotifyPropertyChanged Members
        new public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        

    }
}