﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TRParser
{
    public class Catalog
    {
        public Catalog(string name, string url)
        {
            Name = name;
            Url = url;
        }
        public string Name { get; set; }
        public string Url { get; set; }
        public bool IsSheet { get; set; }
    }
}
