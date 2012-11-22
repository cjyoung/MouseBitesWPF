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
            bw.RunWorkerAsync();
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            worker.RunWorkerAsync();
        }

        private void StopSearch_Click(object sender, RoutedEventArgs e)
        {
            worker.CancelAsync();
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MainVM.AppendStatusLog("===== thread completed =====");
            LaVie.MusicBox.PlayNote(MusicBox.Notes.F4, 200);
            LaVie.MusicBox.PlayNote(MusicBox.Notes.C4, 200);
        }

        private void LaunchSearch(object sender, DoWorkEventArgs e)
        {
            DateTime startTime = DateTime.Now;
            Dictionary<string, int> notes = new Dictionary<string, int>();

            MainVM.AppendStatusLog(new string('*', 25));
            MainVM.AppendStatusLog("Initializing...");
            LaVie.MusicBox.StartUpSong();

            string[] targetDates = { 
                            #region December
                                //December
                                "12/01/2012",
                                 "12/02/2012",
                                 "12/03/2012",
                                 "12/04/2012",
                                 "12/05/2012",
                                 "12/06/2012",
                                 "12/07/2012",
                                 "12/08/2012",
                                 "12/09/2012",
                                 "12/10/2012",
                                 "12/11/2012",
                                 "12/12/2012",
                                 "12/13/2012",
                                 "12/14/2012",
                                 "12/15/2012",
                                 "12/16/2012",
                                 "12/17/2012",
                                 "12/18/2012",
                                 "12/19/2012",
                                 "12/20/2012",
                                 "12/21/2012",
                                 "12/22/2012",
                                 "12/23/2012",
                                 "12/24/2012",
                                 "12/25/2012",
                                 "12/26/2012",
                                 "12/27/2012",
                                 "12/28/2012",
                                 "12/29/2012",
                                 "12/30/2012",
                                 "12/31/2012",
                            #endregion
                            #region January
                                //January
                                "01/01/2013",
                                 "01/02/2013",
                                 "01/03/2013",
                                 "01/04/2013",
                                 "01/05/2013",
                                 "01/06/2013",
                                 "01/07/2013",
                                 "01/08/2013",
                                 "01/09/2013",
                                 "01/10/2013",
                                 "01/11/2013",
                                 "01/12/2013",
                                 "01/13/2013",
                                 "01/14/2013",
                                 "01/15/2013",
                                 "01/16/2013",
                                 "01/17/2013",
                                 "01/18/2013",
                                 "01/19/2013",
                                 "01/20/2013",
                                 "01/21/2013",
                                 "01/22/2013",
                                 "01/23/2013",
                                 "01/24/2013",
                                 "01/25/2013",
                                 "01/26/2013",
                                 "01/27/2013",
                                 "01/28/2013",
                                 "01/29/2013",
                                 "01/30/2013",
                                 "01/31/2013",
                            #endregion
                            #region February 
                                //February
                                "02/01/2013",
                                "02/02/2013",
                                "02/03/2013",
                                "02/04/2013",
                                "02/05/2013",
                                "02/06/2013",
                                "02/07/2013",
                                "02/08/2013",
                                "02/09/2013",
                                "02/10/2013",
                                "02/11/2013",
                                "02/12/2013",
                                "02/13/2013",
                                "02/14/2013",
                                "02/15/2013",
                                "02/16/2013",
                                "02/17/2013",
                                "02/18/2013",
                                "02/19/2013",
                                "02/20/2013",
                                "02/21/2013",
                                "02/22/2013",
                                "02/23/2013",
                                "02/24/2013",
                                "02/25/2013",
                                "02/26/2013",
                                "02/27/2013",
                                "02/28/2013"
                            #endregion
                                //"04/01/2013"
                            };
            string convoId = "";
            SearchParameters sp = new SearchParameters();

            MainVM.AppendStatusLog("creating cookie jar");
            CookieContainer cookieJar = new CookieContainer();

            MainVM.AppendStatusLog("first request, to get cookies and initial info");
            string result = "";
            cookieJar = getCookiesFromRequest(cookieJar, sp, sp.rootUrl + sp.siteUrl, "", out result, "GET");


            MainVM.AppendStatusLog("conducting search");
            foreach (string searchDate in targetDates)
            {
                MainVM.AppendStatusLog(string.Format("searching on {0}...", searchDate));
                ConductSearch(notes, sp, searchDate, ref cookieJar, out convoId);
                if (worker.CancellationPending) return;
            }

            MainVM.AppendStatusLog("finished searching");
        }

        private void ConductSearch(Dictionary<string, int> notes, SearchParameters searchParameters, string targetDate, ref CookieContainer cookieJar, out string convoId)
        {
            targetDate = targetDate.Replace("/", "%2F");
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
                                                    MainVM.CurrentRestaurant.name,
                                                    MainVM.CurrentRestaurant.id,
                                                    targetDate,
                                                    MainVM.CurrentTime.id,
                                                    MainVM.CurrentPartySize.id);
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
            }
            MainVM.AppendStatusLog("getting results page");
            cookieJar = getCookiesFromRequest(cookieJar, searchParameters, searchParameters.rootUrl + redirectURL, postString, out result, "GET");

            MainVM.AppendStatusLog(new string('-', 25));
            string r = "";
            r = HtmlHelper.getTagContents(result, "SearchFailMessage", "div", "id").Trim();
            if (r.Length > 0) MainVM.AppendStatusLog(r);
            r = HtmlHelper.getTagContents(result, "reserveFormLabel", "label", "class").Trim();
            if (r.Length > 0) MainVM.AppendStatusLog(string.Format("Available Times: {0}", r));
            r = HtmlHelper.getTagContents(result, "alternativeTimesOptions", "p", "id").Trim();
            if (r.Length > 0) MainVM.AppendStatusLog(r);
            MainVM.AppendStatusLog(new string('-', 25));

            if (result.Contains("Sorry, we were unable to find available times."))
            {
                MainVM.AppendStatusLog("no times found");
                MainVM.AppendNotAvailableLog(string.Format("{0}", System.Web.HttpUtility.UrlDecode(targetDate)));
            }
            else
            {
                LaVie.MusicBox.PlayNote(LaVie.MusicBox.Notes.A4, 500);
                MainVM.AppendStatusLog("***** possible success *****");
                MainVM.AppendAvailableLog(string.Format("{0}", System.Web.HttpUtility.UrlDecode(targetDate)));
            }
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
                HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse();
                Stream responseStream = webResponse.GetResponseStream();
                StreamReader responseStreamReader = new StreamReader(responseStream);
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

            MainVM.AppendOutputLog("creating cookie jar");
            CookieContainer cookieJar = new CookieContainer();

            MainVM.AppendOutputLog("get cookies and initial info");
            string result = "";
            cookieJar = getCookiesFromRequest(cookieJar, sp, sp.rootUrl + sp.siteUrl, "", out result, "GET");

            string dineObject = HtmlHelper.findJSONObject(result, "WDPRO.dine");
            string viewObject = Regex.Replace(HtmlHelper.findJSONObject(dineObject, "\"view\""),"\"view\"[^:]*:","");

            MainVM.AppendOutputLog(viewObject);

            JavaScriptSerializer ser = new JavaScriptSerializer();
            MainVM.DineView = ser.Deserialize<DineSetting>(viewObject);

            string timesList = HtmlHelper.getTagContents(result, "makeResTime", "select", "id");
            MainVM.TimesList = HtmlHelper.ConvertOptionToObject(timesList);

            string partySizes = HtmlHelper.getTagContents(result, "makeResParty", "select", "id");
            MainVM.PartySizes = HtmlHelper.ConvertOptionToObject(partySizes);
        }
    }
}
