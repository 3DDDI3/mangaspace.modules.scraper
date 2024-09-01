using Scraper.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Classes
{
    public class RemangaPage : IPage
    {
        public string catalogUrl { get; set; }
        public List<string> pages { get; set; }
        public IPageRange pageRange { get; set; }

        public RemangaPage()
        {
            pageRange = new PageRange();
            pages = new List<string>();
        }
        public void getPages()
        {
            pages.Add("asd");
        }
    }
}
