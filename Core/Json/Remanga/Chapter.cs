using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Json.Remanga
{
    public class Chapter
    {
        public _Chapter[] results { get; set; }
        public int count { get; set; }
        public string? next { get; set; }
    }

    public class _Chapter
    {
        public string id { get; set; }
        public string index {get; set; }
        public string tome { get; set; }
        public string? name { get; set; }
        public string chapter { get; set; }
        public _Publisher[] publishers { get; set; }
    }

    public class _Publisher
    {
        public int id { get; set; }
        public string name { get; set; }
        public string dir { get; set; }
        public Cover cover { get; set; }
    }

    public class Cover
    {
        public string mid {  get; set; }
        public string high { get; set; }
    }

}
