using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TRParser
{
    public class TopRambler
    {
        public TopRambler()
        {
            FullPath = new HashSet<string>();
            Geo = new HashSet<string>();
        }
        private string _id;
        private string _url;
        public string Id
        {
            get
            {
                if (_id == null) _id = Program.GenerateHashMd5(Url);
                return _id;
            }
            set
            {
                _id = value;
            }
        }
        public string Url
        {
            get { return _url; }

            set
            {
                if (!new Regex("^https?://").Match(value).Success) value = "http://" + value;
                _url = value;
            }
        }
        public string ShortUrl
        {
            get
            {
                return new Regex(@"^https?://|www\.|/.*").Replace(Url, string.Empty);
            }
            set { }
        }
        public string Name { get; set; }
        public HashSet<string> FullPath { get; set; }
        public int IndexPop { get; set; }
        public int Views { get; set; }
        public HashSet<string> Geo { get; set; }
    }
}
