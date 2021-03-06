﻿using System;
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
        private Func<TopRambler,HashSet<string>> _setCatalog;

        public Downloader(Catalog catalog, Dictionary<string, TopRambler> cache, bool isCatOrGeo)
        {
            _cache = cache;
            _catalog = catalog;
            _setCatalog = x => isCatOrGeo ? x.FullPath : x.Geo;
        }

        public void Run(object state=null){
            Console.WriteLine("Обкачка начата");
            var doc = new HtmlAgilityPack.HtmlDocument();
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
                    doc.LoadHtml(page);
                    var rows = doc.DocumentNode.SelectNodes("//table[@id=\"stat-top100\"]/tr");

                    foreach (var row in rows)
                    {
                        var href = row.SelectSingleNode("td/div/div/div/a[2]");
                        var url = href.GetAttributeValue("href", null);

                        var elem = new TopRambler { Url = url };

                        if (!_cache.ContainsKey(elem.Url))
                        {
                            var name = href.InnerText;

                            var indexPop = Program.GetInt(row.SelectSingleNode("td[3]").InnerText);
                            var views = Program.GetInt(row.SelectSingleNode("td[5]").InnerText);

                            elem.Name = name;
                            elem.IndexPop = indexPop;
                            elem.Views = views;

                            _setCatalog(elem).Add(_catalog.Name);

                            _cache.Add(elem.Url, elem);
                        }
                        else
                        {
                            if (!_setCatalog(_cache[elem.Url]).Contains(_catalog.Name))
                            {
                                _setCatalog(_cache[elem.Url]).Add(_catalog.Name);
                            }
                        }
                    }

                    Console.Write(".");

                    //Переход на следующую страницу
                    var next = doc.DocumentNode.SelectSingleNode("//*[@id=\"nextPage\"]");
                    if (next != null)
                    {
                        _catalog.Url = next.GetAttributeValue("href", null);                        
                    }
                    else break;
                }
                catch (Exception e) { Console.WriteLine("[Downloader|ERROR]:\t"+e.Message+"\n"+e.StackTrace); }
            }
            _catalog.IsFinished = true;
            Console.WriteLine("Обкачка закончена");
        }

    }
}
