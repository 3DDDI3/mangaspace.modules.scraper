using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Json.Remanga
{
    public class Chapter
    {
        public ChapterContent content { get; set; }
    }

    public class ChapterContent
    {
        public string tome { get; set; }
        public string name { get; set; }
        public string chapter { get; set; }
        public Publisher[] publishers { get; set; }
        public Pages[][] pages { get; set; }
    }

    public class Pages
    {
        public string id { get; set; }
        public string link { get; set; }
        public string width { get; set; }
        public string height { get; set; }
    }

    public class Publisher
    {
        public string dir { get; set; }
    }
}
