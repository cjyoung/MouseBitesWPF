﻿using LaVie.Libraries;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaVie.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private bool _defaultToSearchValue = false;

        private ObservableCollection<DineOption> _PartySizes;
        public ObservableCollection<DineOption> PartySizes
        {
            get { return _PartySizes; }
            set
            {
                _PartySizes = value;
                OnPropertyChanged("PartySizes");
            }
        }

        private ObservableCollection<DineOption> _TimesList;
        public ObservableCollection<DineOption> TimesList
        {
            get { return _TimesList; }
            set
            {
                _TimesList = value;
                OnPropertyChanged("TimesList");
            }
        }

        private DineSetting _DineView;
        public DineSetting DineView
        {
            get { return _DineView; }
            set
            {
                _DineView = value;
                UpdateListOfDatesAvailableToSearch();
                OnPropertyChanged("DineView");
            }
        }

        private void UpdateListOfDatesAvailableToSearch()
        {
            DateTime mindate = DateTime.ParseExact(_DineView.search.minDate, "MMMM, dd, yyyy", System.Globalization.CultureInfo.InvariantCulture);
            //maxDate from DineView.search.maxDate is more than 180 days out, which gives errors when searching for times on them
            DateTime maxdate = DateTime.ParseExact(_DineView.search.maxGuestBookDate, "MMMM, dd, yyyy", System.Globalization.CultureInfo.InvariantCulture);
            DatesList = new ObservableCollection<SearchDate>(
                (from d in Enumerable.Range(0, (int)Math.Round(maxdate.Subtract(mindate).TotalDays + 1))
                .Select(offset => mindate.AddDays(offset))
                 select
                     new SearchDate { date = d.Date, toSearch = _defaultToSearchValue })
                .ToList());
        }

        private ObservableCollection<SearchDate> _DatesList;
        public ObservableCollection<SearchDate> DatesList
        {
            get { return _DatesList; }
            set
            {
                _DatesList = value;
                OnPropertyChanged("DatesList");
            }
        }

        private DineOption _CurrentRestaurant;
        public DineOption CurrentRestaurant
        {
            get { return _CurrentRestaurant; }
            set
            {
                _CurrentRestaurant = value;
                OnPropertyChanged("CurrentRestaurant");
            }
        }

        private DineOption _CurrentPartySize;
        public DineOption CurrentPartySize
        {
            get { return _CurrentPartySize; }
            set
            {
                _CurrentPartySize = value;
                OnPropertyChanged("CurrentPartySize");
            }
        }

        private DineOption _CurrentTime;
        public DineOption CurrentTime
        {
            get { return _CurrentTime; }
            set
            {
                _CurrentTime = value;
                OnPropertyChanged("CurrentTime");
            }
        }

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

        private string _OutputLog = String.Empty;
        public string OutputLog
        {
            get { return _OutputLog; }
            set
            {
                _OutputLog = value;
                OnPropertyChanged("OutputLog");

            }
        }

        internal void AppendOutputLog(string text)
        {
            _OutputLog += text + Environment.NewLine;
            OnPropertyChanged("OutputLog");
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