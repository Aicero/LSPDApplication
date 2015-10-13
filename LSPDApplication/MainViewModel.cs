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

namespace LSPDApplication.ViewModel
{
    public class MainViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public MainViewModel()
        {
            this.SearchForHTMLFilesCommand = new RelayCommand(this.ChooseFolder);
            this.ExportCommand = new RelayCommand(this.ExportData);
            this.FilterDataCommand = new RelayCommand(this.FilterData);
        }

        #region ICommands
        public ICommand SearchForHTMLFilesCommand { get; set; }
        public ICommand ExportCommand { get; set; }
        public ICommand FilterDataCommand { get; set; }

        #endregion

        #region Fields
        public string sourceOfHTMLFiles { get; set; }
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

        private void ChooseFolder()
        {
            var htmlFilesList = GetFilesFromDirectory();
            List<Officer> officersList = GetOfficersList(htmlFilesList);

            this.WorkersData = officersList.OrderBy(x => x.workerNick).ToList();
            this.OnPropertyChanged("WorkersData");
        }

        private string[] GetFilesFromDirectory()
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();

            if (result != DialogResult.OK)
            {
                return new string[] { };
            }
            var extractPath = Path.GetFullPath(fbd.SelectedPath);
            this.sourceOfHTMLFiles = extractPath;
            this.OnPropertyChanged("sourceOfHTMLFiles");

