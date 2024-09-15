using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Json.Mangalib
{
    public class Chapter
    {
        public int branch_id { get; set; }
        public int chapter_id { get; set; }
        public string chapter_name { get; set; }
        public string chapter_number { get; set; }
        public string chapter_volume {  get; set; }
    }
}
