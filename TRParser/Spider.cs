using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TRParser
{
    public class Spider
    {
        private Dictionary<string, TopRambler> _cache;
        private Queue<Catalog> _queue;

        private int _catalogCount;

        private const string StartPage = "http://top100.rambler.ru/navi/?pageCount=1000&stat=1";

        private const string PatternCatalog = @"<a href=""(?<url>.+?)"">(?<name>[-\w, ]+)</a>";
        private const string PatternCatalogBegFirst = @"<div class=""cl"" id=""theme_full"">";//<h3>Тема:</h3>
        private const string PatternCatalogBeg = @"(?<=<div class=""cl"" id=""theme_full"">[\w\s\p{S}\p{P}]*<dd>)";
        private const string PatternCatalogEnd = @"</dl>\s*</div>";
        public Spider()
        {
            _cache = new Dictionary<string, TopRambler>();
            _queue = new Queue<Catalog>();
        }
        public void Run()
        {
            Preprocessing();
        }
        private void Preprocessing()
        {
            var answ = true;
            while (answ)
            {
                Console.WriteLine("Загрузить каталоги?(y/n)");
                var key = Console.ReadKey().KeyChar;
                switch (key)
                {
                    case 'y': ReadCatalogs(); answ = false; break;
                    case 'n': ParseCatalogs(); answ = false; break;
                    default: Console.WriteLine("Неверно. Повторите ввод!"); break;
                }
            }


        }
        private void Processing()
        {
            while (_queue.Any())
            {
                var item = _queue.Dequeue();
                var th = new Thread(new Downloader(item, _cache).Run);
                th.Start();
            }
        }

        private void ParseCatalogs()
        {
            var regCatalog = new Regex(PatternCatalog);
            var regBeginFirst = new Regex(PatternCatalogBegFirst);
            var regBegin = new Regex(PatternCatalogBeg);
            var regEnd = new Regex(PatternCatalogEnd);

            var isFirst = true;

            var localQueue = new Queue<Catalog>();
            localQueue.Enqueue(new Catalog("", StartPage));
            var listCatalogs = new List<Catalog>();

            while (localQueue.Any())
            {
                try
                {
                    var catalog = localQueue.Dequeue();
                    var message = "Извлечен следующий каталог :" + catalog.Name;
                    Console.WriteLine(message);
                    Program.AddToFileLog("[Spider|INFO]:\t" + message);

                    var page = Program.DownloadPage(catalog.Url);
                    if (page == null)
                    {
                        localQueue.Enqueue(catalog);
                        message = "Каталог - " + catalog.Name + "не был скачан. Добавляем в конец очереди.";
                        Console.WriteLine(message);
                        Program.AddToFileLog("[Spider|WARN]:\t" + message);
                        continue;
                    }
                    var mchBeg = ((isFirst) ? regBeginFirst : regBegin).Match(page);
                    isFirst = false;

                    if (!mchBeg.Success)
                    {
                        message = "Дошли до листового каталога:" + catalog.Name + "(" + catalog.Url + ")";
                        
                        Program.ColoredPrint(message, ConsoleColor.Green);
                        Program.AddToFileLog("[Spider|INFO]:\t" + message);

                        //var th = new Thread(new Downloader(page, _cache).Run);
                        //th.Start();
                        catalog.IsSheet = true;
                        listCatalogs.Add(catalog);

                        continue;
                    }

                    var beg = mchBeg.Index;
                    var len = regEnd.Match(page, beg).Index - beg;

                    var mch = regCatalog.Match(page, beg, len);
                    do
                    {
                        var cat = new Catalog(catalog.Name + "/" + mch.Groups["name"].Value, mch.Groups["url"].Value);
                        localQueue.Enqueue(cat);
                    } while ((mch = mch.NextMatch()).Value != "");
                }
                catch (Exception e) { Program.AddToFileLog("[Spider|ERROR]:\t" + e.Message + "\n" + e.StackTrace); }
            }
            //"_queue = listCatalogs;"
            SaveCatalogs(listCatalogs);
        }
        private Queue<Catalog> ReadCatalogs()
        {
            Console.Write("Reading catalogs...");
            Queue<Catalog> res;
            using (var fs = new FileStream("Catalogs.txt", FileMode.OpenOrCreate))
            {
                var byteArr = new byte[fs.Length];
                fs.Read(byteArr, 0, byteArr.Length);
                res = JsonConvert.DeserializeObject<Queue<Catalog>>(Encoding.UTF8.GetString(byteArr));
            }
            Console.WriteLine("done");
            return res;
        }
        private void SaveCatalogs(IEnumerable<Catalog> queue)
        {
            Console.Write("Saving catalogs...");
            using (var fs = new FileStream("Catalogs.txt", FileMode.Create))
            {
                var jsonStr = JsonConvert.SerializeObject(queue);
                var res = Encoding.UTF8.GetBytes(jsonStr);
                fs.Write(res, 0, res.Length);
            }
            Console.WriteLine("done");
        }
        
    }
}
