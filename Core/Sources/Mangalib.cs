using Scraper.Core.Interfaces;
using Scraper.Core.Classes;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using MangalibJson = Scraper.Core.Json.Mangalib;
using Scraper.Core.Enums;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Scraper.Core.Json.Mangalib;
using System;
using Chapter = Scraper.Core.Classes.General.Chapter;
using Page = Scraper.Core.Classes.General.Page;
using Scraper.Core.Classes.General;
using Scraper.Core.Classes.Uploader;

namespace Scraper.Core.Sources
{
    public class Mangalib : IScraper
    {
        private IServer server;
        public string baseUrl { get; set; }
        public EdgeDriver driver { get; set; }
        public IPage page { get; set; }
        public ITitle title { get; set; }

        public Mangalib(Configuration conf, EdgeOptions? options = null)
        {
            title = new Title()
            {
                persons = new List<IPerson>(),
                contacts = new List<string>(),
                genres = new List<string>(),
                chapters = new List<IChapter>()
            };

            page = new Page() { baseUrl = conf.scraperConfiguration.baseUrl, catalogUrl = conf.scraperConfiguration.catalogUrl, pageUrl = conf.scraperConfiguration.pages };
            server = new Server()
            {
                url = conf.serverConfiguration.url,
                username = conf.serverConfiguration.username,
                password = conf.serverConfiguration.password,
                rootPath = conf.serverConfiguration.rootPath
            };

            stardDriver(new EdgeOptions() { 
                PageLoadStrategy = PageLoadStrategy.Eager });
        }

        private void stardDriver(EdgeOptions edgeOptions = null)
        {
            edgeOptions = edgeOptions ?? new EdgeOptions();
            driver = new EdgeDriver(edgeOptions);
        }

        public void getPages()
        {
            driver.Navigate().GoToUrl($"{page.baseUrl}{page.catalogUrl}");
        }

        public void parse()
        {
            getPages();

            foreach (var url in driver.FindElements(By.XPath("//a[@class='media-card']")).Select(x => x.GetAttribute("href")))
            {
                driver.Navigate().GoToUrl(url);


                /**
                 * Нажатие кнопки показать все
                 */
                if (driver.FindElements(By.XPath("//div[@class='media-info-list__expand']")).Count > 0)
                    driver.FindElement(By.XPath("//div[@class='media-info-list__expand']")).Click();

                getPersons();
                getTitleInfo();
                getChapters();

                break;
            }
        }

        public void getTitleInfo()
        {
            foreach (var block in driver.FindElements(By.XPath("//a[@class='media-info-list__item'] | //div[@class='media-info-list__item'] | //div[@class='media-info-list__item media-info-list__item_alt-names is-expanded']")))
            {
                switch (block.FindElement(By.XPath(".//div[1]")).Text)
                {
                    case "Тип":
                        title.type = block.FindElement(By.XPath(".//div[2]")).Text;
                        break;

                    case "Год релиза":
                        title.releaseYear = ushort.Parse(block.FindElement(By.XPath(".//div[2]")).Text);
                        break;

                    case "Статус тайтла":
                        switch (block.FindElement(By.XPath(".//div[2]")).Text)
                        {
                            case "Онгоинг":
                                title.titleStatus = TitleStatus.continues;
                                break;

                            case "Анонс":
                                title.titleStatus = TitleStatus.announcement;
                                break;

                            case "Выпуск прекращён":
                                title.titleStatus = TitleStatus.terminated;
                                break;

                            case "Завершён":
                                title.titleStatus = TitleStatus.finished;
                                break;

                            case "Приостановлен":
                                title.titleStatus = TitleStatus.suspended;
                                break;
                        }
                        break;

                    case "Статус перевода":
                        switch (block.FindElement(By.XPath(".//div[2]")).Text)
                        {
                            case "Продолжается":
                                title.translateStatus = TranslateStatus.continues;
                                break;

                            case "Завершён":
                                title.translateStatus = TranslateStatus.finished;
                                break;

                            case "Заморожен":
                                title.translateStatus = TranslateStatus.freezed;
                                break;

                            case "Заброшен":
                                title.translateStatus = TranslateStatus.terminated;
                                break;
                        }
                        break;

                    case "Возрастной рейтинг":
                        switch (block.FindElement(By.XPath(".//div[2]")).Text)
                        {
                            case "16+":
                                title.ageLimiter = AgeLimiter.minor;
                                break;

                            case "18+":
                                title.ageLimiter = AgeLimiter.adult;
                                break;
                        }
                        break;

                    case "Альтернативные названия":
                        title.altName = String.Join(" ", block.FindElements(By.XPath(".//div[2]/div")).Select(x => x.Text));
                        break;
                }

                /**
                 * Раскрытие описания
                 */
                if (driver.FindElements(By.XPath("//button[@class='media-description__expand']")).Count > 0)
                    driver.FindElement(By.XPath("//button[@class='media-description__expand']")).Click();

                title.name = driver.FindElement(By.XPath("//div[@class='media-name__main']")).Text;
                title.altName = driver.FindElement(By.XPath("//div[@class='media-name__alt']")).Text;
                title.cover = new Image(driver.FindElement(By.XPath("//div[@class='media-sidebar__cover paper']/img")).GetAttribute("href"));
                title.genres = driver.FindElements(By.XPath("//div[@class='media-tags']/a")).Select(x => x.GetAttribute("href")).ToList();

            }

            server.rootPath = $"{server.rootPath}{title.altName}/";
            server.connect();

            if (!server.client.DirectoryExists(server.rootPath))
                server.client.CreateDirectory(server.rootPath);

        }

