//This is the file controlling almost all the ui controls.
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Drawing;
using System.IO;
using Microsoft.Win32;
using ActivityMonitor;
using wpf;
using System.Threading;
using System.Diagnostics;

namespace wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon m_notifyIcon;
        public static string password = null;
        static bool startup = true;

        /// <summary>
        /// function called on pressing close button
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e)
        {
            Hide();
            //if (m_notifyIcon != null)
            //    m_notifyIcon.ShowBalloonTip(2000);
            e.Cancel = true;
            return;
        }

        /// <summary>
        /// stores current window state
        /// </summary>
        private WindowState m_storedWindowState = WindowState.Normal;

        /// <summary>
        /// called when window state changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void OnStateChanged(object sender, EventArgs args)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
            else
            {
                m_storedWindowState = WindowState;
            }
        }

        /// <summary>
        /// called when window's visibility changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            CheckTrayIcon();
        }

        /// <summary>
        /// called when tray icon is double clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void m_notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            if (getPPresence() == "yes")
            {
                Password pwin = new Password();
                pwin.Closed += pwin_Closed;
                pwin.Show();
            }
            else
            {
                Show();
                WindowState = m_storedWindowState;
            }
        }

        /// <summary>
        /// called when password window is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void pwin_Closed(object sender, EventArgs e)
        {
            if (password == null || password != getPassword())
            {
                MessageBox.Show("Password Incorrect");
                return;
            }
            password = null;
            Show();
            WindowState = m_storedWindowState;
        }

        void CheckTrayIcon()
        {
            ShowTrayIcon(!IsVisible);
        }

        /// <summary>
        /// shows system tray icon
        /// </summary>
        /// <param name="show"></param>
        void ShowTrayIcon(bool show)
        {
            if (m_notifyIcon != null)
                m_notifyIcon.Visible = show;
        }

        private string createFolder(string path)
        {
            bool isExists = System.IO.Directory.Exists(path);
            if (!isExists)
            {
                Directory.CreateDirectory(path);
                return path;
            }
            return createFolder(path + "0");
        }

        /// <summary>
        /// checks if user is entering for the first time
        /// </summary>
        /// <returns></returns>
        private bool IsFirstTime()
        {
            RegistryKey key;
            key = Registry.CurrentUser.OpenSubKey("Software", true).OpenSubKey("PCTrack", true);
            string w = null;
            try
            {
                w = key.GetValue("Path") as string;
            }
            catch 
            { 
                return true;  
            }
            if (w == null || w == "")
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// mainWindow constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            bool isFirstTime = IsFirstTime();
            if (isFirstTime)
            {
                WindowState = System.Windows.WindowState.Normal;
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                dialog.Description = "Choose folder where application data wil be saved.";
                string folderPath = createFolder("C:\\users\\" + Environment.UserName + "\\Documents\\PCTrack");
                dialog.SelectedPath = folderPath;
                //dialog.RootFolder = Environment.SpecialFolder.MyDocuments;
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                string res = dialog.SelectedPath;
                if (res.ToLower() != folderPath.ToLower())
                {
                    Directory.Delete(folderPath);
                }
                RegistryKey key;
                key = Registry.CurrentUser.OpenSubKey("Software", true).OpenSubKey("PCTrack", true);
                key.SetValue("Path", res);
            }

            List<KeyValuePair<string, double>> valueList = new List<KeyValuePair<string, double>>();
            valueList.Add(new KeyValuePair<string, double>("A", 0));
            valueList.Add(new KeyValuePair<string, double>("B", 0));

            ProcessStatGraph.DataContext = valueList;
            laptopStatGraph.DataContext = valueList;
            
            this.Height = SystemParameters.MaximizedPrimaryScreenHeight;
            this.Width = SystemParameters.MaximizedPrimaryScreenWidth;
            pcPanel.Width = SystemParameters.MaximizedPrimaryScreenWidth / 2.23;
            processPanel.Width = SystemParameters.MaximizedPrimaryScreenWidth / 2.23;
            ProcessStatGraph.Width = SystemParameters.MaximizedPrimaryScreenWidth / 2.23;
            laptopStatGraph.Width = SystemParameters.MaximizedPrimaryScreenWidth / 2.23;
            ProcessStatGraph.Height = SystemParameters.MaximizedPrimaryScreenHeight / 2.8;
            laptopStatGraph.Height = SystemParameters.MaximizedPrimaryScreenHeight / 2.8;
            this.Title = "PCTrack";
            if (getPPresence() == "yes")
            {
                passwordItem.IsChecked = true;
            }
            else
            {
                passwordItem.IsChecked = false;
                startup = false;
            }
            if (XmlDataLayer.GetConfigEntry("sync_online") == "enabled")
            {
                syncToggle.IsChecked = true;
                uploadCloud.IsEnabled = true;
                if (XmlDataLayer.GetConfigEntry("upload_cloud") == "enabled")
                {
                    uploadCloud.IsChecked = true;
                }
                downloadCloud.IsEnabled = true;
                if (XmlDataLayer.GetConfigEntry("download_cloud") == "enabled")
                {
                    downloadCloud.IsChecked = true;
                }
            }
            else
            {
                syncToggle.IsChecked = false;
            }
            if (XmlDataLayer.GetConfigEntry("upload_cloud") == "enabled")
            {
                uploadCloud.IsChecked = true;
            }
            if (XmlDataLayer.GetConfigEntry("download_cloud") == "enabled")
            {
                downloadCloud.IsChecked = true;
            }
            m_notifyIcon = new System.Windows.Forms.NotifyIcon();


            System.Windows.Forms.ContextMenu trayMenu = new System.Windows.Forms.ContextMenu();
            System.Windows.Forms.MenuItem exit = new System.Windows.Forms.MenuItem();
            exit.Text = "exit";
            exit.Visible = true;
            exit.Click += exit_Click;
            System.Windows.Forms.MenuItem pause = new System.Windows.Forms.MenuItem();
            pause.Text = "pause";
            pause.Visible = true;
            pause.Click += pause_Click;
            //exit.Click += exit_Click;
            trayMenu.MenuItems.Add(pause);
            trayMenu.MenuItems.Add(exit);
            m_notifyIcon.ContextMenu = trayMenu;
    
            //m_notifyIcon.BalloonTipText = "The app has been minimised. Click the tray icon to show.";
            m_notifyIcon.BalloonTipText = null;
            m_notifyIcon.BalloonTipTitle = "PCTrack";
            m_notifyIcon.Text = "PCTrack";
            string s = System.IO.Path.GetFullPath("systray.ico");
            m_notifyIcon.Icon = new System.Drawing.Icon(s);
            m_notifyIcon.DoubleClick += new EventHandler(m_notifyIcon_DoubleClick);

            Uri iconUri = new Uri(s, UriKind.RelativeOrAbsolute);
            this.Icon = BitmapFrame.Create(iconUri);

            ContextMenu windowMenu = new ContextMenu();
            MenuItem syncEntry = new MenuItem();
            syncEntry.Header = "WPF";
            //syncEntry.Tag = new List<string> { date.ToString("yyyy-MM-dd"), start, end, process.Name };
            //syncEntry.Click += deleteProcessEntry_Click;
            windowMenu.Items.Add(syncEntry);
            this.ContextMenu = windowMenu;
        
            MainManager.SpawnProcesses();
            Thread refreshUserInterface = new Thread(refreshUI);
            refreshUserInterface.Start();

            WindowState = System.Windows.WindowState.Minimized;
            Hide();
            CheckTrayIcon();
        }

        /// <summary>
        /// called when pause is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void pause_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.MenuItem pauseClick = sender as System.Windows.Forms.MenuItem;
            if (pauseClick.Text == "pause")
            {
                JobMonitor.Pause = true;
                pauseClick.Text = "resume";
            }
            else
            {
                JobMonitor.Pause = false;
                pauseClick.Text = "pause";
            }
        }

        /// <summary>
        /// called when application is to be exited
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void exit_Click(object sender, EventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }

        /// <summary>
        /// started in a thread to refresh UI
        /// </summary>
        private void refreshUI()
        {
            while (true)
            {
                refreshUIOnce();
                Thread.Sleep(2 * 60 * 1000);
            }
        }

        /// <summary>
        /// called to refresh ui once
        /// </summary>
        private void refreshUIOnce()
        {
            DateTime? date = null;
            mainActivityDate.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                new Action(
                    delegate()
                    {
                        date = mainActivityDate.SelectedDate;
                        mainActivityDate.SelectedDate = date == null ? DateTime.Now : date;
                    }
            ));
            date = date == null ? DateTime.Now : date;
            mainActivities.Dispatcher.Invoke(
            System.Windows.Threading.DispatcherPriority.Normal,
            new Action(
                delegate()
                {
                    displayGrid((DateTime)date);
                }
            ));
            DateTime? startDate = null, endDate = null;
            mainProcessStartDate.Dispatcher.Invoke(
               System.Windows.Threading.DispatcherPriority.Normal,
               new Action(
                   delegate()
                   {
                       startDate = mainProcessStartDate.SelectedDate;
                       mainProcessStartDate.SelectedDate = startDate == null ? DateTime.Now : startDate;
                   }
            ));
            mainProcessEndDate.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                new Action(
                    delegate()
                    {
                        endDate = mainProcessEndDate.SelectedDate;
                        mainProcessEndDate.SelectedDate = endDate == null ? DateTime.Now : endDate;
                    }
            ));
            startDate = startDate == null ? DateTime.Now : startDate;
            endDate = endDate == null ? DateTime.Now : endDate;
            ProcessStatGraph.Dispatcher.Invoke(
            System.Windows.Threading.DispatcherPriority.Normal,
            new Action(
                delegate()
                {
                    displayChartProcess((DateTime)startDate, (DateTime)endDate);
                }
            ));

            laptopStartDate.Dispatcher.Invoke(
               System.Windows.Threading.DispatcherPriority.Normal,
               new Action(
                   delegate()
                   {
                       startDate = laptopStartDate.SelectedDate;
                       laptopStartDate.SelectedDate = startDate == null ? DateTime.Now.AddDays(-5) : startDate;
                   }
            ));
            laptopEndDate.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                new Action(
                    delegate()
                    {
                        endDate = laptopEndDate.SelectedDate;
                        laptopEndDate.SelectedDate = endDate == null ? DateTime.Now : endDate;
                    }
            ));
            startDate = startDate == null ? DateTime.Now.AddDays(-5) : startDate;
            endDate = endDate == null ? DateTime.Now : endDate;
            laptopStatGraph.Dispatcher.Invoke(
            System.Windows.Threading.DispatcherPriority.Normal,
            new Action(
                delegate()
                {
                    laptopChartProcess((DateTime)startDate, (DateTime)endDate);
                }
            ));
        }

        /// <summary>
        /// displays main log grid on the window
        /// </summary>
        /// <param name="date"></param>
        private void displayGrid(DateTime date)
        {
            //date = new DateTime(2013, 11, 4);
            mainActivities.Children.Clear();
            mainActivities.RowDefinitions.Clear();
            mainActivities.ColumnDefinitions.Clear();
            DayFinalActivities todayFinalActivities = new DayFinalActivities(date);
            ColumnDefinition timeCol = new ColumnDefinition();
            timeCol.Width = new GridLength(1, GridUnitType.Star);
            ColumnDefinition titleCol = new ColumnDefinition();
            titleCol.Width = new GridLength(5, GridUnitType.Star);
            ColumnDefinition processCol = new ColumnDefinition();
            processCol.Width = new GridLength(1, GridUnitType.Star);
            mainActivities.ColumnDefinitions.Add(timeCol);
            mainActivities.ColumnDefinitions.Add(titleCol);
            mainActivities.ColumnDefinitions.Add(processCol);
            int i = 0;
            string endTime = null;
            foreach (var entry in todayFinalActivities.FinalActivityList)
            {
                RichTextBox ttime = new RichTextBox();
                ttime.IsReadOnly = true;
                string start = new DateTime(entry.StartTime.Ticks).ToString("HH:mm");
                string end = new DateTime(entry.EndTime.Ticks).ToString("HH:mm");
                if (endTime != null && (endTime != start))
                {
                    RowDefinition row1 = new RowDefinition();
                    row1.Height = new GridLength(ttime.FontSize - 4);
                    mainActivities.RowDefinitions.Add(row1);
                    RichTextBox dummy = new RichTextBox();
                    Label col = new Label();
                    col.Background = System.Windows.Media.Brushes.WhiteSmoke;
                    Grid.SetRow(col, i);
                    Grid.SetColumn(col, 0);
                    Grid.SetColumnSpan(col, 3);
                    mainActivities.Children.Add(col);
                    i += 1;
                }
                int rows = addRequiredRows(entry);
                double hrs = (entry.EndTime-entry.StartTime).TotalHours;
                ttime.AppendText(start + " - " + end+"   ("+hrs+" hr");
                if (hrs != 1)
                {
                    ttime.AppendText("s");
                }
                ttime.AppendText(")");
                ttime.Background = System.Windows.Media.Brushes.Ivory;
                Grid.SetRow(ttime, i);
                Grid.SetColumn(ttime, 0);
                Grid.SetRowSpan(ttime, rows);
                ContextMenu entryMenu = new ContextMenu();
                MenuItem deleteTimeEntry = new MenuItem();
                deleteTimeEntry.Header = "delete time entry";
                deleteTimeEntry.Click += deleteTimeEntry_Click;
                deleteTimeEntry.Tag = new List<string> { date.ToString("yyyy-MM-dd"), start, end };
                entryMenu.Items.Add(deleteTimeEntry);
                ttime.ContextMenu = entryMenu;
                mainActivities.Children.Add(ttime);
                int n = mainActivities.RowDefinitions.Count;
                int j=0;
                foreach (var process in entry.Processes)
                {
                    int prows = process.Titles.Count*2;
                    RichTextBox tname = new RichTextBox();
                    tname.IsReadOnly = true;
                    tname.Background = System.Windows.Media.Brushes.SeaShell;
                    TextRange time = new TextRange(tname.Document.ContentEnd, tname.Document.ContentEnd);
                    time.Text = "(" + roundOff(process.Duration).ToString("HH:mm") + ") ";
                    //time.ApplyPropertyValue(TextElement.ForegroundProperty, System.Drawing.Brushes.Red);
                    tname.AppendText(process.Name);
                    ContextMenu processMenu = new ContextMenu();
                    MenuItem deleteProcessEntry = new MenuItem();
                    deleteProcessEntry.Header = "delete process entry";
                    deleteProcessEntry.Tag = new List<string> { date.ToString("yyyy-MM-dd"), start, end, process.Name };
                    deleteProcessEntry.Click += deleteProcessEntry_Click;
                    processMenu.Items.Add(deleteProcessEntry);
                    tname.ContextMenu = processMenu;
                    Grid.SetRow(tname, i+j);
                    Grid.SetRowSpan(tname, prows);
                    Grid.SetColumn(tname, 2);
                    mainActivities.Children.Add(tname);
                    int k=0;
                    foreach (var title in process.Titles)
                    {
                        RichTextBox ttitle = new RichTextBox();
                        ttitle.IsReadOnly = true;
                        ttitle.AppendText("(" + roundOff(title.Duration).ToString("HH:mm") + ") " + title.Heading);
                        ContextMenu titleMenu = new ContextMenu();
                        MenuItem deleteTitleEntry = new MenuItem();
                        deleteTitleEntry.Header = "delete title entry";
                        deleteTitleEntry.Tag = new List<string> { date.ToString("yyyy-MM-dd"), start, end, process.Name, title.Heading };
                        deleteTitleEntry.Click += deleteTitleEntry_Click;
                        titleMenu.Items.Add(deleteTitleEntry);
                        ttitle.ContextMenu = titleMenu;
                        Grid.SetRow(ttitle, i+j+k);
                        Grid.SetRowSpan(ttitle, 2);
                        Grid.SetColumn(ttitle, 1);
                        mainActivities.Children.Add(ttitle);
                        k += 2;
                    }
                    j += prows;
                }
                i += rows;
                endTime = end;
            }
        }

        /// <summary>
        /// called when title entry is to be deleted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void deleteTitleEntry_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            XmlDataLayer.DeleteTitleEntry(((List<string>)item.Tag)[0], ((List<string>)item.Tag)[1], ((List<string>)item.Tag)[2], ((List<string>)item.Tag)[3], ((List<string>)item.Tag)[4]);
            refreshUIOnce();
        }

        /// <summary>
        /// called when process entry is to be deleted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void deleteProcessEntry_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            XmlDataLayer.DeleteProcessEntry(((List<string>)item.Tag)[0], ((List<string>)item.Tag)[1], ((List<string>)item.Tag)[2], ((List<string>)item.Tag)[3]);
            refreshUIOnce();
        }

        /// <summary>
        /// called when time entry is to be deleted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void deleteTimeEntry_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            XmlDataLayer.DeleteTimeEntry(((List<string>)item.Tag)[0], ((List<string>)item.Tag)[1], ((List<string>)item.Tag)[2]);
            refreshUIOnce();
        }

        /// <summary>
        /// round off date to nearest half hour
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        private static DateTime roundOff(TimeSpan timeSpan)
        {
            return new DateTime(timeSpan.Add(new TimeSpan(0, 0, 30)).Ticks);
        }

        /// <summary>
        /// add rows to grid according to entry
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        private int addRequiredRows(DayFinalActivities.FinalActivities entry)
        {
            TimeSpan diff = entry.EndTime - entry.StartTime;
            int toAdd = 0;
            foreach (var process in entry.Processes)
            {
                toAdd += process.Titles.Count;
            }
            RichTextBox ttime = new RichTextBox();
            toAdd *= 2;
            for (int i = 0; i < toAdd; i++)
            {
                RowDefinition row1 = new RowDefinition();
                row1.Height = new GridLength(ttime.FontSize + 6);
                mainActivities.RowDefinitions.Add(row1);
            }
            return toAdd;
        }

        /// <summary>
        /// called when date for logs is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainActivityDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var picker = sender as DatePicker;
	        DateTime? date = picker.SelectedDate;
            if (date == null)
            {
                displayGrid(DateTime.Now);
            }
            else
            {
                displayGrid((DateTime)date);
            }
        }

        /// <summary>
        /// called when online sync is disabled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Unchecked_1(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            bool x = item.IsChecked;
            uploadCloud.IsEnabled = false;
            downloadCloud.IsEnabled = false;
            XmlDataLayer.SetConfigEntry("authentication", "not_done");
            XmlDataLayer.SetConfigEntry("sync_online", "disabled");
        }

        /// <summary>
        /// called when online sync is enabled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Checked_1(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            XmlDataLayer.SetConfigEntry("sync_online", "enabled");
            string pcId = XmlDataLayer.GetConfigEntry("pc_id");
            if (pcId == null)
            {
                Random rnd = new Random();
                int id = rnd.Next(0, 1000000);
                XmlDataLayer.SetConfigEntry("pc_id", id.ToString());
            }
            uploadCloud.IsEnabled = true;
            downloadCloud.IsEnabled = true;
            if (XmlDataLayer.GetConfigEntry("upload_cloud") == "enabled")
            {
                uploadCloud.IsChecked = true;
            }
            else
            {
                uploadCloud.IsChecked = false;
            }
            if (XmlDataLayer.GetConfigEntry("download_cloud") == "enabled")
            {
                downloadCloud.IsChecked = true;
            }
            else
            {
                downloadCloud.IsChecked = false;
            }
            Thread newThread = new Thread(() => GoogleDriveSync.UploadDownloadNewFiles());
            newThread.Start();
            MenuItem item = sender as MenuItem;
            bool x = item.IsChecked;
        }

        /// <summary>
        /// called to abort uploads
        /// </summary>
        private void abortUploadDriveThread()
        {
            if (GoogleDriveSync.uploadThread != null && GoogleDriveSync.uploadThread.IsAlive == true)
            {
                GoogleDriveSync.uploadThread.Abort();
            }
        }

        /// <summary>
        /// called to abort downloads
        /// </summary>
        private void abortDownloadDriveThread()
        {
            if (GoogleDriveSync.downloadThread != null && GoogleDriveSync.downloadThread.IsAlive == true)
            {
                GoogleDriveSync.downloadThread.Abort();
            }
        }

        /// <summary>
        /// called when startdate of process graph is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainProcessStartDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var picker = sender as DatePicker;
            DateTime? startDate = picker.SelectedDate;
            DateTime? endDate = mainProcessEndDate.SelectedDate;
            if (startDate == null)
            {
                startDate = (DateTime)DateTime.Now;
            }
            if (endDate == null)
            {
                endDate = (DateTime)DateTime.Now;
            }
            if (startDate > endDate)
            {
                MessageBox.Show("Start date must be less than End date");
            }
            else
            {
                displayChartProcess((DateTime)startDate, (DateTime)endDate);
            }
        }

        /// <summary>
        /// called when end-date of processgraph is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainProcessEndDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var picker = sender as DatePicker;
            DateTime? endDate = picker.SelectedDate;
            DateTime? startDate = mainProcessStartDate.SelectedDate;
            if (startDate == null)
            {
                startDate = (DateTime)DateTime.Now;
            }
            if (endDate == null)
            {
                endDate = (DateTime)DateTime.Now;
            }
            if (startDate > endDate)
            {
                MessageBox.Show("Start date must be less than End date");
            }
            else
            {
                displayChartProcess((DateTime)startDate, (DateTime)endDate);
            }
        }

        /// <summary>
        /// valled to display the process graph
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        private void displayChartProcess(DateTime startDate, DateTime endDate)
        {
            bool flag = true;
            List<DayProcessStats.FinalProcessStat> ProcessList = new List<DayProcessStats.FinalProcessStat>();
            DateTime current = startDate;

            while (current <= endDate)
            {
                DayProcessStats dayProcessStats = new DayProcessStats(current);

                foreach (var process in dayProcessStats.FinalProcessStatList)
                {
                    flag = true;
                    foreach (var pro in ProcessList)
                    {
                        if (pro.Name.Equals(process.Name))
                        {
                            pro.Period = pro.Period.Add(process.Period);
                            flag = false;
                            break;
                        }
                    }

                    if (flag)
                    {
                        ProcessList.Add(new DayProcessStats.FinalProcessStat(process.Name, process.Period));
                    }
                }
                current = current.AddDays(1);
            }

            List<KeyValuePair<string, double>> valueList = new List<KeyValuePair<string, double>>();
            List<KeyValuePair<string, double>> finalValueList = new List<KeyValuePair<string, double>>();

            foreach (var process in ProcessList)
            {
                double period = process.Period.TotalHours < 0 ? 0 : process.Period.TotalHours;
                valueList.Add(new KeyValuePair<string, double>(process.Name, (double)period));
            }

            valueList.Sort(compare);
            int k = 0;
            double p = 0;
            foreach (var entry in valueList)
            {
                if (k < 5)
                {
                    finalValueList.Add(entry);
                    k++;
                }
                else
                {
                    p = p + entry.Value;
                }
            }
            finalValueList.Add(new KeyValuePair<string, double>("others", p));

            ProcessStatGraph.DataContext = finalValueList;
        }


        /// <summary>
        /// called when start date of pc time graph is changed
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private void laptopStartDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var picker = sender as DatePicker;
            DateTime? startDate = picker.SelectedDate;
            DateTime? endDate = laptopEndDate.SelectedDate;
            if (startDate == null)
            {
                startDate = (DateTime)DateTime.Now;
            }
            if (endDate == null)
            {
                endDate = (DateTime)DateTime.Now;
            }
            if (startDate > endDate)
            {
                MessageBox.Show("Start date must be less than End date.");
            }
            else if (((TimeSpan)(endDate-startDate)).TotalDays > 15)
            {
                MessageBox.Show("Interval must be lesser than 15 days.");
            }
            else
            {
                laptopChartProcess((DateTime)startDate, (DateTime)endDate);
            }
        }

        /// <summary>
        /// called when end date of pc time graph is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void laptopEndDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var picker = sender as DatePicker;
            DateTime? endDate = picker.SelectedDate;
            DateTime? startDate = laptopStartDate.SelectedDate;
            if (startDate == null)
            {
                startDate = (DateTime)DateTime.Now;
            }
            if (endDate == null)
            {
                endDate = (DateTime)DateTime.Now;
            }
            if (startDate > endDate)
            {
                MessageBox.Show("Start date must be less than End date");
            }
            else if (((TimeSpan)(endDate - startDate)).TotalDays > 15)
            {
                MessageBox.Show("Interval must be lesser than 15 days.");
            }
            else
            {
                laptopChartProcess((DateTime)startDate, (DateTime)endDate);
            }
        }

        /// <summary>
        /// called to draw pc time graph
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        private void laptopChartProcess(DateTime startDate, DateTime endDate)
        {
            List<KeyValuePair<string, double>> valueList = new List<KeyValuePair<string, double>>();
            List<KeyValuePair<string, double>> finalValueList = new List<KeyValuePair<string, double>>();
            DateTime current = startDate;
            TimeSpan period = new TimeSpan();

            while (current <= endDate)
            {
                DayProcessStats dayProcessStats = new DayProcessStats(current);
                period = TimeSpan.Zero;
                foreach (var process in dayProcessStats.FinalProcessStatList)
                {
                    period = period.Add(process.Period);
                }
                valueList.Add(new KeyValuePair<string, double>(current.ToString("dd-MM"), (double)period.TotalHours));
                current = current.AddDays(1);
            }

            laptopStatGraph.DataContext = valueList;
        }

        static int compare(KeyValuePair<string, double> a, KeyValuePair<string, double> b)
        {
            return b.Value.CompareTo(a.Value);
        }

        /// <summary>
        /// disable password
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Unchecked_2(object sender, RoutedEventArgs e)
        {
            setPPresence("no");
        }

        /// <summary>
        /// to enable password
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Checked_2(object sender, RoutedEventArgs e)
        {
            if (startup)
            {
                startup = false;
                return;
            }
            setPPresence("yes");
            Password p = new Password();
            p.Closed += p_Closed;
            p.Show();
        }

        /// <summary>
        /// called when password popup is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void p_Closed(object sender, EventArgs e)
        {
            if (password == null)
            {
                setPPresence("no");
                passwordItem.IsChecked = false;
                return;
            }
            setPassword(password);
            password = null;
        }

        /// <summary>
        /// gets the current password
        /// </summary>
        /// <returns></returns>
        private string getPassword()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", true).OpenSubKey("PCTrack", true);
            try
            {
                string password = key.GetValue("Password") as string;
                return password;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// called to set the password
        /// </summary>
        /// <param name="hash"></param>
        private void setPassword(string hash)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", true).OpenSubKey("PCTrack", true);
            key.SetValue("password", hash);
        }

        /// <summary>
        /// called to check if password is present
        /// </summary>
        /// <returns></returns>
        private string getPPresence()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", true).OpenSubKey("PCTrack", true);
            try
            {
                string password = key.GetValue("IsPassword") as string;
                return password;
            }
            catch (Exception)
            {
                return "no";
            }
        }

        /// <summary>
        /// called to set password presence
        /// </summary>
        /// <param name="value"></param>
        private void setPPresence(string value)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", true).OpenSubKey("PCTrack", true);
            key.SetValue("IsPassword", value);
        }

        /// <summary>
        /// called when uploads are enabled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void uploadCloud_Checked_1(object sender, RoutedEventArgs e)
        {
            if (XmlDataLayer.GetConfigEntry("sync_online") == "enabled")
            {
                abortUploadDriveThread();
                XmlDataLayer.SetConfigEntry("upload_cloud", "enabled");
                Thread newThread = new Thread(() => GoogleDriveSync.UploadDownloadNewFiles());
                newThread.Start();
            }
        }

        /// <summary>
        /// called when uploads are disabled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void uploadCloud_Unchecked_1(object sender, RoutedEventArgs e)
        {
            XmlDataLayer.SetConfigEntry("upload_cloud", "disabled");
            abortUploadDriveThread();
        }

        /// <summary>
        /// called when downloads are enabled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void downloadCloud_Checked_1(object sender, RoutedEventArgs e)
        {
            if (XmlDataLayer.GetConfigEntry("sync_online") == "enabled")
            {
                abortDownloadDriveThread();
                XmlDataLayer.SetConfigEntry("download_cloud", "enabled");
                Thread newThread = new Thread(() => GoogleDriveSync.UploadDownloadNewFiles());
                newThread.Start();
            }
        }

        /// <summary>
        /// called when downloads are disabled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void downloadCloud_Unchecked_1(object sender, RoutedEventArgs e)
        {
            XmlDataLayer.SetConfigEntry("download_cloud", "disabled");
            abortDownloadDriveThread();
        }
    }
}