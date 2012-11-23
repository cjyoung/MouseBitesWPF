using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace LaVie.Libraries
{
    public class DineSetting
    {
        public DineSettingSearch search { get; set; }
        public DineSettingRestaurants restaurants { get; set; }
    }

    public class DineSettingSearch
    {
        public string minDate { get; set; }
        public string maxDate { get; set; }
        public string maxGuestBookDate { get; set; }
        public string defaultDate { get; set; }
        public ObservableCollection<string> blockOutDates { get; set; }
    }

    public class DineSettingRestaurants
    {
        public ObservableCollection<string> schema { get; set; }
        public ObservableCollection<DineOption> list { get; set; }
    }

    public class DineOption
    {
        public string name { get; set; }
        public string id { get; set; }
    }

    public class SearchDate
    {
        public bool toSearch { get; set; }
        public DateTime date { get; set; }
    }
}
