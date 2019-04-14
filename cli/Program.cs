using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Parse.HackerNews
{
    class Program
    {
        static void Main(string[] args)
        {
            HNFetcher.Logger = (string s) =>
            {
                Console.WriteLine(s);
            };
            //var html = HNFetcher.GetThreadListToday();
            //File.WriteAllText("./thread.html", html);
            // var html = File.ReadAllText("./thread.html");
            // var threads = HNParser.ParseThreadList(html);

            //var html = HNFetcher.GetThreadListDay(new DateTime(2017, 1, 1));
            //File.WriteAllText("./threads-2017-01-01.html", html);
            //var html = File.ReadAllText("./threads-2017-01-01.html");
            //var threads = HNParser.ParseThreadList(html);
            //File.WriteAllText("./threads-2017-01-01.json", threads.ToJson());

            //var html = HNFetcher.GetThread("19658503");
            //File.WriteAllText("./19658503.html", html);
            //var html = File.ReadAllText("./19658503.html");
            //var comments = HNParser.ParseComments(html);
            //File.WriteAllText("./19658503.json", comments.ToJson());
            //var comments = HNParser.ParseComments(html);

            var a = new HNAgent();
            a.GetThread("19658973");
            //File.WriteAllText("./blah.json", a.Threads.ToJson());
            File.WriteAllText("./comments.json", a.Comments.ToJson());
        }
    }
}