        public void getPersons()
        {
            foreach (var block in driver.FindElements(By.XPath("//a[@class='media-info-list__item'] | //div[@class='media-info-list__item'] | //div[@class='media-info-list__item media-info-list__item_alt-names is-expanded']")))
            {
                switch (block.FindElement(By.XPath(".//div[1]")).Text)
                {
                    case "Автор":
                        block.FindElements(By.XPath(".//div[2]/a"))
                            .ToList()
                            .ForEach(x => title.persons.Add(
                                new Person()
                                {
                                    type = PersonType.author,
                                    name = x.Text,
                                    url = x.GetAttribute("href")
                                })
                            );
                        break;

                    case "Художник":
                        block.FindElements(By.XPath(".//div[2]/a"))
                           .ToList()
                           .ForEach(x => title.persons.Add(
                               new Person()
                               {
                                   type = PersonType.painter,
                                   name = x.Text,
                                   url = x.GetAttribute("href")
                               })
                           );
                        break;

                    case "Издательство":
                        block.FindElements(By.XPath(".//div[2]/a"))
                            .ToList()
                            .ForEach(x => title.persons.Add(
                                new Person()
                                {
                                    type = PersonType.publishing,
                                    name = x.Text,
                                    url = x.GetAttribute("href")
                                })
                            );
                        break;
                }
            }

            driver.FindElements(By.XPath("//div[@class='team-list']/a"))
                .ToList()
                .ForEach(x => title.persons.Add(
                    new Person()
                    {
                        url = x.GetAttribute("href"),
                        image = new Image(Regex.Replace(x.FindElement(By.XPath(".//div[1]")).GetCssValue("background-image"), @"^(url\("")|(\?""\))$", "")),
                        name = x.FindElement(By.XPath(".//div[1]")).Text
                    })
                );
        }

        public void getChapters()
        {
            var url = Regex.Replace(driver.Url, @"(\?[a-zA-z\-\=""]+)$", "");

            driver.Close();

            stardDriver(new EdgeOptions() { PageLoadStrategy = PageLoadStrategy.Eager });

            driver.Navigate().GoToUrl($"{url}?section=chapters");

            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

            MangalibJson.Chapter[] chapters = JsonConvert.DeserializeObject<MangalibJson.Chapter[]>(js.ExecuteScript("return JSON.stringify(window.__DATA__.chapters.list)").ToString());

            foreach (var chapter in chapters)
            {
                title.chapters.Add(
                    new Chapter()
                    {
                        name = chapter.chapter_name,
                        number = chapter.chapter_number,
                        volume = chapter.chapter_volume,
                        url = $"{url}/v{chapter.chapter_volume}/c{chapter.chapter_number}?bid={chapter.branch_id}&id={chapter.chapter_id}"
                    });
                break;
            }
            getImages();

        }

        /// <summary>
        /// TODO Реализовать скачивание изображений
        /// </summary>
        public void getImages()
        {
            var url = Regex.Replace(driver.Url, @"(\?[a-zA-z\-\=""]+)$", "");

            MangalibUploader uploader = new MangalibUploader(server);

            foreach (var chapter in title.chapters)
            {
                driver.Close();

                stardDriver(new EdgeOptions() { PageLoadStrategy = PageLoadStrategy.Eager });

                driver.Navigate().GoToUrl(chapter.url);

                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

                MangalibJson.Page[] pages = JsonConvert.DeserializeObject<MangalibJson.Page[]>(js.ExecuteScript("return JSON.stringify(window.__pg)").ToString());
                foreach (var page in pages)
                {
                    chapter.images.Add(new Image($"https://img33.imgslib.link//manga/{Regex.Matches(url, @"http(?:s)?:\/{2}mangalib.me\/([a-zA-Z-]+)")[0].Groups[1].Value}/chapters/{Regex.Matches(chapter.url, @"&id=(\d+)")[0].Groups[1].Value}/{page.u}"));
                }

                uploader.upload(chapter);
                chapter.images = new List<IImage>();
            }
        }
    }
}
