using OpenQA.Selenium.Edge;
using Scraper.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Classes
{
    public abstract class Scraper:IScraper
    {
        public string baseUrl { get; set; }
        public IPage page { get; set; }
        public ITitle title { get; set; }
        public EdgeDriver driver { get; set; }
        public EdgeOptions edgeOptions { get; set; }
        public Scraper(string baseUrl, string catalogUrl)
        {
            title = new Title();
            driver = new EdgeDriver(edgeOptions = new EdgeOptions());
            driver.Navigate().GoToUrl(baseUrl);
        }
    }
}
