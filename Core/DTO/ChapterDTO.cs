using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.DTO
{
    public class ChapterDTO
    {
        public string url { get; set; }
        public string? name { get; set; }
        public string number { get; set; }
        public bool isLast { get; set; }

        public ChapterDTO(string url, string number, string? name = null, bool isLast=false)
        {
            this.name = name;
            this.url = url;
            this.number = number;
            this.isLast = isLast;
        }
    }
}
