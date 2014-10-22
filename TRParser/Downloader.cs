using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TRParser
{
    public class Downloader
    {
        private Dictionary<string, TopRambler> _cache;
        private Catalog _catalog;

        private const string PatternNext = @"<a id=""nextPage"" class=""n_pager_next"" href=""(?<link>.*)"">\w+</a>";
        private const string PatternElement = @"<a name=""\d+"" href=""(?<url>.*?)"".*>\s*(?<name>.*)\s*</a>";
        private const string PatternRating = @"<td align=""right"">(?<int>-?[\d&nbsp;]+)</td>";

        private Regex _regNext;
        private Regex _regElement; 
        private Regex _regRating;

        public Downloader(Catalog catalog, Dictionary<string, TopRambler> cache)
        {
            _cache = cache;
            _catalog = catalog;

            _regNext = new Regex(PatternNext);
            _regElement = new Regex(PatternElement);
            _regRating = new Regex(PatternRating);
        }

        public void Run(){
            Console.WriteLine("Обкачка начата");
            while (true)
            {
                try
                {
                    var page = Program.DownloadPage(_catalog.Url + "&pageCount=1000&stat=1", 10, 10);
                    if (page == null)
                    {
                        var message = "Не удалось скачать страницу: " + _catalog.Url;
                        Program.ColoredPrint(message, ConsoleColor.Red);
                        Program.AddToFileLog("[Downloader|ERROR]:\t" + message);
                        break;
                    }



                    Console.WriteLine(".");

                    //Переход на следующую страницу
                    var mchNext = _regNext.Match(page);
                    if (mchNext.Success)
                    {
                        _catalog.Url = mchNext.Groups["link"].Value;
                    }
                    else break;
                }
                catch (Exception e) { Console.WriteLine("[Downloader|ERROR]:\t"+e.Message+"\n"+e.StackTrace); }
            }
            Console.WriteLine("Обкачка закончена");
        }

    }
}
