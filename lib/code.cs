using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

// Some code for parsing hacker news content in .NET standard 2.0
namespace Parse.HackerNews
{
    // Link / discussion thread
    public class Thread
    {
        public string id;
        public DateTime submitTime;
        
        public string url;
        public string title;
        public string submittedBy;

        public int points;
        public int comments;
    }

    public class Comment
    {
        public string id;
        public string user;
        public DateTime submitTime;
        public string comment;
    }

    public static class Json
    {
        public static string str(string name, string val, bool last)
            => "\"" + name + "\": \"" + val.Replace("\"", "\\\"").Replace("\n", "\\n") + "\"" + (!last ? "," : "") + "\n";
        public static string i(string name, string val, bool last)
            => "\"" + name + "\": " + val + (!last ? "," : "") + "\n";
        public static string d(string name, DateTime date, bool last)
            => "\"" + name + "\": \"" + date.ToString("s", System.Globalization.CultureInfo.InvariantCulture) + "\"" + (!last ? "," : "") + "\n";
    }

    public static class Ext
    {
        public static string ToJson(this IEnumerable<Thread> threads)
        {
            var sb = new System.Text.StringBuilder();
            var list = threads.ToList();
            var c = list.Count;
            sb.Append("{\n");
            sb.Append("  \"threads\": [\n");
            for(var i=0;i<c;i++)
            {
                var t = list[i];
                sb.Append("  {\n");
                sb.Append("    "+Json.i("id", t.id, false));
                sb.Append("    "+Json.str("url", t.url, false));
                sb.Append("    "+Json.str("submittedBy", t.submittedBy, false));
                sb.Append("    "+Json.d("submitTime", t.submitTime, false));
                sb.Append("    "+Json.i("score", t.points.ToString(), false));
                sb.Append("    " + Json.i("comments", t.comments.ToString(),true));
                sb.Append("  }");
                if (i != c - 1)
                    sb.Append(",");
                sb.Append("\n");
            }
            sb.Append("  ]\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        public static string ToJson(this IEnumerable<Comment> comments)
        {
            var sb = new System.Text.StringBuilder();
            var list = comments.ToList();
            var co = list.Count;
            sb.Append("{\n");
            sb.Append("  \"comments\": [\n");
            for (var i = 0; i < co; i++)
            {
                var c = list[i];
                sb.Append("  {\n");
                sb.Append("    " + Json.i("id", c.id, false));
                sb.Append("    " + Json.str("user", c.user, false));
                sb.Append("    " + Json.d("submitTime", c.submitTime, false));
                sb.Append("    " + Json.str("comment", c.comment, true));
                sb.Append("  }");
                if (i != co - 1)
                    sb.Append(",");
                sb.Append("\n");
            }
            sb.Append("  ]\n");
            sb.Append("}\n");
            return sb.ToString();
        }
    }

    public class ParseUtils
    {
        public static string ParseBetween(string str, int idx, string start, string end)
        {
            var slen = start.Length;
            var elen = end.Length;
            var i = str.IndexOf(start, idx) + slen;
            return str.Substring(i, str.IndexOf(end, i) - i);
        }

        public static int Mon(string s)
        {
            switch(s)
            {
                case "Jan": return 1;
                case "Feb": return 2;
                case "Mar": return 3;
                case "Apr": return 4;
                case "May": return 5;
                case "June": return 6;
                case "July": return 7;
                case "Aug": return 8;
                case "Sept": return 9;
                case "Oct": return 10;
                case "Nov": return 11;
                case "Dec": return 12;
                default: throw new Exception("Unable to parse month:" + s);
            }
        }

        public static DateTime? AgeString(string agostr)
        {
            
            if (agostr.Contains("on") && !agostr.Contains("month"))
            {
                var s = agostr.Split(' ');
                var imon = Mon(s[1]);
                var iday = Int32.Parse(s[2].Replace(",",""));
                var iyear = Int32.Parse(s[3]);
                return new DateTime(iyear, imon, iday);
            }

            // Yes, there is some error margin here, not possible to get the exact timestamp as far as I know...
            var agonum = Int32.Parse(agostr.Substring(0, agostr.IndexOf(" ")));
            if (agostr.Contains("minute"))
                return DateTime.Now.AddMinutes(-agonum);
            else if (agostr.Contains("hour"))
                return DateTime.Now.AddHours(-agonum);
            else if (agostr.Contains("day"))
                return DateTime.Now.AddDays(-agonum);
            else if (agostr.Contains("month"))
                return DateTime.Now.AddMonths(-agonum);
            return null;
        }
    }

    public class HNParser
    {

        public static string GetMoreHref(string html)
        {
            var idx = html.IndexOf("class=\"morelink\"");
            if (idx == -1)
                return null;
            var hrefidx = html.LastIndexOf("href=", idx);
            var href = ParseUtils.ParseBetween(html, hrefidx, "href=\"", "\"");
            return href.Replace("&amp;", "&");
        }

        // CSS classes to denote comment score, more downed = more gray
        private static readonly string[] textColors = new string[] { "c00", "c5a", "c73", "c9c" };
        public static string ParseComment(string html, int idxage, out int idxcom)
        {
            idxcom = -1;
            foreach (var s in textColors)
            {
                idxcom = html.IndexOf("commtext " + s, idxage);
                if (idxcom != -1)
                    break;
            }

            return ParseUtils.ParseBetween(html, idxcom, "\">", "</span>");
        }

        public static List<Comment> ParseComments(string html)
        {
            var idx = html.IndexOf("class='comment-tree'", 0);
            if (idx == -1)
                idx = html.IndexOf("<tr id=\"pagespace\"");

            var comments = new List<Comment>();
            while (true)
            {
                var c = new Comment();
                var athing = html.IndexOf("athing comtr", idx);
                if (athing == -1)
                    break;
                c.id = ParseUtils.ParseBetween(html, athing, "id='", "'");
                c.user = ParseUtils.ParseBetween(html, athing, "class=\"hnuser\">", "</a>");
                var idxage = html.IndexOf("class=\"age\"", athing) + 15;
                var agostr = ParseUtils.ParseBetween(html, idxage, "\">", "</a>");
                c.submitTime = ParseUtils.AgeString(agostr).Value;

                c.comment = ParseComment(html, idxage, out int idxcom);
                comments.Add(c);

                idx = idxcom;
            }
            return comments;
        }

        public static List<Thread> ParseThreadList(string html)
        {
            var idx = html.IndexOf("class=\"itemlist\"", 0);
            var threads = new List<Thread>();
            while(true)
            {
                var thread = new Thread();
                var vote = html.IndexOf("vote?id=", idx);
                if (vote == -1)
                    break;

                var slink = html.IndexOf("storylink", vote);
                var href = html.LastIndexOf("href=\"", slink);
                var score = html.IndexOf("score", slink);

                thread.id = ParseUtils.ParseBetween(html, vote, "?id=", "&amp;");
                thread.url = ParseUtils.ParseBetween(html, href, "href=\"", "\"");
                thread.title = ParseUtils.ParseBetween(html, slink, "\">", "</");
                var pstr = ParseUtils.ParseBetween(html, score, "\">", "</");
                thread.points = Int32.Parse(pstr.Substring(0, pstr.IndexOf(" ")));
                thread.submittedBy = ParseUtils.ParseBetween(html, score, "class=\"hnuser\">", "</a>");

                var idxage = html.IndexOf("class=\"age\"", score)+15;
                var agostr = ParseUtils.ParseBetween(html, idxage, "\">", "</a>");
                thread.submitTime = ParseUtils.AgeString(agostr).Value;

                var cend = html.IndexOf("comments</a>", idxage);
                var cstart = html.LastIndexOf("item?id=", cend);
                var comstr = ParseUtils.ParseBetween(html, cstart, "\">", "&nbsp;comments");
                thread.comments = Int32.Parse(comstr);
                idx = cstart;

                threads.Add(thread);
            }
            return threads;
        }
    }

    public delegate void logger(string s);
    public class HNFetcher
    {
        public static System.Action<string> Logger;

        readonly public static string hnurl = "https://news.ycombinator.com/";
        readonly public static string useragent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:66.0) Gecko/20100101 Firefox/66.0";

        public static string GetHTML(string hackerNewsUrl) {
            if (Logger != null)
                Logger.Invoke("Fetching " + hackerNewsUrl);
            var c = new HttpClient();
            c.DefaultRequestHeaders.Add("User-Agent", useragent);
            c.DefaultRequestHeaders.Add("Referer", "https://news.ycombinator.com/");
            var s = c.GetStringAsync(hnurl + hackerNewsUrl).GetAwaiter().GetResult();
            Logger.Invoke("Retrieved " + hackerNewsUrl);
            return s;
        }

        public static string GetThread(string id, int page=1)
            => GetHTML("item?id=" + id + "&p="+page);

        public static string GetThreadListToday(int page=1)
            => GetHTML("news?p="+page);

        public static string GetThreadListDay(DateTime date, int page=1)
            => GetHTML("front?day=" + date.ToString("yyyy-MM-dd") + "&p="+page);

        public static string GetThreadsUser(string id)
            => GetHTML("threads?id=" + id);
    }

