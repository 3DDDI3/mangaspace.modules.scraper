using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using Scraper.Core.Interfaces;

namespace Scraper.Core.Classes
{
    public class Scraper : IScraper
    {
        public string baseUrl { get; set; }
        public IPage page { get; set; }
        public ITitle title { get; set; }
        public EdgeDriver driver { get; set; }
        public EdgeOptions edgeOptions { get; set; }
        public Scraper(Configuration configuration, EdgeOptions? options = null)
        {
            title = new Title()
            {
                persons = new List<IPerson>(),
                contacts = new List<string>(),
                genres = new List<string>(),
                chapters = new List<IChapter>()
            };

            driverStart(options);
        }

        public void driverStart(EdgeOptions? options = null)
        {
            edgeOptions = options ?? new EdgeOptions();
            driver = new EdgeDriver(edgeOptions);
            ((IJavaScriptExecutor)driver).ExecuteScript("window.navigator.userAgent = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/61.0.3163.100 Safari/537.36'");
        }

        public virtual void getPages() { }
        public virtual void getTitleInfo() { }
        public virtual void getChapters() { }
        public virtual void getImages() { }
        public virtual void getPersons() { }
        public virtual void parse() { }
    }
}
