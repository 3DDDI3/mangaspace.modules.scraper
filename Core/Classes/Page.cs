using Scraper.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Classes
{
    public class Page : IPage
    {
        public string? baseUrl { get; set; }
        public string? catalogUrl { get; set; }
        public string? pageUrl{ get; set; }
        public IPageRange pageRange { get; set; }
        public Page() {
            pageRange = new PageRange();
        }
    }
}
