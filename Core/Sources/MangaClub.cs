using OpenQA.Selenium.Edge;
using Scraper.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Sources
{
    public class MangaClub : IScraper
    {
        public string baseUrl { get; set; }
        public EdgeDriver driver { get; set; }
        public IPage page { get; set; }
        public ITitle title { get; set; }

        public void getChapters()
        {
        }

        public void getImages()
        {
        }

        public void getPages()
        {
        }

        public void getPersons()
        {
        }

        public void getTitleInfo()
        {
        }

        public void parse()
        {
        }
    }
}