            var htmlFilesList = Directory.GetFiles(extractPath);
            return htmlFilesList;
        }

        private List<Officer> GetOfficersList(string[] htmlFilesList)
        {
            List<Officer> officersList = new List<Officer>();

            foreach (var item in htmlFilesList)
            {
                WebClient wc = new WebClient();
                string htmlString = wc.DownloadString(item);

                Officer officer = GetOfficerData(htmlString);
                officersList.Add(officer);
            }
            return officersList;
        }

        private Officer GetOfficerData(string htmlString)
        {
            var officer = new Officer();
            Match mNick = Regex.Match(htmlString, "<td>(.*?)</td>", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Match mTitle = Regex.Match(htmlString, "<input type=.*? name=\"workerTitle\" class=.*? value=\"(.*?)\"></td>", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Match mPayday = Regex.Match(htmlString, "<input type=.*? name=\"workerPayday\" class=.*? value=\"(.*?)\"></td>", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Match mSkin = Regex.Match(htmlString, "<input type=.*? name=\"workerSkin\" class=.*? value=\"(.*?)\"></td>", RegexOptions.Multiline | RegexOptions.IgnoreCase);

            if (mNick.Success && mTitle.Success && mPayday.Success && mSkin.Success)
            {
                officer.workerNick = mNick.Groups[1].Value;
                officer.workerRank = mTitle.Groups[1].Value;
                officer.workerPayday = Int32.Parse(mPayday.Groups[1].Value);
                officer.workerSkin = Int32.Parse(mSkin.Groups[1].Value);
                officer.workerDutyList = this.GetWorkerDutyList(htmlString);
                officer.workerDutyTime = this.GetWorkerDutyTime(officer.workerDutyList);
                if (officer.workerDutyTime.Hours < 7 )
                {
                    officer.workerHappyHours = 0;
                    officer.workerWarn = true;
                }
                if (officer.workerDutyTime.Hours >= 7 || officer.workerRank == "Sergeant II")
                {
                    officer.workerHappyHours = this.GetWorkerHappyHours(officer.workerDutyList);
                    officer.workerWarn = false;
                }
                officer.workerHappyHoursMoney = officer.workerHappyHours * 500;
            }
            return officer;
        }

        private int GetWorkerHappyHours(List<Duty> workerDutyList)
        {
            TimeSpan workerHappyHours = new TimeSpan();
            foreach (var v in workerDutyList)
            {
                if (v.EndTime.Equals(new DateTime(1970, 1, 1, 0, 0, 0)) || (v.StartTime.Hour >= 23 || v.EndTime.Hour < 20) && v.EndTime.Day == v.StartTime.Day)
                {
                    continue;
                }

                DateTime start = new DateTime();
                DateTime end = new DateTime();

                if (v.EndTime.Hour < 23)
                {
                    end = v.EndTime;
                    if (v.StartTime.Hour < 20)
                    {
                        start = new DateTime(v.StartTime.Year, v.StartTime.Month, v.StartTime.Day, 20, 0, 0);
                    }
                    if (v.StartTime.Hour >= 20)
                    {
                        start = v.StartTime;
                    }
                    if (v.EndTime.Day > v.StartTime.Day)
                    {
                        end = new DateTime(v.StartTime.Year, v.StartTime.Month, v.StartTime.Day, 23, 0, 0);
                    }
                }
                if (v.EndTime.Hour >= 23)
                {
                    end = new DateTime(v.StartTime.Year, v.StartTime.Month, v.StartTime.Day, 23, 0, 0);
                    if (v.StartTime.Hour < 20)
                    {
                        start = new DateTime(v.StartTime.Year, v.StartTime.Month, v.StartTime.Day, 20, 0, 0);
                    }

                    if (v.StartTime.Hour >= 20)
                    {
                        start = v.StartTime;
                    }
                }
                workerHappyHours += end.Subtract(start);
            }
            return workerHappyHours.Hours;
            //return workerHappyHours;
        }

        private List<Duty> GetWorkerDutyList(string htmlString)
        {
            var workerDutyList = new List<Duty>();

            string tr_pattern = "<tr>(.*?)</tr>";
            string td_pattern = "<td.*?>(.*?)</td>";

            var dutyTable = GetDutyStatistics(htmlString);
            var trContents = GetContents(dutyTable, tr_pattern);

            var iteration = 0;
            foreach (var trContent in trContents)
            {
                if (iteration == 0)
                {
                    iteration++;
                    continue;
                }
                List<string> tdContents = GetContents(trContent, td_pattern);
                Duty dutyStats = new Duty();

                dutyStats.StartTime = HtmlToDateTimeReplace(tdContents.ElementAt(0));
                if (Int32.Parse(dutyStats.StartTime.Hour.ToString()) > 2 && Int32.Parse(dutyStats.StartTime.Hour.ToString()) < 9)
                {
                    dutyStats.StartTime = new DateTime(dutyStats.StartTime.Year, dutyStats.StartTime.Month, dutyStats.StartTime.Day, 9, 0, 0);
                }

                dutyStats.EndTime = HtmlToDateTimeReplace(tdContents.ElementAt(1));
                if (Int32.Parse(dutyStats.EndTime.Hour.ToString()) < 9 && Int32.Parse(dutyStats.EndTime.Hour.ToString()) > 2)
                {
                    dutyStats.EndTime = new DateTime(dutyStats.EndTime.Year, dutyStats.EndTime.Month, dutyStats.EndTime.Day, 2, 0, 0);
                }

                dutyStats.Duration = dutyStats.EndTime.Subtract(dutyStats.StartTime);

                workerDutyList.Add(dutyStats);
            }
            return workerDutyList;
        }

        private string GetDutyStatistics(string htmlString)
        {
            string table_pattern = "<table.*?>(.*?)</table>";

            MatchCollection table_matches = Regex.Matches(htmlString, table_pattern, RegexOptions.Singleline);
            List<string> tableContents = new List<String>();

            foreach (Match match in table_matches)
            {
                tableContents.Add(match.Value);
            }

            return tableContents.ElementAt(5);
        }

        private List<string> GetContents(string input, string pattern)
        {
            MatchCollection matches = Regex.Matches(input, pattern, RegexOptions.Singleline);
            List<string> contents = new List<string>();

            foreach (Match match in matches)
            {
                contents.Add(match.Value);
            }
            return contents;
        }

        private DateTime HtmlToDateTimeReplace(string element)
        {
            string result = Regex.Replace(element, "<td>", "");
            result = Regex.Replace(result, "</td>", "");

            return DateTime.Parse(result);
        }

        private TimeSpan GetWorkerDutyTime(List<Duty> workerDutyList)
        {
            TimeSpan workerDutyTime = new TimeSpan();
            foreach (var v in workerDutyList)
            {
                if (v.EndTime.Equals(new DateTime(1970, 1, 1, 0, 0, 0)))
                {
                    continue;
                }

                if (v.Duration.Ticks > 0)
                {
                    workerDutyTime += v.Duration;
                }
            }
            return workerDutyTime;
        }






















        private void FilterData()
        {
            throw new NotImplementedException();
        }

        private void ExportData()
        {
            throw new NotImplementedException();
        }
    }
}