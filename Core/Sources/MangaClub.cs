using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using Scraper.Core.Classes.General;
using Scraper.Core.Interfaces;
using Class = Scraper.Core.Classes.General;

namespace Scraper.Core.Sources
{
    public class MangaClub : IScraper
    {
        private IServer server;
        public string baseUrl { get; set; }
        public EdgeDriver driver { get; set; }
        public IPage page { get; set; }
        public ITitle title { get; set; }

        public MangaClub(Configuration conf, EdgeOptions? options = null)
        {
            title = new Title()
            {
                persons = new List<IPerson>(),
                contacts = new List<string>(),
                genres = new List<string>(),
                chapters = new List<IChapter>()
            };

            page = new Class.Page()
            {
                baseUrl = conf.scraperConfiguration.baseUrl,
                catalogUrl = conf.scraperConfiguration.catalogUrl,
                pageUrl = conf.scraperConfiguration.pages
            };

            server = new Server()
            {
                url = conf.serverConfiguration.url,
                username = conf.serverConfiguration.username,
                password = conf.serverConfiguration.password,
                rootPath = conf.serverConfiguration.rootPath
            };

            stardDriver(new EdgeOptions()
            {
                PageLoadStrategy = PageLoadStrategy.Eager
            });
        }
        private void stardDriver(EdgeOptions edgeOptions = null)
        {
            edgeOptions = edgeOptions ?? new EdgeOptions();
            driver = new EdgeDriver(edgeOptions);
        }

        public void getChapters()
        {
            
        }

        public void getImages()
        {
        }

        public void getPages()
        {
            driver.Navigate().GoToUrl($"{page.baseUrl}{page.catalogUrl}");
            page.pageRange = new PageRange()
            {
                from = 1,
                to = int.Parse(driver.FindElements(By.XPath("//div[@class='pagination-list']/a"))[driver.FindElements(By.XPath("//div[@class='pagination-list']/a")).Count - 2].Text)
            };
        }

        public void getPersons()
        {
        }

        public void getTitleInfo()
        {

        }

        public void parse()
        {
            getPages();

            for (int i = 0; i < page.pageRange.to; i++)
            {
                driver.Navigate().GoToUrl($"{page.baseUrl}{page.catalogUrl}/{page.pageUrl}{i + 1}");

                foreach (var title in driver.FindElements(By.XPath("//div[@class='shortstory']/div/h4/a")))
                {
                    driver.Navigate().GoToUrl(title.GetAttribute("href"));
                    getTitleInfo();
                }

                break;
            }
        }
    }
}
