using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Json.Remanga
{
    public class Publisher
    {
        public int id { get; set; }
        public string name { get; set; }
        public string dir { get; set; }
        public _RemangaCover cover { get; set; }
    }

    public class _RemangaCover
    {
        public string mid { get; set; }
        public string high { get; set; }
    }
}
