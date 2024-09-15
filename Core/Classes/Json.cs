using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Classes
{
    public class Json
    {
        public Content content { get; set; }
    }

    public class Content { 
        public Pages[][] pages { get; set; }
    }

    public class Pages
    {
        public string id { get; set; }
        public string link { get; set; }
        public string width { get; set; }
        public string height { get; set; }
    }
}
