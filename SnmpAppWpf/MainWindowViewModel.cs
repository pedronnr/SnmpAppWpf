using LiveCharts;
using SnmpAppWpf.Snmp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Timers;

namespace SnmpAppWpf
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        #region Variables
        private SnmpResolver snmpResolver;
        private Timer aTimer;
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Ctors
        public MainWindowViewModel()
        {
            snmpResolver = new SnmpResolver(IpAddress, Community);

            SetTimer();
        }
        #endregion

        #region Properties
        public ChartValues<double> InputValues { get; set; } = new ChartValues<double>();
        public ChartValues<double> OutputValues { get; set; } = new ChartValues<double>();
        public string[] Labels { get; set; } = new string[20];
        public Func<double, string> YFormatter { get; set; }
        public string IpAddress { get; set; } = "192.168.1.24";
        public string Community { get; set; } = "public";
        public bool IsRunning { get; set; }
        public IList<string> Logs { get; set; } = new List<string>();
        #endregion

        #region Methods
        private void SetTimer()
        {
            aTimer = new Timer(1000);
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            GetSnmp();
            LoadData();
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
        // Create the OnPropertyChanged method to raise the event
        // The calling member's name will be used as the parameter.
        //protected void OnPropertyChanged([CallerMemberName] string name = null)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        //}
        #endregion
    }
}