    public class HNAgent
    {
        string html;
        string url;

        // How much we sleep in ms between each page fetch, to prevent hammering.
        int minSleep; 
        int maxSleep;
        bool firstRequest = true;

        public HNAgent (int minSleep=60000, int maxSleep=120000)
        {
            this.minSleep = minSleep;
            this.maxSleep = maxSleep;
        }

        private void Sleep()
        {
            if(firstRequest)
            {
                firstRequest = false;
                return;
            }
            var sleep = new Random().Next(this.minSleep, this.maxSleep);
            System.Threading.Tasks.Task.Delay(sleep).Wait();
        }

        public HNAgent GetThread(string id, int page=1)
        {
            Sleep();
            html = HNFetcher.GetThread(id, page);
            return this;
        }

        public HNAgent GetThreadListToday()
        {
            Sleep();
            html = HNFetcher.GetThreadListToday();
            return this;
        }

        public HNAgent GetThreadsUser(string id)
        {
            Sleep();
            html = HNFetcher.GetThreadsUser(id);
            url = HNFetcher.hnurl + "threads?id=";
            return this;
        }

        public HNAgent NextPage()
        {
            Sleep();
            var href = HNParser.GetMoreHref(html);
            if (href != null)
            {
                url = HNFetcher.hnurl + href;
                html = HNFetcher.GetHTML(href);
            }
            return this;
        }

        public bool HasMore
            => HNParser.GetMoreHref(html) != null;

        public List<Comment> Comments
            => HNParser.ParseComments(html);

        public List<Thread> Threads
            => HNParser.ParseThreadList(html);
    }
}
