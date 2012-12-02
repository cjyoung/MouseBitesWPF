using LaVie.Libraries;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
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
        const bool DEBUG_MODE = true;

        public MainWindow()
        {
            InitializeComponent();
            worker.DoWork += new DoWorkEventHandler(LaunchSearch);
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.WorkerSupportsCancellation = true;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(LoadDropDownOptions);
            bw.RunWorkerCompleted += worker_RunWorkerCompleted;
            bw.WorkerSupportsCancellation = true;

            MainVM.AppendStatusLog("please wait, retrieving dates...");
            bw.RunWorkerAsync();
        }

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

        private void StopSearch_Click(object sender, RoutedEventArgs e)
        {
            worker.CancelAsync();
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MainVM.AppendStatusLog("===== thread completed =====");
            isSearching = false;
            //LaVie.MusicBox.PlayNote(MusicBox.Notes.F4, 200);
            //LaVie.MusicBox.PlayNote(MusicBox.Notes.C4, 200);
        }

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

        private void LaunchSearchInstance(object sender, DoWorkEventArgs e)
        {
            DateTime startTime = DateTime.Now;
            Dictionary<string, int> notes = new Dictionary<string, int>();

            MainVM.AppendStatusLog(new string('*', 25));
            MainVM.AppendStatusLog("Initializing...");
            //LaVie.MusicBox.StartUpSong();

            string convoId = "";
            SearchParameters sp = new SearchParameters();

            MainVM.AppendStatusLog("creating cookie jar");
            CookieContainer cookieJar = new CookieContainer();

            MainVM.AppendStatusLog("first request, to get cookies and initial info");
            string result = "";
            cookieJar = getCookiesFromRequest(cookieJar, sp, sp.rootUrl + sp.siteUrl, "", out result, "GET");


            MainVM.AppendStatusLog("conducting search");
            foreach (string searchDate in
                (
                    from d in MainVM.DatesList
                    where d.toSearch == true
                    select d.date.ToString("MM'/'dd'/'yyyy")
                ))
            {
                MainVM.AppendStatusLog(string.Format("searching on {0}...", searchDate));
                ConductSearch(notes, sp, searchDate, ref cookieJar, out convoId);
                if (worker.CancellationPending) return;
            }

            MainVM.AppendStatusLog("finished searching");
        }

        private void ConductSearch(Dictionary<string, int> notes, SearchParameters searchParameters, string targetDate, ref CookieContainer cookieJar, out string convoId)
        {
            string postString = string.Format("webBindCommandName=tableServiceSearchForm" +
                                                "&mode=async&_eventId=SubmitDiningSearch" +
                                                "&locations=" +
                                                "&cuisines=" +
                                                "&searchRestaurantName={0}" +
                                                "&searchRestaurantId={1}" +
                                                "&searchDate={2}" +
                                                "&times={3}" +
                                                "&allAvailableTimes=06%3A30%20am" +
                                                "&partySizes={4}" +
                                                "&_onlyShowDiningPlans=on" +
                                                "&WDW_SchEvts_Global_QQContDine_Link=Search%20for%20a%20Table" +
                                                "&mode=async",
                                                    System.Web.HttpUtility.UrlEncode(MainVM.CurrentRestaurant.name),
                                                    System.Web.HttpUtility.UrlEncode(MainVM.CurrentRestaurant.id),
                                                    System.Web.HttpUtility.UrlEncode(targetDate),
                                                    System.Web.HttpUtility.UrlEncode(MainVM.CurrentTime.id),
                                                    System.Web.HttpUtility.UrlEncode(MainVM.CurrentPartySize.id));
            string result;
            MainVM.StatusLog += "second request, to get conversation id: ";
            CookieContainer cookies = cookieJar;
            cookieJar = getCookiesFromRequest(cookies, searchParameters, searchParameters.rootUrl + searchParameters.siteUrl, postString, out result);
            convoId = Regex.Match(result, "ConversationId\":\"([^\"]*)\"").Groups[1].Value;
            MainVM.AppendStatusLog(string.Format("{0}", convoId));
            string nextURL = Regex.Match(result, "NextURL\":\"([^\"]*)\"").Groups[1].Value.Replace("\\/", "/");
            string redirectURL = "";
            while (redirectURL == "")
            {
                if (worker.CancellationPending) return;
                System.Threading.Thread.Sleep(1000);
                MainVM.AppendStatusLog("searching...");
                cookieJar = getCookiesFromRequest(cookieJar, searchParameters, searchParameters.rootUrl + nextURL, postString, out result, "GET", convoId);
                redirectURL = Regex.Match(result, "RedirectURL\":\"([^\"]*)\"").Groups[1].Value.Replace("\\/", "/");
                if (result == "error") redirectURL = "error";
            }

            if (redirectURL != "error")
            {
                MainVM.AppendStatusLog("getting results page");
                cookieJar = getCookiesFromRequest(cookieJar, searchParameters, searchParameters.rootUrl + redirectURL, postString, out result, "GET");

                MainVM.AppendStatusLog(new string('-', 25));
                string r = "";
                r = HtmlHelper.getTagContents(result, "SearchFailMessage", "div", "id").Trim();
                if (r.Length > 0) MainVM.AppendStatusLog(r);
                r = HtmlHelper.getTagContents(result, "reserveFormLabel", "label", "class").Trim();
                if (r.Length > 0) MainVM.AppendStatusLog(string.Format("Available Times: {0}", r));
                List<string> b = ParseAltTimes(result);
                if (b.Count() > 0) MainVM.AppendStatusLog(string.Format("Alt Times found: {0}", string.Join(", ", b)));
                MainVM.AppendStatusLog(new string('-', 25));

                if (result.Contains("Sorry, we were unable to find available times."))
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
                    if (b.Count > 0 && r.Length > 0)
                    {
                        b.Add(r);
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
            else
            {
                MainVM.AppendNotAvailableLog(string.Format("{0} (error)", System.Web.HttpUtility.UrlDecode(targetDate)));
            }
        }

        private List<string> ParseAltTimes(string result)
        {
            //reserveFormLabel
            List<string> altTimes = new List<string>();

            MatchCollection matches = Regex.Matches(result, "<label[^>]*class=['\"]reserveFormLabel['\"][^>]*>.*", RegexOptions.Singleline & RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                altTimes.Add(HtmlHelper.getTagContents(match.Groups[0].Value, "reserveFormLabel", "label", "class").Trim());
            }

            return altTimes;
        }

        private CookieContainer getCookiesFromRequest(CookieContainer cookieJar, SearchParameters searchParameters, string url, string postString, out string result, string method = "POST", string conversationid = "")
        {
            byte[] postBytes = Encoding.ASCII.GetBytes(postString);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            request.Referer = searchParameters.rootUrl + searchParameters.siteUrl;
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";
            request.CookieContainer = cookieJar;
            if (conversationid != "")
            {
                request.Headers.Add("X-Conversation-Id", conversationid);
                request.Headers.Add("X-Service-Request", "type=poll, attempt=1");
            }
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
                if (DEBUG_MODE) MainVM.AppendStatusLog("debug: starting http request");
                HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse();
                if (DEBUG_MODE) MainVM.AppendStatusLog(string.Format("debug: code: {0}; status: {1}", webResponse.StatusCode, webResponse.StatusDescription));
                Stream responseStream = webResponse.GetResponseStream();
                StreamReader responseStreamReader = new StreamReader(responseStream);
                if (DEBUG_MODE) MainVM.AppendStatusLog("debug: reading stream from response object");
                result = responseStreamReader.ReadToEnd();
                foreach (Cookie item in webResponse.Cookies)
                {
                    cookieJar.Add(item);
                }
                responseStream.Close();
                webResponse.Close();
            }
            catch (Exception ex)
            {
                MainVM.AppendStatusLog(string.Format("Error: {0}\nPress any key to continue", ex.Message));
                result = "error";
            }
            return cookieJar;
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

        private void LoadDropDownOptions(object sender, DoWorkEventArgs e)
        {
            SearchParameters sp = new SearchParameters();

            if (DEBUG_MODE) MainVM.AppendStatusLog("creating cookie jar");
            CookieContainer cookieJar = new CookieContainer();

            if (DEBUG_MODE) MainVM.AppendStatusLog("get cookies and initial info");
            string result = "";
            cookieJar = getCookiesFromRequest(cookieJar, sp, sp.rootUrl + sp.siteUrl, "", out result, "GET");

            if (DEBUG_MODE) MainVM.AppendStatusLog(string.Format("result size: {0}", result.Length));

            string dineObject = HtmlHelper.findJSONObject(result, "WDPRO.dine");
            string viewObject = Regex.Replace(HtmlHelper.findJSONObject(dineObject, "\"view\""), "\"view\"[^:]*:", "");

            if (DEBUG_MODE) MainVM.AppendStatusLog(viewObject);

            JavaScriptSerializer ser = new JavaScriptSerializer();
            MainVM.DineView = ser.Deserialize<DineSetting>(viewObject);

            string timesList = HtmlHelper.getTagContents(result, "makeResTime", "select", "id");
            MainVM.TimesList = HtmlHelper.ConvertOptionToObject(timesList);

            string partySizes = HtmlHelper.getTagContents(result, "makeResParty", "select", "id");
            MainVM.PartySizes = HtmlHelper.ConvertOptionToObject(partySizes);
        }

        private void ClearLogs_Click(object sender, RoutedEventArgs e)
        {
            MainVM.AvailableLog = "";
            MainVM.NotAvailableLog = "";
            MainVM.StatusLog = "";
        }
    }
}
