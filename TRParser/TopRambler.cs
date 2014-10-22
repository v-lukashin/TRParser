using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TRParser
{
    public class TopRambler
    {
        private string _id;
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
        public string Url { get; set; }
        public string Description { get; set; }
        public HashSet<string> FullPath { get; set; }
        public int IndexPop { get; set; }
        public int Views { get; set; }
        public HashSet<string> Geo { get; set; }
    }
}
