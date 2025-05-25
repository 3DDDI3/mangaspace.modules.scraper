using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Json.Remanga
{
    public class Page
    {
        public string tome { get; set; }
        public string index { get; set; }
        public string chapter { get; set; }
        public string? name {  get; set; }
        public Pages[][] pages { get; set; }
        public Publisher[] publishers {get; set; }
    }

    public class Pages
    {
        public string link { get; set; }
    }
}
