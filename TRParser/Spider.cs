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
        private List<Catalog> _catalogs;
        private List<Catalog> _catalogsGeo;

        public Spider()
        {
            _cache = new Dictionary<string, TopRambler>();
            _catalogs = new List<Catalog>();
            _catalogsGeo = new List<Catalog>();
            ThreadPool.SetMaxThreads(50, 50);
        }
        public void Run()
        {
            Preprocessing();
            Processing();
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
                    case 'y': _catalogs = ReadCatalogs("Catalogs.txt"); answ = false; break;
                    case 'n': ParseCatalogs(true); answ = false; break;
                    default: Console.WriteLine("Неверно. Повторите ввод!"); break;
                }
            }
            answ = true;
            while (answ)
            {
                Console.WriteLine("Загрузить гео?(y/n)");
                var key = Console.ReadKey().KeyChar;
                switch (key)
                {
                    case 'y': _catalogsGeo = ReadCatalogs("Geo.txt"); answ = false; break;
                    case 'n': ParseCatalogs(false); answ = false; break;
                    default: Console.WriteLine("Неверно. Повторите ввод!"); break;
                }
            }

        }
        private void Processing()
        {
            var tsk = new Task(SaveLoop);
            tsk.Start();
            var opt = new ParallelOptions { MaxDegreeOfParallelism = 50 };
            Parallel.ForEach(_catalogs, opt, (item) =>
            {
                if (!item.IsFinished) new Downloader(item, _cache, true).Run();
            });

            Parallel.ForEach(_catalogsGeo, opt, (item) =>
            {
                if (!item.IsFinished) new Downloader(item, _cache, false).Run();
            });

            Save();
            //while (_queue.Any())
            //{
            //    var item = _queue.Dequeue();

            //    //ThreadPool.QueueUserWorkItem(new Downloader(item, _cache).Run);
            //    //new Downloader(item, _cache).Run();
            //    //var th = new Thread(new Downloader(item, _cache).Run);
            //    //th.Start();
            //}
        }
        private void ParseCatalogs(bool isCatOrGeo = true)
        {
            var isFirst = isCatOrGeo;

            var localQueue = new Queue<Catalog>();

            if (isCatOrGeo)
            {
                localQueue.Enqueue(new Catalog("", ""));
            }
            else
            {
                localQueue.Enqueue(new Catalog("Россия", "?rgn=1"));
                localQueue.Enqueue(new Catalog("СНГ", "?rgn=107"));
            }

            var listCatalogs = new List<Catalog>();

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();

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

                    doc.LoadHtml(page);

                    var node = doc.DocumentNode.SelectSingleNode(isCatOrGeo ? "//div[@id=\"theme_full\"]" : "//div[@id=\"region_full\"]");
                    var href = node.SelectNodes((isFirst) ? "dl/dt/a" : "dl/dd/a");

                    if (node == null || href == null)
                    {
                        message = "Дошли до листового каталога:" + catalog.Name + "(" + catalog.Url + ")";

                        Program.ColoredPrint(message, ConsoleColor.Green);
                        Program.AddToFileLog("[Spider|INFO]:\t" + message);

                        catalog.IsSheet = true;
                        listCatalogs.Add(catalog);

                        continue;
                    }

                    if (!isCatOrGeo) listCatalogs.Add(catalog);

                    
                    isFirst = false;

                    foreach (var a in href)
                    {
                        var cat = new Catalog(isCatOrGeo ? catalog.Name + "/" : "" + a.InnerText, a.Attributes.AttributesWithName("href").ElementAt(0).Value);
                        localQueue.Enqueue(cat);
                    }

                }
                catch (Exception e) { Program.AddToFileLog("[Spider|ERROR]:\t" + e.Message + "\n" + e.StackTrace); }
            }
            if (isCatOrGeo) _catalogs = listCatalogs;
            else _catalogsGeo = listCatalogs;

            SaveCatalogs(listCatalogs, isCatOrGeo ? "Catalogs.txt" : "Geo.txt");
        }


        private List<Catalog> ReadCatalogs(string fileName)
        {
            Console.Write("Reading catalogs...");
            List<Catalog> res;
            using (var fs = new FileStream(fileName, FileMode.OpenOrCreate))
            {
                var byteArr = new byte[fs.Length];
                fs.Read(byteArr, 0, byteArr.Length);
                res = JsonConvert.DeserializeObject<List<Catalog>>(Encoding.UTF8.GetString(byteArr));
            }
            Console.WriteLine("done");
            return res;
        }
        private void SaveCatalogs(IEnumerable<Catalog> queue, string fileName)
        {
            Console.Write("Saving catalogs...");
            using (var fs = new FileStream(fileName, FileMode.Create))
            {
                var jsonStr = JsonConvert.SerializeObject(queue);
                var res = Encoding.UTF8.GetBytes(jsonStr);
                fs.Write(res, 0, res.Length);
            }
            Console.WriteLine("done");
        }

        private void SaveLoop()
        {
            while (true)
            {
                Thread.Sleep(60000);
                var a = 0;
                var b = 0;
                ThreadPool.GetAvailableThreads(out a, out b);
                Console.WriteLine("Доступные потоки: {" + a + ";" + b + "}");
                Save();
            }
        }
        private void Save()
        {
            Program.ColoredPrint("Сохранение данных..", ConsoleColor.White);
            SaveCatalogs(_catalogs, "Catalogs.txt");
            SaveCatalogs(_catalogsGeo, "Geo.txt");
            Program.SaveAll(_cache.Values.ToArray(), "mongodb://localhost:27017/topRambler");
            Program.ColoredPrint("Сохранение завершено", ConsoleColor.White);
        }

        //private const string PatternCatalog = @"<a href=""(?<url>.+?)"">(?<name>[-\w,. ]+)</a>";
        //private const string PatternCatalogBegFirst = @"<div class=""cl"" id=""theme_full"">";//<h3>Тема:</h3>
        //private const string PatternCatalogBeg = @"(?<=<div class=""cl"" id=""theme_full"">[\w\s\p{S}\p{P}]*?<dd>)";
        //private const string PatternCatalogEnd = @"</dl>\s*</div>";

        //private void ParseCatalogsOld()
        //{
        //    var regCatalog = new Regex(PatternCatalog);
        //    var regBeginFirst = new Regex(PatternCatalogBegFirst);
        //    var regBegin = new Regex(PatternCatalogBeg);
        //    var regEnd = new Regex(PatternCatalogEnd);

        //    var isFirst = false;

        //    var localQueue = new Queue<Catalog>();
        //    localQueue.Enqueue(new Catalog("", "?theme=321%2F332"));
        //    var listCatalogs = new List<Catalog>();

        //    while (localQueue.Any())
        //    {
        //        try
        //        {
        //            var catalog = localQueue.Dequeue();
        //            var message = "Извлечен следующий каталог :" + catalog.Name;
        //            Console.WriteLine(message);
        //            Program.AddToFileLog("[Spider|INFO]:\t" + message);

        //            var page = Program.DownloadPage(catalog.Url);
        //            if (page == null)
        //            {
        //                localQueue.Enqueue(catalog);
        //                message = "Каталог - " + catalog.Name + "не был скачан. Добавляем в конец очереди.";
        //                Console.WriteLine(message);
        //                Program.AddToFileLog("[Spider|WARN]:\t" + message);
        //                continue;
        //            }
        //            var mchBeg = ((isFirst) ? regBeginFirst : regBegin).Match(page);
        //            isFirst = false;

        //            if (!mchBeg.Success || mchBeg.Value == "")
        //            {
        //                message = "Дошли до листового каталога:" + catalog.Name + "(" + catalog.Url + ")";

        //                Program.ColoredPrint(message, ConsoleColor.Green);
        //                Program.AddToFileLog("[Spider|INFO]:\t" + message);

        //                //var th = new Thread(new Downloader(page, _cache).Run);
        //                //th.Start();
        //                catalog.IsSheet = true;
        //                listCatalogs.Add(catalog);

        //                continue;
        //            }

        //            var beg = mchBeg.Index;
        //            var len = regEnd.Match(page, beg).Index - beg;

        //            var mch = regCatalog.Match(page, beg, len);
        //            do
        //            {
        //                var cat = new Catalog(catalog.Name + "/" + mch.Groups["name"].Value, mch.Groups["url"].Value);
        //                localQueue.Enqueue(cat);
        //            } while ((mch = mch.NextMatch()).Value != "");
        //        }
        //        catch (Exception e) { Program.AddToFileLog("[Spider|ERROR]:\t" + e.Message + "\n" + e.StackTrace); }
        //    }
        //    //"_queue = listCatalogs;"
        //    SaveCatalogs(listCatalogs);
        //}
    }
}
