using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LaVie.Libraries
{
    static class HtmlHelper
    {
        public static string getTagContents(string results, string id, string tag, string property)
        {
            try
            {
                MatchCollection matches = Regex.Matches(results, "(<" + tag + "[^>]*" + property + "=\"" + id + "\"[^>]*>)(.*)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                string endOfPage = matches[0].Groups[2].Value;
                int endOfDiv = getTag(endOfPage, 0, 0, tag);

                return endOfPage.Substring(0, endOfDiv) + "\n";
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static int getTag(string code, int startFrom, int level, string tag)
        {
            int openDiv = code.IndexOf("<" + tag);
            int closeDiv = code.IndexOf("</" + tag);
            if (openDiv < closeDiv && openDiv > 0)
            { return getTag(code.Substring(openDiv + 1), startFrom + openDiv + 1, ++level, tag); }
            else if (level > 0)
            { return getTag(code.Substring(closeDiv + 1), startFrom + closeDiv + 1, --level, tag); }
            else
            { return closeDiv + startFrom; }
        }

        public static string findJSONObject(string results, string name)
        {
            try
            {
                MatchCollection matches = Regex.Matches(results, name + ".*", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                string endOfPage = matches[0].Groups[0].Value;
                int endOfDiv = getJSONBrackets(endOfPage, 0, 0);

                return endOfPage.Substring(0, endOfDiv) + "\n";
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static int getJSONBrackets(string code, int startFrom, int level, bool sub = false)
        {
            if (sub && level == 0)
            { return startFrom; }

            int openDiv = code.IndexOf("{");
            int closeDiv = code.IndexOf("}");
            if (openDiv < closeDiv && openDiv > 0)
            { return getJSONBrackets(code.Substring(openDiv + 1), startFrom + openDiv + 1, ++level, true); }
            else
            { return getJSONBrackets(code.Substring(closeDiv + 1), startFrom + closeDiv + 1, --level, true); }
        }

        public static ObservableCollection<DineOption> ConvertOptionToObject(string selectHtml)
        {
            ObservableCollection<DineOption> options = new ObservableCollection<DineOption>();

            MatchCollection mc = Regex.Matches(selectHtml, "(<option[^v]*value=['\"]([^'\"]*)['\"][^>]*>)([^<]*)</option>", RegexOptions.Singleline & RegexOptions.IgnoreCase);

            foreach (Match item in mc)
            {
                if (!item.Groups[2].Value.Contains("disabled"))
                {
                    DineOption newOption = new DineOption()
                        {
                            id = item.Groups[2].Value,
                            name = item.Groups[3].Value
                        };
                    options.Add(newOption);
                }
            }

            return options;
        }
    }
}
