using OpenQA.Selenium.Edge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Interfaces
{
    public interface IScraper
    {
        public string baseUrl { get; set; }
        public EdgeDriver driver { get; set; }
        public EdgeOptions edgeOptions { get; set; }
        public IPage page { get; set; }
        public ITitle title { get; set; }
    }
}
