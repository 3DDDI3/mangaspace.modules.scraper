using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using Scraper.Core.Classes.General;
using Scraper.Core.Classes.Uploader;
using Scraper.Core.Enums;
using Scraper.Core.Interfaces;
using System.Text.RegularExpressions;
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
            driver.FindElements(By.XPath("//div[@class='chapter-item']")).ToList().ForEach(x => title.chapters.Add(
                new Chapter()
                {
                    url = x.FindElement(By.XPath("./div[1]/a")).GetAttribute("href"),
                    volume = Regex.Matches(x.FindElement(By.XPath("./div[1]/a")).Text, @"(?:Том\s*(\d+))|(?:Глава\s*(\d+))")[0].Groups[1].Value,
                    number = Regex.Matches(x.FindElement(By.XPath("./div[1]/a")).Text, @"(?:Том\s*(\d+))|(?:Глава\s*(\d+))")[1].Groups[2].Value
                }));
            driver.Close();
        }

        public void getImages()
        {
            stardDriver(new EdgeOptions() { PageLoadStrategy = PageLoadStrategy.Eager });
            foreach (var chapter in title.chapters)
            {

                driver.Navigate().GoToUrl("https://mangaclub.ru/manga/view/12188-ischezajuschie-ponedelniki/v1-c33.html#1");

                WebDriverWait wait;

                while (driver.FindElements(By.XPath("//div[@class='manga-lines-page']/a")).Count() > driver.FindElements(By.XPath("//div[@class='vertical container']/img")).Count())
                    new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                driver.FindElements(By.XPath("//div[@class='vertical container']/img")).ToList().ForEach(
                    x => chapter.images.Add(
                        new Image(x.GetAttribute("src"))
                    )
                );

                MangaClubUploader uploader = new MangaClubUploader(server);
                uploader.upload(chapter);

                break;

               
            }
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
            title.name = driver.FindElement(By.XPath("//div[@class='content-title']/h2")).Text;

            foreach (var block in driver.FindElements(By.XPath("//div[@class='info']/div/span")))
            {
                switch (block.Text)
                {
                    case "Названия":
                        title.altName = block.FindElement(By.XPath("./parent::div/strong")).Text.Split("/")[0];
                        break;

                    case "Год печати":
                        title.releaseYear = ushort.Parse(block.FindElement(By.XPath("./parent::div/a")).Text);
                        break ;

                    case "Жанр":
                        title.genres = block.FindElements(By.XPath("./parent::div/div/a")).Select(x => x.Text).ToList();
                        break ;

                    case "Автор(ы)":
                        block.FindElements(By.XPath("./parent::div/div/a")).ToList().ForEach(
                            x => title.persons.Add(
                                new Person()
                                {
                                    type = PersonType.author,
                                    name = x.Text,
                                })
                        );
                        break ;

                    case "Перевод":
                        block.FindElements(By.XPath("./parent::div/div/a")).Where(x => x.Text != "ссылка").ToList().ForEach(
                            x => title.persons.Add(new Person()
                            {
                                name = x.Text,
                                type = PersonType.translator,
                            })
                        );
                        break ;

                    case "Статус перевода":
                        switch (block.FindElement(By.XPath("./parent::div/a")).Text)
                        {
                            case "Завершен":
                                title.translateStatus = TranslateStatus.finished;
                                break;

                            case "Продолжается":
                                title.translateStatus = TranslateStatus.continues;
                                break;
                        }
                        
                        break ;
                }
            }
            

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
                    getChapters();
                    getImages();
                }

                break;
            }
        }
    }
}
