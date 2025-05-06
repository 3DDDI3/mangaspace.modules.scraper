using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Json.Remanga
{
    public class Branch
    {
        public string id { get; set; }
        public RemangaPublisher[] publishers { get; set; }
    }

    public class RemangaPublisher
    {
        public string id { get; set; }
        public string name { get; set; }
        public string dir { get; set; }
        public RemangaCover cover { get; set; }
        public RemangaType type { get; set; }
    }

    public class RemangaCover
    {
        public string mid { get; set; }
        public string high {  get; set; }
    }

    public class RemangaType
    {
        public string id { get; set; }
        public string name { get; set; }
    }
}
