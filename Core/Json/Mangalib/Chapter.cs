using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Json.Mangalib
{
    public class Chapter
    {
        public string name { get; set; }
        public string volume { get; set; }
        public string number { get; set; }
        public Translator[] teams { get; set; }
        public Page[] pages { get; set; }
    }
}
