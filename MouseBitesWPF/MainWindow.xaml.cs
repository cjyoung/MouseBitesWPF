using LaVie.Libraries;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LaVie
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BackgroundWorker worker = new BackgroundWorker();
        bool isSearching = false;
        CookieContainer cookieJar = new CookieContainer();


        /// <summary>
        /// Constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            MainVM.Initialize();
            worker.DoWork += new DoWorkEventHandler(LaunchSearch);
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.WorkerSupportsCancellation = true;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(InitializeData);
            bw.RunWorkerCompleted += worker_RunWorkerCompleted;
            bw.WorkerSupportsCancellation = true;

            MainVM.AppendStatusLog("please wait, retrieving dates...");
            bw.RunWorkerAsync();
        }

        /// <summary>
        /// start async search thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            if (isSearching)
            {
                MessageBox.Show("A search is currently running, please stop the current one before starting a new one.");
            }
            else
            {
                if (MainVM.DatesList.Where(b => b.toSearch == true).Count() == 0
                        || Restaurants.SelectedItem == null
                        || PartySizes.SelectedItem == null
                        || Times.SelectedItem == null)
                    MessageBox.Show("Please make sure you've selected a date, restaurant, party size, and dining time before searching");
                else
                    worker.RunWorkerAsync();
            }
        }

        /// <summary>
        /// Cancel async search thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopSearch_Click(object sender, RoutedEventArgs e)
        {
            worker.CancelAsync();
        }

        /// <summary>
        /// clean up after thread has completed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MainVM.AppendStatusLog("===== thread completed =====");
            isSearching = false;
            //LaVie.MusicBox.PlayNote(MusicBox.Notes.F4, 200);
            //LaVie.MusicBox.PlayNote(MusicBox.Notes.C4, 200);
        }

        /// <summary>
        /// engine for searching, executed as a new thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LaunchSearch(object sender, DoWorkEventArgs e)
        {
            isSearching = true;

            if (!MainVM.RepeatSearch)
            {
                MainVM.AvailableLog = "";
                MainVM.NotAvailableLog = "";
                LaunchSearchInstance(sender, e);
            }
            else
            {
                while (!worker.CancellationPending)
                {
                    MainVM.AvailabilityLogToSend = "";
                    MainVM.AppendAvailableLog("--new search--");
                    MainVM.AppendNotAvailableLog("--new search--");
                    LaunchSearchInstance(sender, e);

                    if (MainVM.SendEmail && MainVM.AvailabilityLogToSend.Length > 0)
                    {
                        EmailHelper.SendEmail(MainVM.EmailLogin, EmailPassword.Password, MainVM.AvailabilityLogToSend);
                        MainVM.AppendStatusLog("email sent");
                    }

                    for (int i = 0; i < MainVM.RepeatSearchAmount && !worker.CancellationPending; i++)
                    {
                        MainVM.AppendStatusLog(string.Format("pausing before next search... {0} mins",
                                                            MainVM.RepeatSearchAmount - i));
                        for (int j = 0; j < 60 && !worker.CancellationPending; j++)
                        {
                            System.Threading.Thread.Sleep(1000);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// actual search event, handles calls to other functions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LaunchSearchInstance(object sender, DoWorkEventArgs e)
        {
            DateTime startTime = DateTime.Now;
            Dictionary<string, int> notes = new Dictionary<string, int>();

            MainVM.AppendStatusLog(new string('*', 25));
            MainVM.AppendStatusLog("Initializing...");
            //LaVie.MusicBox.StartUpSong();

            //string convoId = "";

            MainVM.AppendStatusLog("creating cookie jar");

            MainVM.AppendStatusLog("first request, to get cookies and initial info");
            string result = getCookiesFromRequest(SearchParameters.rootUrl + SearchParameters.siteUrl, "", "GET");

            string pep_csrf = "";
            Match match = Regex.Match(result, "<input[^>]*name=['\"]pep_csrf['\"][^>]*value=['\"]([^'\"]*)['\"]*[^>]>", RegexOptions.Singleline & RegexOptions.IgnoreCase);
            pep_csrf = match.Groups[1].ToString();

            MainVM.AppendStatusLog("conducting search");
            foreach (string searchDate in
                (
                    from d in MainVM.DatesList
                    where d.toSearch == true
                            && DateTime.Compare(d.date, DateTime.Now) >= 0 //only search dates in the future
                    //select d.date.ToString("MM'/'dd'/'yyyy")
                    select d.date.ToString("yyyy-MM-dd") //updating to match new date format
                ))
            {
                MainVM.AppendStatusLog(string.Format("searching on {0}...", searchDate));
                ConductSearch(searchDate, pep_csrf);
                //performSearch(SearchType.TableService);
                if (worker.CancellationPending) return;
            }

            MainVM.AppendStatusLog("finished searching");
        }

        private void ConductSearch(string targetDate, string pep_csrf)
        {
            string postString = string.Format("&searchDate={1}" +
                                                "&skipPricing=true" +
                                                "&searchTime={2}" +
                                                "&partySize={3}" +
                                                "&id={0}%3BentityType%3Drestaurant" +
                                                "&type=dining" +
                                                "&pep_csrf={4}",
                                                    System.Web.HttpUtility.UrlEncode(MainVM.CurrentRestaurant.id),
                                                    System.Web.HttpUtility.UrlEncode(targetDate),
                                                    System.Web.HttpUtility.UrlEncode(MainVM.CurrentTime.id),
                                                    System.Web.HttpUtility.UrlEncode(MainVM.CurrentPartySize.id),
                                                    pep_csrf);

            MainVM.AppendStatusLog("getting results page");
            string result = getCookiesFromRequest(SearchParameters.rootUrl + SearchParameters.diningSearchUrl, postString, "POST");

            MainVM.AppendStatusLog(new string('-', 25));

            //NOTE - the style of the link will have "selected" if it's a direct hit; we're not going
            //  to try to get that to work right now - for now we're just going to return results
            //string r = "";
            //r = HtmlHelper.getTagContents(result, "SearchFailMessage", "div", "id").Trim();
            //if (r.Length > 0) MainVM.AppendStatusLog(r);
            //r = HtmlHelper.getTagContents(result, "reserveFormLabel", "label", "class").Trim();
            //if (r.Length > 0) MainVM.AppendStatusLog(string.Format("Available Times: {0}", r));
            List<string> b = ParseAltTimes(result);
            if (b.Count() > 0) MainVM.AppendStatusLog(string.Format(/*"Alt Times found: {0}"*/"Available Times: {0}", string.Join(", ", b)));
            MainVM.AppendStatusLog(new string('-', 25));

            if (result.Contains("data-hasavailability=\"\""))
            {
                MainVM.AppendStatusLog("no times found");
                MainVM.AppendNotAvailableLog(string.Format("{0}", System.Web.HttpUtility.UrlDecode(targetDate)));
            }
            else
            {
                //LaVie.MusicBox.PlayNote(LaVie.MusicBox.Notes.A4, 500);
                MainVM.AppendStatusLog("***** possible success *****");
                MainVM.AppendAvailableLog(string.Format("{0}", System.Web.HttpUtility.UrlDecode(targetDate)));
                MainVM.AppendAvailabilityLogToSend(string.Format("{0}", System.Web.HttpUtility.UrlDecode(targetDate)));
                if (b.Count > 0/* && r.Length > 0*/)
                {
                    //b.Add(r);
                    foreach (string time in
                        (from a in b.Distinct()
                         orderby DateTime.Parse(a) ascending
                         select a))
                    {
                        MainVM.AppendAvailableLog(string.Format("- {0}", time));
                        MainVM.AppendAvailabilityLogToSend(string.Format("- {0}", time));
                    }
                }

            }
        }

        private List<string> ParseAltTimes(string result)
        {
            //reserveFormLabel
            List<string> altTimes = new List<string>();

            MatchCollection matches = Regex.Matches(result, "<span[^>]*class=['\"]buttonText['\"][^>]*>.*", RegexOptions.Singleline & RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                altTimes.Add(HtmlHelper.getTagContents(match.Groups[0].Value, "buttonText", "span", "class").Trim());
            }

            return altTimes;
        }

        private String getCookiesFromRequest(string url, string postString, string method = "POST")
        {
            String result = "";

            byte[] postBytes = Encoding.ASCII.GetBytes(postString);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            request.Referer = SearchParameters.rootUrl + SearchParameters.siteUrl;
            //request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";
            request.CookieContainer = cookieJar;
            if (method == "POST")
            {
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = postBytes.Length;
                request.Headers.Add("Pragma", "no-cache");
                Stream postStream = request.GetRequestStream();
                postStream.Write(postBytes, 0, postBytes.Length);
                postStream.Close();
            }
            try
            {
                if (MainVM.VerboseLogging) MainVM.AppendStatusLog("debug: starting http request");
                HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse();
                if (MainVM.VerboseLogging) MainVM.AppendStatusLog(string.Format("debug: code: {0}; status: {1}", webResponse.StatusCode, webResponse.StatusDescription));
                Stream responseStream = webResponse.GetResponseStream();
                StreamReader responseStreamReader = new StreamReader(responseStream);
                if (MainVM.VerboseLogging) MainVM.AppendStatusLog("debug: reading stream from response object");
                result = responseStreamReader.ReadToEnd();
                if (result.Contains("systemErrorMessageTitle")) throw new Exception(HtmlHelper.getTagContents(result, "systemErrorMessageTitle", "h4", "id"));
                foreach (Cookie item in webResponse.Cookies)
                {
                    cookieJar.Add(item);
                }
                responseStream.Close();
                webResponse.Close();
            }
            catch (Exception ex)
            {
                MainVM.AppendStatusLog(string.Format("Error: {0}", ex.Message));
                result = "error";
            }
            return result;
        }

        private void Status_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            //tb.CaretIndex = tb.Text.Length;
            tb.ScrollToEnd();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            worker.CancelAsync();
            Application.Current.Shutdown();
        }

        /// <summary>
        /// gets options from dining site and uses this information to create the option lists
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InitializeData(object sender, DoWorkEventArgs e)
        {
            if (MainVM.VerboseLogging) MainVM.AppendStatusLog("getting initial info");

            //get cookie jar
            CookieContainer cookieJar = getNewCookieCollection();

            string result = getOptionsFromSite();

            if (MainVM.VerboseLogging) MainVM.AppendStatusLog(string.Format("result size: {0}", result.Length));

            if (result.Contains("partySize"))
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(DineRestaurants));

                string timesList = HtmlHelper.getTagContents(result, "diningAvailabilityForm-searchTime", "select", "id");
                MainVM.TimesList = HtmlHelper.ConvertOptionToObject(timesList);

                string partySizes = HtmlHelper.getTagContents(result, "partySize", "select", "id");
                MainVM.PartySizes = HtmlHelper.ConvertOptionToObject(partySizes);

                string restaurants = getRestaurantListFromSite();
                if (restaurants != "error")
                {
                    MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(restaurants));
                    DineRestaurants restaurantsObject = ser.ReadObject(ms) as DineRestaurants;
                    MainVM.Restaurants = new ObservableCollection<DineOption>
                        (from rest in restaurantsObject.restaurants
                         orderby rest.name
                         select rest);

                    MainVM.LoadingMessage = "";
                    MainVM.LoadingMessageVisibility = System.Windows.Visibility.Hidden;
                }
                else
                {
                    MainVM.LoadingMessage = "Error";
                    MainVM.AppendStatusLog("Error: could not retrieve date and restaurant info");
                }
            }
            else
            {
                MainVM.LoadingMessage = "Error";
                MainVM.AppendStatusLog("Error: could not retrieve date and restaurant info");
            }
        }

        /// <summary>
        /// Clear logs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearLogs_Click(object sender, RoutedEventArgs e)
        {
            MainVM.AvailableLog = "";
            MainVM.NotAvailableLog = "";
            MainVM.StatusLog = "";
        }

        /// <summary>
        /// Creates a new Cookie Collection from site
        /// </summary>
        /// <returns>New Cookie Collection from site</returns>
        private CookieContainer getNewCookieCollection()
        {
            CookieContainer cookieJar = new CookieContainer();
            cookieJar = new CookieContainer();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(SearchParameters.rootUrl + SearchParameters.siteUrl);
            request.Method = "GET";
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";
            request.CookieContainer = cookieJar;
            try
            {
                if (MainVM.VerboseLogging) MainVM.AppendStatusLog("debug: starting http request");
                HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse();
                if (MainVM.VerboseLogging) MainVM.AppendStatusLog(string.Format("debug: code: {0}; status: {1}", webResponse.StatusCode, webResponse.StatusDescription));
                Stream responseStream = webResponse.GetResponseStream();
                StreamReader responseStreamReader = new StreamReader(responseStream);
                if (MainVM.VerboseLogging) MainVM.AppendStatusLog("debug: reading in response");
                String result = responseStreamReader.ReadToEnd();
                if (result.Contains("systemErrorMessageTitle")) throw new Exception(HtmlHelper.getTagContents(result, "systemErrorMessageTitle", "h4", "id"));

                if (MainVM.VerboseLogging) MainVM.StatusLog += "debug: adding cookies";
                foreach (Cookie item in webResponse.Cookies)
                {
                    if (MainVM.VerboseLogging) MainVM.StatusLog += ".";
                    cookieJar.Add(item);
                }
                if (MainVM.VerboseLogging) MainVM.StatusLog += "\n";
                responseStream.Close();
                webResponse.Close();
            }
            catch (Exception ex)
            {
                MainVM.AppendStatusLog(string.Format("Error: {0}", ex.Message));
            }

            return cookieJar;
        }

        /// <summary>
        /// Get initial options from the web site
        /// </summary>
        /// <returns>string of the result</returns>
        private String getOptionsFromSite()
        {
            return GetRequest(SearchParameters.rootUrl + SearchParameters.siteUrl);
        }

        /// <summary>
        /// Get restaurant list from the web site
        /// </summary>
        /// <returns>string of the result</returns>
        private String getRestaurantListFromSite()
        {
            //to debug - bypass request and load from text files
            //return System.IO.File.ReadAllText("Files/restaurant.json", Encoding.Default);
            return GetRequest(SearchParameters.rootUrl + SearchParameters.restaurantListUrl);
        }

        /// <summary>
        /// generic function for GET requests
        /// </summary>
        /// <param name="URL">URL to get</param>
        /// <returns>String of the response</returns>
        private String GetRequest(String URL, bool ticketRequest = false)
        {
            String result = "";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.Referer = SearchParameters.rootUrl + SearchParameters.siteUrl;
            request.Method = "GET";
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";
            request.CookieContainer = cookieJar;

            if (!ticketRequest)
            {
                //adding in authticket - maybe this is needed for the api calls again?
                AuthTicket ticket = new AuthTicket();
                try
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(AuthTicket));

                    string authResult = GetRequest(SearchParameters.rootUrl + SearchParameters.authServerUrl, true);

                    MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(authResult));
                    ticket = ser.ReadObject(ms) as AuthTicket;
                }
                catch (Exception ex)
                {
                    MainVM.AppendStatusLog(string.Format("Error: {0}", ex.Message));
                    result = "error";
                }

                //toss the auth ticket into the mix
                request.Headers.Add(HttpRequestHeader.Authorization.ToString(), String.Format("BEARER {0}", ticket.accessToken));
            }

            try
            {
                if (MainVM.VerboseLogging) MainVM.AppendStatusLog("debug: starting http request");
                HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse();
                if (MainVM.VerboseLogging) MainVM.AppendStatusLog(string.Format("debug: code: {0}; status: {1}", webResponse.StatusCode, webResponse.StatusDescription));
                Stream responseStream = webResponse.GetResponseStream();
                StreamReader responseStreamReader = new StreamReader(responseStream);
                if (MainVM.VerboseLogging) MainVM.AppendStatusLog("debug: reading stream from response object");
                result = responseStreamReader.ReadToEnd();
                if (result.Contains("systemErrorMessageTitle")) throw new Exception(HtmlHelper.getTagContents(result, "systemErrorMessageTitle", "h4", "id"));
                responseStream.Close();
                webResponse.Close();
            }
            catch (Exception ex)
            {
                MainVM.AppendStatusLog(string.Format("Error: {0}", ex.Message));
                result = "error";
            }
            return result;
        }

        private void ReinitData_Click(object sender, RoutedEventArgs e)
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(InitializeData);
            bw.RunWorkerCompleted += worker_RunWorkerCompleted;
            bw.WorkerSupportsCancellation = true;

            MainVM.AppendStatusLog("please wait, retrieving dates...");
            bw.RunWorkerAsync();
        }

        /// <summary>
        /// Perform a search of the provided type
        /// </summary>
        /// <param name="searchType">Enum of which type of search to perform</param>
        /// <returns>String of result</returns>
        //private String performMultiSearch(SearchType searchType)
        //{
        //    String result = "";
        //    //TODO - construct from request data
        //    String postString = "searchDate=2013-04-30&searchTime=19%3A00&partySize=2&skipPricing=true";

        //    //get new auth ticket
        //    AuthTicket ticket = new AuthTicket();
        //    try
        //    {
        //        DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(AuthTicket));

        //        string authResult = GetRequest(SearchParameters.rootUrl + SearchParameters.authServerUrl);

        //        MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(authResult));
        //        ticket = ser.ReadObject(ms) as AuthTicket;
        //    }
        //    catch (Exception ex)
        //    {
        //        MainVM.AppendStatusLog(string.Format("Error: {0}", ex.Message));
        //        result = "error";
        //    }

        //    byte[] postBytes = Encoding.ASCII.GetBytes(postString);
        //    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(SearchParameters.rootUrl + searchType.ToString());
        //    request.Method = "POST";
        //    request.Referer = SearchParameters.rootUrl + SearchParameters.siteUrl;
        //    request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        //    request.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";
        //    request.CookieContainer = cookieJar;

        //    request.Headers.Add(HttpRequestHeader.Authorization.ToString(), String.Format("BEARER {0}", ticket.accessToken));

        //    if (request.Method == "POST")
        //    {
        //        request.ContentType = "application/x-www-form-urlencoded";
        //        request.ContentLength = postBytes.Length;
        //        request.Headers.Add("Pragma", "no-cache");
        //        Stream postStream = request.GetRequestStream();
        //        postStream.Write(postBytes, 0, postBytes.Length);
        //        postStream.Close();
        //    }
        //    try
        //    {
        //        if (MainVM.VerboseLogging) MainVM.AppendStatusLog("debug: starting http request");
        //        HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse();
        //        if (MainVM.VerboseLogging) MainVM.AppendStatusLog(string.Format("debug: code: {0}; status: {1}", webResponse.StatusCode, webResponse.StatusDescription));
        //        Stream responseStream = webResponse.GetResponseStream();
        //        StreamReader responseStreamReader = new StreamReader(responseStream);
        //        if (MainVM.VerboseLogging) MainVM.AppendStatusLog("debug: reading stream from response object");
        //        result = responseStreamReader.ReadToEnd();
        //        if (result.Contains("systemErrorMessageTitle")) throw new Exception(HtmlHelper.getTagContents(result, "systemErrorMessageTitle", "h4", "id"));
        //        foreach (Cookie item in webResponse.Cookies)
        //        {
        //            cookieJar.Add(item);
        //        }
        //        responseStream.Close();
        //        webResponse.Close();
        //    }
        //    catch (Exception ex)
        //    {
        //        MainVM.AppendStatusLog(string.Format("Error: {0}", ex.Message));
        //        result = "error";
        //    }
        //    return result;
        //}
    }
}
