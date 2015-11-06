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
using System.Xml.Serialization;

namespace LSPDApplication.ViewModel
{
    public class OfficerDetailsWindowViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public OfficerDetailsWindowViewModel()
        {
            this.SaveDataCommand = new RelayCommand(this.SaveData);
            this.WorkersData = Deserialize();
            this.OnPropertyChanged("WorkersData");
        }

        #region ICommands
        public ICommand SaveDataCommand { get; set; }
        #endregion

        #region Fields
        public List<Officer> WorkersData { get; set; }
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

        private void SaveData()
        {
            Serialize(this.WorkersData);
        }

        #region serialization
        public static void Serialize(List<Officer> workersData)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<Officer>));
            using (TextWriter writer = new StreamWriter("LSPDofficerslist.xml"))
            {
                serializer.Serialize(writer, workersData);
                writer.Close();
            }
        }

        public static List<Officer> Deserialize()
        {
            List<Officer> deserialized = new List<Officer>();
            if (File.Exists("LSPDofficerslist.xml"))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<Officer>));
                using (StreamReader reader = new StreamReader("LSPDofficerslist.xml"))
                {
                    deserialized = (List<Officer>)serializer.Deserialize(reader);
                }
            }
            return deserialized;
        }
        #endregion
    }
}