using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LaVie.Libraries
{
    static class SearchParameters
    {
        static internal string rootUrl = "https://disneyworld.disney.go.com";
        static internal string siteUrl = "/dining/";

        static internal string restaurantListUrl = "/api/wdpro/bulk-service/snapshot/WDW-finder-restaurant";
        static internal string authServerUrl = "/authentication/get-client-token/";
        static internal string diningSearchUrl = "/finder/dining-availability/";

        static internal int minDate = 0;
        static internal int maxDate = 180;
        static internal string dateFormat = "YYYY-MM-DD";
    }

    //type-safe-enum pattern
    public sealed class SearchType
    {
        private readonly String name;
        private readonly int value;

        public static readonly SearchType TableService = new SearchType(1, "/api/wdpro/availability-service/destinations/80007798/grouped-table-service-availability");
        public static readonly SearchType DinnerShow = new SearchType(2, "/api/wdpro/availability-service/destinations/80007798/grouped-dinner-show-availability");
        public static readonly SearchType DiningEvent = new SearchType(3, "/api/wdpro/availability-service/destinations/80007798/grouped-dining-event-availability");

        private SearchType(int value, String name)
        {
            this.name = name;
            this.value = value;
        }

        public override String ToString()
        {
            return name;
        }

    }
}
