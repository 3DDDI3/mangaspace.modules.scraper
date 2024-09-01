using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Classes
{
    public class Remanga:Scraper
    {
        public Remanga(string baseUrl, string catalogUrl) : base(baseUrl, catalogUrl) {
            page = new RemangaPage();
            page.getPages();
        }
    }
}
