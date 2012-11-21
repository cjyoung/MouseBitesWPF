using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaVie.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _StatusLog = String.Empty;
        public string StatusLog
        {
            get { return _StatusLog; }
            set
            {
                _StatusLog = value;
                OnPropertyChanged("StatusLog");
            }
        }

        internal void AppendStatusLog(string text)
        {
            _StatusLog += text + Environment.NewLine;
            OnPropertyChanged("StatusLog");
        }

        private string _AvailableLog = String.Empty;
        public string AvailableLog
        {
            get { return _AvailableLog; }
            set
            {
                _AvailableLog = value;
                OnPropertyChanged("AvailableLog");
            }
        }

        internal void AppendAvailableLog(string text)
        {
            _AvailableLog += text + Environment.NewLine;
            OnPropertyChanged("AvailableLog");
        }

        private string _NotAvailableLog = String.Empty;
        public string NotAvailableLog
        {
            get { return _NotAvailableLog; }
            set
            {
                _NotAvailableLog = value;
                OnPropertyChanged("NotAvailableLog");

            }
        }

        internal void AppendNotAvailableLog(string text)
        {
            _NotAvailableLog += text + Environment.NewLine;
            OnPropertyChanged("NotAvailableLog");
        }


        // Declare the event 
        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event 
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
