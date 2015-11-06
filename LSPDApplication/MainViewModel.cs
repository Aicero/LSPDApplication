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
    public class MainViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public MainViewModel()
        {
            this.SearchForHTMLFilesCommand = new RelayCommand(this.ChooseFolder);
            this.ImportCommand = new RelayCommand(this.ImportData);
            this.ExportCommand = new RelayCommand(this.ExportData);
            this.ProcessDataCommand = new RelayCommand(this.ProcessData);
            this.ShowMoreInfoCommand = new RelayCommand(this.ShowDetails);
            this.toDate = DateTime.Now.AddDays(-1);
            this.OnPropertyChanged("toDate");
            this.fromDate = DateTime.Now.AddDays(-7);
            this.OnPropertyChanged("fromDate");
        }

        #region ICommands
        public ICommand SearchForHTMLFilesCommand { get; set; }
        public ICommand ImportCommand { get; set; }
        public ICommand ExportCommand { get; set; }
        public ICommand ProcessDataCommand { get; set; }
        public ICommand ShowMoreInfoCommand { get; set; }

        #endregion

        #region Fields
        public string sourceOfHTMLFiles { get; set; }
        public List<Officer> WorkersData { get; set; }
        public DateTime fromDate { get; set; }
        public DateTime toDate { get; set; }

        private int maxEndOfDutyHour = 1;
        private int minStartOfDutyHour = 10;
        private int happyHourStart = 20;
        private int happyHourEnd = 23;
        private int mphh = 500;

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
            Serialize(this.WorkersData);
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
                officer.workerDutyTime = "0";
                officer.workerAway = 0;
                officer.workerWarn = false;
                officer.workerHappyHours = 0;
                officer.workerHappyHoursMoney = 0;
            }
            return officer;
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
                if (Int32.Parse(dutyStats.StartTime.Hour.ToString()) > maxEndOfDutyHour && Int32.Parse(dutyStats.StartTime.Hour.ToString()) < minStartOfDutyHour)
                {
                    dutyStats.StartTime = new DateTime(dutyStats.StartTime.Year, dutyStats.StartTime.Month, dutyStats.StartTime.Day, minStartOfDutyHour, 0, 0);
                }

                dutyStats.EndTime = HtmlToDateTimeReplace(tdContents.ElementAt(1));
                if (Int32.Parse(dutyStats.EndTime.Hour.ToString()) < minStartOfDutyHour && Int32.Parse(dutyStats.EndTime.Hour.ToString()) > maxEndOfDutyHour)
                {
                    dutyStats.EndTime = new DateTime(dutyStats.EndTime.Year, dutyStats.EndTime.Month, dutyStats.EndTime.Day, maxEndOfDutyHour, 0, 0);
                }

                dutyStats.Duration = dutyStats.EndTime.Subtract(dutyStats.StartTime).ToString();

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

        private void ProcessData()
        {
            List<Officer> officersList = WorkersData;
            foreach (var officer in officersList)
            {
                officer.workerDutyTime = this.GetWorkerDutyTime(officer.workerDutyList);
                TimeSpan workerDutyTime = TimeSpan.Parse(officer.workerDutyTime);
                if (workerDutyTime.Hours < 7 - officer.workerAway && workerDutyTime.Days == 0)
                {
                    officer.workerHappyHours = 0;
                    officer.workerWarn = true;
                }
                if (workerDutyTime.Hours >= 7 - officer.workerAway || workerDutyTime.Days != 0)
                {
                    officer.workerWarn = false;
                    if (workerDutyTime.Hours >= 12 || workerDutyTime.Days != 0)
                    {
                        officer.workerHappyHours = this.GetWorkerHappyHours(officer.workerDutyList);
                    }
                }
                officer.workerHappyHoursMoney = officer.workerHappyHours * mphh;
            }
            this.WorkersData = officersList.OrderBy(x => x.workerNick).ToList();
            Serialize(this.WorkersData);
            this.OnPropertyChanged("WorkersData");
        }

        private string GetWorkerDutyTime(List<Duty> workerDutyList)
        {
            TimeSpan workerDutyTime = new TimeSpan();
            foreach (var v in workerDutyList)
            {
                TimeSpan duration = TimeSpan.Parse(v.Duration);
                
                if (v.StartTime < fromDate.Date && v.EndTime <= fromDate.Date) // zaczê³o siê i skoñczy³o przed fromDate
                {
                    continue;
                }

                if (v.EndTime.Equals(new DateTime(1970, 1, 1, 0, 0, 0))) // jeœli b³¹d z koñczeniem duty (crash lub wcia¿ na /duty)
                {
                    continue;
                }

                if (duration.Ticks > 0)
                {
                    if (v.StartTime < fromDate.Date && v.EndTime > fromDate.Date) // zaczê³o siê przed, skoñczy³o siê po fromDate
                    {
                        TimeSpan overtime = fromDate.Date.Subtract(v.StartTime);
                        TimeSpan dutyDuration = duration.Subtract(overtime);

                        workerDutyTime += dutyDuration; //dodawanie d³ugoœci duty
                    }

                    if (v.StartTime >= fromDate.Date && v.EndTime <= toDate.Date)
                    {
                        workerDutyTime += duration;
                    }
                    if (v.StartTime < toDate.Date && v.EndTime >= toDate.Date)
                    {
                        TimeSpan overtime = v.EndTime.Subtract(toDate.Date.AddDays(1));
                        TimeSpan dutyDuration = duration.Subtract(overtime);
                        workerDutyTime += dutyDuration;
                    }
                }
            }
            return workerDutyTime.ToString();
        }

        private int GetWorkerHappyHours(List<Duty> workerDutyList)
        {
            TimeSpan workerHappyHours = new TimeSpan();
            foreach (var v in workerDutyList)
            {
                if (v.EndTime.Equals(new DateTime(1970, 1, 1, 0, 0, 0)) || (v.StartTime.Hour >= happyHourEnd || v.EndTime.Hour < happyHourStart) && v.EndTime.Day == v.StartTime.Day)
                {
                    continue;
                }

                DateTime start = new DateTime();
                DateTime end = new DateTime();

                if (v.EndTime.Hour < happyHourEnd)
                {
                    end = v.EndTime;
                    if (v.StartTime.Hour < happyHourStart)
                    {
                        start = new DateTime(v.StartTime.Year, v.StartTime.Month, v.StartTime.Day, happyHourStart, 0, 0);
                    }
                    if (v.StartTime.Hour >= happyHourStart)
                    {
                        start = v.StartTime;
                    }
                    if (v.EndTime.Day > v.StartTime.Day)
                    {
                        end = new DateTime(v.StartTime.Year, v.StartTime.Month, v.StartTime.Day, happyHourEnd, 0, 0);
                    }
                }
                if (v.EndTime.Hour >= happyHourEnd)
                {
                    end = new DateTime(v.StartTime.Year, v.StartTime.Month, v.StartTime.Day, happyHourEnd, 0, 0);
                    if (v.StartTime.Hour < happyHourStart)
                    {
                        start = new DateTime(v.StartTime.Year, v.StartTime.Month, v.StartTime.Day, happyHourStart, 0, 0);
                    }

                    if (v.StartTime.Hour >= happyHourStart)
                    {
                        start = v.StartTime;
                    }
                }
                workerHappyHours += end.Subtract(start);
            }
            return workerHappyHours.Hours;
        }


        private void ShowDetails()
        {
            OfficerDetailsWindow showDetailsWindow = new OfficerDetailsWindow();
            showDetailsWindow.Show();
        }

        private void ImportData()
        {
            List<Officer> deserialized = Deserialize();
            this.WorkersData = deserialized.OrderBy(x => x.workerNick).ToList();
            this.OnPropertyChanged("WorkersData");
        }


        private void ExportData()
        {
            DateTime today = DateTime.Now.Date;
            List<Officer> warnedOfficers = WorkersData.Where(o => o.workerWarn == true).OrderBy(o => o.workerNick).ToList();
            List<Officer> mostDutyTimeBenefits = WorkersData.OrderByDescending(o => o.workerDutyTime).ToList();
            List<Officer> happyHoursList = WorkersData.Where(o => o.workerHappyHours != 0).OrderBy(o => o.workerNick).ToList();
            
            StreamWriter writer = new StreamWriter("cpf.txt");
            writer.WriteLine("[center]" + today + "[/center]");
            writer.WriteLine();
            writer.WriteLine("[b]1.[/b] Wyliczona ilosc /duty czlonkow frakcji:");
            writer.WriteLine("[spoiler][img=][/spoiler]");
            writer.WriteLine();
            writer.WriteLine("[b]2.[/b] W zwiazku z niska aktywnoscia, ostrzezenia do akt otrzymuja:");
            writer.Write("[indent=1]");
            foreach (var officer in warnedOfficers)
            {
                writer.WriteLine(officer.workerNick);
            }
            writer.WriteLine("[/indent]");
            writer.WriteLine();
            writer.WriteLine("[b]3.[/b] Premie za najwyzsza aktywnosc w minionym tygodniu otrzymuja:");
            writer.Write("[indent=1]");
            for (int i = 0; i < 3; i++)
            {
                writer.WriteLine(mostDutyTimeBenefits[i].workerNick + " - ($" + (5 - i) + "000)");
            }
            writer.WriteLine("[/indent]");
            writer.WriteLine();
            writer.WriteLine("[b]4.[/b] Premie 'Happy Hours' za gre miêdzy godzina 20:00 a 23:00 otrzymuja:");
            foreach (var officer in happyHoursList)
            {
                writer.WriteLine(officer.workerNick + " - ($" + officer.workerHappyHoursMoney + ")");
            }
            writer.WriteLine("[/indent]");
            writer.Close();
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