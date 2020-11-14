using GalaSoft.MvvmLight.Command;
using LiveCharts;
using SnmpAppWpf.Snmp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Timers;
using System.Windows.Input;

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

            snmpResolver = new SnmpResolver(IpAddress, Community);

            SetTimer();
        }
        #endregion

        #region Properties
        public ChartValues<double> InputValues { get; set; } = new ChartValues<double>();
        public ChartValues<double> OutputValues { get; set; } = new ChartValues<double>();
        public string[] Labels { get; set; } = new string[20];
        public Func<double, string> YFormatter { get; set; }

        private string ipAddress;
        public string IpAddress { get { return ipAddress; } set { ipAddress = value; OnPropertyChanged(nameof(IpAddress)); } }

        private string community;
        public string Community { get { return community; } set { community = value; OnPropertyChanged(nameof(Community)); } }

        private string machineName;
        public string MachineName { get { return machineName; } set { machineName = value; OnPropertyChanged(nameof(MachineName)); } }

        public string upTime;
        public string UpTime { get { return upTime; } set { upTime = value; OnPropertyChanged(nameof(UpTime)); } }

        private bool isRunning;
        public bool IsRunning { get { return isRunning; } set { isRunning = value; OnPropertyChanged(nameof(IsRunning)); } }

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

        public void StartStop()
        {
            snmpResolver.SetConfig(IpAddress, Community);
            aTimer.Enabled = !aTimer.Enabled;
        }

        private void LoadData()
        {
            var oidRowInOctets = snmpResolver.OidTable.FirstOrDefault(r => r.Description == "ifInOctets");
            var oidRowOutOctets = snmpResolver.OidTable.FirstOrDefault(r => r.Description == "ifOutOctets");

            if (oidRowInOctets != null && oidRowOutOctets != null)
            {
                Labels = Array.ConvertAll(oidRowInOctets.Results.ToArray(), (d) => $"{d.RequestDate.Hour}:{d.RequestDate.Minute}:{d.RequestDate.Second}");
                YFormatter = value => value.ToString();

                // adds to input octet
                double inOctets = Convert.ToDouble(oidRowInOctets.CurrentResult) -
                                    Convert.ToDouble(string.IsNullOrEmpty(oidRowInOctets.PreviousResult) ? "0" : oidRowInOctets.PreviousResult);
                InputValues.Add(inOctets);

                // adds to output octet
                double outOctets = Convert.ToDouble(oidRowOutOctets.CurrentResult) -
                                    Convert.ToDouble(string.IsNullOrEmpty(oidRowOutOctets.PreviousResult) ? "0" : oidRowOutOctets.PreviousResult);
                OutputValues.Add(outOctets);
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