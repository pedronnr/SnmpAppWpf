using GalaSoft.MvvmLight.Command;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using SnmpAppWpf.Snmp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SnmpAppWpf
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        #region Variables
        private SnmpResolver snmpResolver;
        private Timer aTimer;
        #endregion

        #region Ctors
        public MainWindowViewModel()
        {
            IpAddress = "192.168.1.24";
            Community = "public";
            Text = "Start";

            Initialize();

            snmpResolver = new SnmpResolver(IpAddress, Community);

            SetTimer();
        }
        #endregion

        #region Properties
        public Func<double, string> XFormatter { get; set; }
        public Func<double, string> YFormatter { get; set; }

        private string ipAddress;
        public string IpAddress { get { return ipAddress; } set { ipAddress = value; OnPropertyChanged(nameof(IpAddress)); } }

        private string community;
        public string Community { get { return community; } set { community = value; OnPropertyChanged(nameof(Community)); } }

        private string machineName;
        public string MachineName { get { return machineName; } set { machineName = value; OnPropertyChanged(nameof(MachineName)); } }

        private string upTime;
        public string UpTime { get { return upTime; } set { upTime = value; OnPropertyChanged(nameof(UpTime)); } }

        private string text;
        public string Text { get { return text; } set { text = value; OnPropertyChanged(nameof(Text)); } }

        public SeriesCollection Series { get; set; }

        public IList<string> Logs { get; set; } = new List<string>();
        #endregion

        #region Methods
        private void SetTimer()
        {
            aTimer = new Timer(1000);
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = false;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            GetSnmp();
            LoadData();
        }

        public ICommand StartStopCommand
        {
            get { return new RelayCommand(StartStop); }
        }

        private void Initialize()
        {
            XFormatter = d => new DateTime((long)d).ToString("HH:mm:ss");
            YFormatter = value => value.ToString();

            var gradientBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };
            gradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(33, 148, 241), 0));
            gradientBrush.GradientStops.Add(new GradientStop(Colors.Transparent, 1));

            Series = new SeriesCollection()
            {
                new LineSeries
                {
                    StrokeThickness = 2,
                    Title = "In Octets",
                    Stroke = Brushes.Red,
                    LineSmoothness = 1,
                    PointGeometrySize = 10,
                    Fill = gradientBrush,
                    Values = new ChartValues<DateTimePoint>()
                },
                new LineSeries
                {
                    StrokeThickness = 2,
                    Title = "Out Octets",
                    Stroke = Brushes.Blue,
                    LineSmoothness = 1,
                    PointGeometrySize = 10,
                    Fill = gradientBrush,
                    Values = new ChartValues<DateTimePoint>()
                }
            };
        }

        public void StartStop()
        {
            snmpResolver.SetConfig(IpAddress, Community);
            aTimer.Enabled = !aTimer.Enabled;
            Text = aTimer.Enabled ? "Stop" : "Start";
        }

        private void LoadData()
        {
            MachineName = snmpResolver.OidTable.FirstOrDefault(r => r.Description == "sysDescr")?.CurrentResult;
            UpTime = snmpResolver.OidTable.FirstOrDefault(r => r.Description == "sysUpTime")?.CurrentResult;

            var oidRowInOctets = snmpResolver.OidTable.FirstOrDefault(r => r.Description == "ifInOctets");
            var oidRowOutOctets = snmpResolver.OidTable.FirstOrDefault(r => r.Description == "ifOutOctets");

            if (oidRowInOctets != null && oidRowOutOctets != null)
            {
                // adds to input octet
                double inOctets = string.IsNullOrEmpty(oidRowInOctets.PreviousResult) ? 0 :
                                   Convert.ToDouble(oidRowInOctets.CurrentResult) - Convert.ToDouble(oidRowInOctets.PreviousResult);

                Series[0].Values.Add(new DateTimePoint(oidRowInOctets.CurrentDate, inOctets));

                // adds to output octet
                double outOctets = string.IsNullOrEmpty(oidRowOutOctets.PreviousResult) ? 0 :
                                    Convert.ToDouble(oidRowOutOctets.CurrentResult) - Convert.ToDouble(oidRowOutOctets.PreviousResult);
                Series[1].Values.Add(new DateTimePoint(oidRowOutOctets.CurrentDate, outOctets));
            }
        }

        public void GetSnmp()
        {
            snmpResolver.Get();
        }
        #endregion

        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}