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
using System.Diagnostics;

namespace UpTimeWin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<DateTime, List<TimeBlocks>> PcOnOff = new Dictionary<DateTime, List<TimeBlocks>>();
        BackgroundWorker bgworker = new BackgroundWorker();
        public MainWindow()
        {
            InitializeComponent();
            bgworker.DoWork += new DoWorkEventHandler(bgwork_dowork);
            bgworker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
            bgworker.RunWorkerAsync();
        }

        void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            DrawData();
        }
        void bgwork_dowork(object sender, DoWorkEventArgs e)
        {
            InitializeData();
        }

        private void InitializeData()
        {
            EventLog logs = new EventLog();
            logs.Log = "System";
            logs.MachineName = ".";
            var entries = logs.Entries.Cast<EventLogEntry>();
            var on = from e in entries
                     where e.InstanceId == 2147489653 && e.Source.Contains("EventLog") && e.EntryType == EventLogEntryType.Information && e.TimeGenerated >= DateTime.Now.AddDays(-11)//DateTime.Now.AddMonths(-1)
                     select new OnOffEntry { EntryDate = e.TimeGenerated.Date, TimeGenerated = e.TimeGenerated, Type = "On" }; ;
            var off = from e in entries
                      where e.InstanceId == 2147489654 && e.Source.Contains("EventLog") && e.EntryType == EventLogEntryType.Information && e.TimeGenerated >= DateTime.Now.AddDays(-11)//DateTime.Now.AddMonths(-1)
                      select new OnOffEntry { EntryDate = e.TimeGenerated.Date, TimeGenerated = e.TimeGenerated, Type = "Off" };
           // var sleep = from e in entries
             //           where e.InstanceId == 42 && e.Source.Contains("Kernel-Power") && e.EntryType == EventLogEntryType.Information && e.TimeGenerated >= DateTime.Now.AddMonths(-1)
               //         select new OnOffEntry { EntryDate = e.TimeGenerated.Date, TimeGenerated = e.TimeGenerated, Type = "Sleep" };
            //var awake = from e in entries
              //          where e.InstanceId == 1 && e.Source.Contains("Kernel-General") && e.EntryType == EventLogEntryType.Information && e.UserName == null && e.TimeGenerated >= DateTime.Now.AddMonths(-1)
                //        select new OnOffEntry { EntryDate = e.TimeGenerated.Date, TimeGenerated = e.TimeGenerated, Type = "Awake" };
            List<OnOffEntry> temp = new List<OnOffEntry>();
            on.ToList().ForEach(x =>
            {
                if (temp.FirstOrDefault(y => y.TimeGenerated == x.TimeGenerated) == null)
                    temp.Add(x);
            });
            off.ToList().ForEach(x =>
            {
                if (temp.FirstOrDefault(y => y.TimeGenerated == x.TimeGenerated) == null)
                    temp.Add(x);
            });
          ////  sleep.ToList().ForEach(x =>
          //  {
          //      if (temp.FirstOrDefault(y => y.TimeGenerated == x.TimeGenerated) == null)
          //          temp.Add(x);
          //  });

          //  //awake.ToList().ForEach(x =>
          //  {
          //      if (temp.FirstOrDefault(y => y.TimeGenerated == x.TimeGenerated) == null)
          //          temp.Add(x);
          //  });
            List<OnOffEntry> result = new List<OnOffEntry>();
            string actionType = "";
            OnOffEntry itemBefore = null;
            foreach (OnOffEntry item in temp.Distinct().OrderBy(x => x.TimeGenerated))
            {
                if (actionType == item.Type)
                    result.Remove(itemBefore);
                result.Add(item);
                actionType = item.Type;
                itemBefore = item;
            }
            if ((result[0].Type == "Sleep" || result[0].Type == "Off"))
            {
                result.Insert(0, new OnOffEntry { EntryDate = result[0].EntryDate, TimeGenerated = result[0].EntryDate, Type = "On" });
            }

            //testing
            /*foreach (var tempresult in result)
            {
                Console.WriteLine(tempresult.EntryDate + " | " + tempresult.TimeGenerated + " | " + tempresult.Type);
            }*/
            Dictionary<DateTime, List<TimeBlocks>> PcOnOffTime = new Dictionary<DateTime, List<TimeBlocks>>();

            for (int count = 0; count < result.Count - 1; count++)
            {
                int nextCount = count + 1;
                if (!PcOnOffTime.ContainsKey(result[count].EntryDate))
                {
                    List<TimeBlocks> EachTimeBlocks = new List<TimeBlocks>();
                    PcOnOffTime.Add(result[count].EntryDate, EachTimeBlocks);
                }

                PcOnOffTime[result[count].EntryDate].Add(new TimeBlocks
                {
                    EntryStartTime = result[count].TimeGenerated,
                    startAction = result[count].Type,
                    EntryStopTime = result[++count].TimeGenerated,
                    stopAction = result[nextCount].Type
                });
            }

            if (!PcOnOffTime.ContainsKey(result.Last().EntryDate))
            {
                List<TimeBlocks> EachTimeBlocks = new List<TimeBlocks>();
                PcOnOffTime.Add(result.Last().EntryDate, EachTimeBlocks);
            }
            PcOnOffTime[result.Last().EntryDate].Add(new TimeBlocks
            {
                EntryStartTime = result.Last().TimeGenerated,
                startAction = result.Last().Type,
                EntryStopTime = DateTime.Now,
                stopAction = "Off"
            });

            PcOnOff = new Dictionary<DateTime, List<TimeBlocks>>(PcOnOffTime);
            var firstKey = PcOnOffTime.ElementAt(0).Key;
            for (int i = 1; i < PcOnOffTime.Count; i++)
            {
                var nextKey = PcOnOffTime.ElementAt(i).Key;
                int days = 1;
                while (firstKey.AddDays(days).CompareTo(nextKey) == -1)
                {
                    List<TimeBlocks> EachTimeBlocks = new List<TimeBlocks>();
                    PcOnOff.Add(firstKey.AddDays(days), EachTimeBlocks);
                    days++;
                    //if()
                }
                firstKey = nextKey;
            }
        }
        public void DrawData()
        {
            int hoursPerDay = 24;
            if (PcOnOff.Count == 0)
            {
                return;
            }
            HeaderGrid.Children.Clear();
            HeaderGrid.ColumnDefinitions.Clear();
            HeaderGrid.RowDefinitions.Clear();
            FooterGrid.Children.Clear();
            FooterGrid.ColumnDefinitions.Clear();
            FooterGrid.RowDefinitions.Clear();
            MainGrid.Children.Clear();
            MainGrid.ColumnDefinitions.Clear();
            MainGrid.RowDefinitions.Clear();

            for (int i = 0; i < hoursPerDay; i++)
            {
                ColumnDefinition colHour = new ColumnDefinition();
                colHour.Width = new GridLength(1, GridUnitType.Star);
                HeaderGrid.ColumnDefinitions.Add(colHour);


                TextBlock tbHour = new TextBlock();
                tbHour.FontWeight = FontWeights.Bold;
                tbHour.FontFamily = new FontFamily("Castellar");
                tbHour.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                tbHour.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                tbHour.Foreground = new SolidColorBrush(Colors.White);
                tbHour.Text = i.ToString();
                Grid.SetRow(tbHour, 0);
                Grid.SetColumn(tbHour, i);

                HeaderGrid.Children.Add(tbHour);
            }

            for (int i = 0; i < hoursPerDay; i++)
            {
                ColumnDefinition colHour = new ColumnDefinition();
                colHour.Width = new GridLength(1, GridUnitType.Star);
                FooterGrid.ColumnDefinitions.Add(colHour);


                TextBlock tbHour = new TextBlock();
                tbHour.FontWeight = FontWeights.Bold;
                tbHour.FontFamily = new FontFamily("Castellar");
                tbHour.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                tbHour.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                tbHour.Foreground = new SolidColorBrush(Colors.White);
                tbHour.Text = i.ToString();
                Grid.SetRow(tbHour, 0);
                Grid.SetColumn(tbHour, i);

                FooterGrid.Children.Add(tbHour);
            }

            ColumnDefinition colDate = new ColumnDefinition();
            colDate.Width = new GridLength(1, GridUnitType.Star);
            MainGrid.ColumnDefinitions.Add(colDate);

            ColumnDefinition colTime = new ColumnDefinition();
            colTime.Width = new GridLength(8, GridUnitType.Star);
            MainGrid.ColumnDefinitions.Add(colTime);

            ColumnDefinition colTotalTime = new ColumnDefinition();
            colTotalTime.Width = new GridLength(1, GridUnitType.Star);
            MainGrid.ColumnDefinitions.Add(colTotalTime);

            int index = 0;
            foreach (var item in PcOnOff.Keys.OrderBy(x=> x))
            {
                RowDefinition rowDef = new RowDefinition();
                rowDef.Height = new GridLength(29);
                MainGrid.RowDefinitions.Add(rowDef);

                TextBlock tbDate = new TextBlock();
                tbDate.Foreground = new SolidColorBrush(Colors.Black);
                //tbDate.FontFamily = new FontFamily("Castellar");
                tbDate.Text = item.ToString("dd.MM.yyy");
                tbDate.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                tbDate.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                Grid.SetRow(tbDate, index);
                Grid.SetColumn(tbDate, 0);
                MainGrid.Children.Add(tbDate);

                int totalSeconds = 0;
                foreach (TimeBlocks timeBlock in PcOnOff[item])
                {
                    Rectangle rect = new Rectangle();
                    SolidColorBrush brush = new SolidColorBrush(Colors.Brown);
                    //LinearGradientBrush brush = new LinearGradientBrush();
                    //brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0x00, 0xFF, 0x00), 0.0));
                    //brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x00, 0xFF, 0x00), 0.1));
                    rect.Fill = brush;
                    double duration = timeBlock.EntryStopTime.TimeOfDay.TotalSeconds - timeBlock.EntryStartTime.TimeOfDay.TotalSeconds;
                    totalSeconds += (int)duration;
                    double gridWidth = ((MainGrid.ActualWidth > 0) ? MainGrid.ActualWidth : this.Width) * 0.8;
                    double left = gridWidth * ((double)timeBlock.EntryStartTime.TimeOfDay.TotalSeconds / 86400);
                    double right = gridWidth * (1 - duration / 86400) - left;
                    rect.Margin = new Thickness(left, 4, right, 4);
                    Grid.SetRow(rect, index);
                    Grid.SetColumn(rect, 1);
                    MainGrid.Children.Add(rect);

                }
                TextBlock tbTotalTime = new TextBlock();
                //tbTotalTime.FontFamily = new FontFamily("Castellar");
                tbTotalTime.Foreground = new SolidColorBrush(Colors.Black);
                tbTotalTime.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                tbTotalTime.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                tbTotalTime.Text = (new TimeSpan(0, 0, totalSeconds)).ToString();
                Grid.SetRow(tbTotalTime, index);
                Grid.SetColumn(tbTotalTime, 2);
                MainGrid.Children.Add(tbTotalTime);
                index++;
            }
            if (this.ActualHeight > 250)
            {
                MainGridScrollBar.Height = RootGrid.ActualHeight - 2 * HeaderGrid.ActualHeight;
            }
            else
            {
                MainGridScrollBar.Height = this.Height - 250;
            }
        }
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawData();
        }
    }
}