using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TRParser
{
    public class Program
    {
        private static string _buffer = "";
        private static int _bufferCurrentSize = 0;
        private const int BufferMaxSize = 10;
        static void Main(string[] args)
        {
            new Spider().Run();
            //TestAgP();
            //TestCat();
        }


        static void TestCat()
        {
            //string page = DownloadPage(@"http://top100.rambler.ru/navi/?theme=38&pageCount=1000&stat=1&page=3");
            string page = DownloadPage(@"http://top100.rambler.ru/navi/?theme=157%2F194%2F197");
        
            const string PatternCatalog = @"<a href=""(?<url>.+?)"">(?<name>[-\w, ]+)</a>";
            const string PatternCatalogBegFirst = @"<div class=""cl"" id=""theme_full"">";
            const string PatternCatalogBeg = @"(?<=<div class=""cl"" id=""theme_full"">[\w\s\p{S}\p{P}]*<dd>)";
            const string PatternCatalogEnd = @"</dl>\s*</div>";
            var mchBeg = new Regex(PatternCatalogBeg).Match(page);
            var fl = mchBeg.Success;
            var beg = mchBeg.Index;
            var len = new Regex(PatternCatalogEnd).Match(page, beg).Index - beg;
            var reg = new Regex(PatternCatalog);
            var mch = reg.Match(page, beg, len);
            do
            {
                Console.WriteLine("Res= " + mch);

            } while ((mch = mch.NextMatch()).Value != "");

            const string PatternElement = @"<a name=""\d+"" href=""(?<url>.*?)"".*>\s*.*\s*</a>";
            const string PatternDig = @"<td align=""right"">(?<int>-?[\d&nbsp;]+)</td>";
            var matches = new Regex(PatternDig).Matches(page);
            foreach (Match item in matches)
            {
                Console.WriteLine(item.Groups["int"].Value.Replace("&nbsp;", string.Empty));
            }
            Console.WriteLine("Cnt = " + matches.Count);
        }

        static void TestAgP()
        {
            string page = DownloadPage(@"http://top100.rambler.ru/navi/");
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(page);
            var node = doc.DocumentNode.SelectSingleNode("//*[@id=\"theme_full\"]");
            var href = node.SelectNodes("dl/dt/a");

            foreach (var item in href)
            {
                Console.WriteLine(item.InnerText);
            }
        }


        #region Вспомогательные методы
        public static string DownloadPage(string link, int attemptCount = 10, int waitTimeAttemptSec = 0)
        {
            var cli = new WebClient
            {
                BaseAddress = "http://top100.rambler.ru/navi/",
                Proxy = null,
                Encoding = Encoding.GetEncoding("utf-8")
            };
            var sleepTime = waitTimeAttemptSec * 1000;
            string page = null;
            for (var i = 0; i < attemptCount; i++)
            {
                try
                {
                    page = cli.DownloadString(link);
                    break;
                }
                catch (WebException)
                {
                    Console.WriteLine("TimeoutError({0}). Repeat", i);

                    Thread.Sleep(sleepTime);
                }
            }
            return page;
        }
        public static string GenerateHashMd5(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            bytes = MD5.Create().ComputeHash(bytes);
            return BitConverter.ToString(bytes).Replace("-", string.Empty);
        }
        public static void AddToFileLog(string str)
        {
            str += "\n";

            _buffer += str;
            _bufferCurrentSize++;

            if (_bufferCurrentSize >= BufferMaxSize)
            {
                FlushBuffer();
            }
        }

        public static void FlushBuffer()
        {
            using (var fs = new FileStream("log.txt", FileMode.Append))
            {
                var bytes = Encoding.UTF8.GetBytes(_buffer);
                fs.Write(bytes, 0, bytes.Length);
            }
            _bufferCurrentSize = 0;
            _buffer = "";
        }

        public static void SaveAll(IEnumerable<TopRambler> all, string connStr)
        {
            Console.Write("Saving...");
            var url = MongoUrl.Create(connStr);
            var collect =
                new MongoClient(url).GetServer()
                    .GetDatabase(url.DatabaseName)
                    .GetCollection<TopRambler>(typeof(TopRambler).Name);
            var index = 0;
            foreach (var item in all)
            {
                collect.Save(item);
                index++;
            }
            Console.WriteLine("done ({0}).", index);
            AddToFileLog("Saved " + index + " items");
        }

        public static Dictionary<string, TopRambler> LoadAll(string connStr)
        {
            Console.Write("Loading...");
            var url = MongoUrl.Create(connStr);
            var collect =
                new MongoClient(url).GetServer()
                    .GetDatabase(url.DatabaseName)
                    .GetCollection<TopRambler>(typeof(TopRambler).Name);
            var res = new Dictionary<string, TopRambler>();

            var index = 0;
            foreach (var item in collect.FindAll())
            {
                res.Add(item.Url, item);
                index++;
            }

            Console.WriteLine("done ({0}).", index);
            AddToFileLog("Loaded " + index + " items");
            return res;
        }

        public static void ColoredPrint(string text, ConsoleColor color)
        {
            var tmpColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = tmpColor;
        }

        public static int GetInt(string str)
        {
            str = new Regex(@"\s|&nbsp;").Replace(str, string.Empty);
            return int.Parse(str);
        }
        #endregion
    }
}
