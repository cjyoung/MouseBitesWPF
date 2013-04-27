using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace LaVie.Libraries
{
    public class SearchDate
    {
        public bool toSearch { get; set; }
        public DateTime date { get; set; }
    }


    [DataContract]
    public class DineRestaurants
    {
        [DataMember(Name = "entries")]
        public ObservableCollection<DineOption> restaurants { get; set; }
    }

    [DataContract]
    public class DineOption
    {
        private string _id;

        [DataMember(Name = "id")]
        public string id
        {
            get { return _id; }
            set { _id = value.Split(';')[0]; }
        }

        [DataMember(Name = "name")]
        public string name { get; set; }
    }

    [DataContract]
    public class AuthTicket
    {
        [DataMember(Name = "access_token")]
        public string accessToken { get; set; }

        [DataMember(Name = "expires_in")]
        public string expiresIn { get; set; }
    }

}
