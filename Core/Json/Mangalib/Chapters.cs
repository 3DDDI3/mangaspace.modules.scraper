using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Json.Mangalib
{
    /// <summary>
    ///  Главы
    /// </summary>
    public class Chapters
    {
        public int id { get; set; }
        public string number { get; set; }
        public string name { get; set; }
        public int volume { get; set; }
        public Branch[] branches { get; set; }
    }
}
