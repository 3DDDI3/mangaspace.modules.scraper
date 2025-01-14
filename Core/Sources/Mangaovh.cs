using OpenQA.Selenium.Edge;
using OpenQA.Selenium;
using Scraper.Core.Classes.General;
using Scraper.Core.Interfaces;
using Scraper.Core.Json.Mangaovh;
using Newtonsoft.Json;
using Scraper.Core.Enums;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using JsonObjects = Scraper.Core.Json.Mangaovh;
using Class = Scraper.Core.Classes.General;
using Scraper.Core.Classes.Uploader;
using RussianTransliteration;

namespace Scraper.Core.Sources
{
    public class Mangaovh:IScraper
    {
        private IFTPServer ftpServer;
        public string baseUrl { get; set; }
        public EdgeDriver driver { get; set; }
        public IPage page { get; set; }
        public ITitle title { get; set; }
        public Server server { get; set; }        

        public Mangaovh(Configuration conf, EdgeOptions? options = null)
        {
            title = new Title()
            {
                persons = new List<IPerson>(),
                contacts = new List<string>(),
                genres = new List<string>(),
                chapters = new List<IChapter>()
            };

            page = new Class.Page() { baseUrl = conf.scraperConfiguration.baseUrl, catalogUrl = conf.scraperConfiguration.catalogUrl, pageUrl = conf.scraperConfiguration.pages };
            ftpServer = new FTPServer()
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

        public void getPages() { }

        public void getChapters()
        {
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            JObject result = JsonConvert.DeserializeObject<JObject>(js.ExecuteScript("return JSON.stringify(window.__remixContext.state.loaderData)").ToString());

            JsonObjects.Chapter[] chapters = JsonConvert.DeserializeObject<JsonObjects.Chapter[]>(result["routes/reader/book/$slug/index"]["chapters"].ToString());

            JsonObjects.Branch[] branches = JsonConvert.DeserializeObject<JsonObjects.Branch[]>(result["routes/reader/book/$slug/index"]["branches"].ToString());

            foreach (var chapter in chapters)
            {
                title.chapters.Add(new Class.Chapter()
                {
                    name = chapter.name,
                    number = chapter.number,
                    url = chapter.id,
                    translator = new Class.Person()
                    {
                        name = branches.Where(x => x.id == chapter.branchId).First().publishers[0].name
                    }
                });
            }
        }

        public void getImages()
        {
            string path = driver.Url;

            foreach (var chapter in title.chapters)
            {
                //driver.Navigate().GoToUrl($"{path}/{chapter.url}");

                //IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                //JObject result = JsonConvert.DeserializeObject<JObject>(js.ExecuteScript("return JSON.stringify(window.__remixContext.state.loaderData)").ToString());

                //JsonObjects.Page[] pages = JsonConvert.DeserializeObject<JsonObjects.Page[]>(result["routes/reader/book/$slug/$chapter"]["chapter"]["pages"].ToString());

                //foreach (var page in pages)
                //{
                //    chapter.images.Add(new Image(page.image));
                //}

                //ftpServer.connect();

                //if (!ftpServer.client.DirectoryExists($"{ftpServer.rootPath}{RussianTransliterator.GetTransliteration(Regex.Replace(title.name, @"[\/\\\*\&\]\[\|]+", ""))}"))
                //    ftpServer.client.CreateDirectory($"{ftpServer.rootPath}{RussianTransliterator.GetTransliteration(Regex.Replace(title.name, @"[\/\\\*\&\]\[\|]+", ""))}");

                //ftpServer.rootPath += $"{RussianTransliterator.GetTransliteration(Regex.Replace(title.name, @"[\/\\\*\&\]\[\|]+", ""))}/";

                //MangaovhUploader uploader = new MangaovhUploader(ftpServer);
                //uploader.upload(chapter);

            }
        }

        public void getTitleInfo()
        {
            title.cover.Add(new Image(Regex.Replace(driver.FindElement(By.XPath("//div[@class='MuiBox-root styles-1wd7fad']/img")).GetAttribute("src"), @"\?[a-zA-z=&0-9]+", "")));
            title.country = driver.FindElement(By.XPath("//div[@class='MuiStack-root styles-vev28']/div/a[@class='MuiTypography-root MuiTypography-body2 MuiLink-root MuiLink-underlineHover styles-1n4t9ik']")).Text;
            title.releaseYear = ushort.Parse(driver.FindElement(By.XPath("//div[@class='MuiStack-root styles-vev28']/div/p[@class='MuiTypography-root MuiTypography-body2 styles-ucj12']")).Text);
            title.name = driver.FindElement(By.XPath("//div[@class='MuiStack-root styles-vev28']/h4[@class='MuiTypography-root MuiTypography-h4 styles-1xvinid']")).Text;
            title.otherNames = driver.FindElement(By.XPath("//div[@class='MuiStack-root styles-vev28']/p[@class='MuiTypography-root MuiTypography-body2 styles-ucj12']")).Text;

            if (driver.FindElements(By.XPath("//div[@class='MuiStack-root styles-bw1rt2']/div[@class='MuiStack-root styles-kvb41a']/div")).Count() > 0)
                driver.FindElement(By.XPath("//div[@class='MuiStack-root styles-bw1rt2']/div[@class='MuiStack-root styles-kvb41a']/div")).Click();

            title.genres = driver.FindElements(By.XPath("//a[@class='MuiTypography-root MuiTypography-inherit MuiLink-root MuiLink-underlineNone styles-1ho5bni']")).Select(x => x.Text).ToList();

            driver.FindElements(By.XPath("//div[@class='MuiStack-root styles-3idqc6']/div[@class='MuiStack-root styles-1t2vib1']/div/span")).ToList().ForEach(x => title.description += $"{x.Text}\n");

            switch (driver.FindElement(By.XPath("//div[@class='MuiStack-root styles-vev28']/div/a[@class='MuiTypography-root MuiTypography-body2 MuiLink-root MuiLink-underlineHover styles-1n4t9ik'][2]")).Text)
            {
                case "Заморожен":
                    title.titleStatus = TitleStatus.terminated;
                    break;

                case "Анонс":
                    title.titleStatus = TitleStatus.announcement; ;
                    break;

                case "Онгоинг":
                    title.titleStatus = TitleStatus.continues;
                    break;

                case "Завершен":
                    title.titleStatus = TitleStatus.finished;
                    break;
            }
        }

        public void getPersons()
        {
            foreach (var person in driver.FindElements(By.XPath("//div[@class='MuiStack-root styles-3idqc6']/div[@class='MuiStack-root styles-1t2vib1']/div/div/div[@class='MuiStack-root styles-yd8sa2']")))
            {
                switch (person.FindElement(By.XPath("./div[1]")).Text)
                {
                    case "Издатель":
                        person.FindElements(By.XPath("./div[2]/div/a")).ToList().ForEach(x => title.persons.Add(new Person()
                        {
                            type = PersonType.publishing,
                            name = x.Text,
                            url = x.GetAttribute("href")
                        }));
                        break;

                    case "Журнал":
                        person.FindElements(By.XPath("./div[2]/div/a")).ToList().ForEach(x => title.persons.Add(new Person()
                        {
                            type = PersonType.magazine,
                            name = x.Text,
                            url = x.GetAttribute("href")
                        }));
                        break;

                    case "Автор":
                        person.FindElements(By.XPath("./div[2]/div/a")).ToList().ForEach(x => title.persons.Add(new Person()
                        {
                            type = PersonType.author,
                            name = x.Text,
                            url = x.GetAttribute("href")
                        }));
                        break;

                    case "Художник":
                        person.FindElements(By.XPath("./div[2]/div/a")).ToList().ForEach(x => title.persons.Add(new Person()
                        {
                            type = PersonType.painter,
                            name = x.Text,
                            url = x.GetAttribute("href")
                        }));
                        break;

                    case "Переводчик":
                        person.FindElements(By.XPath("./div[2]/div/a")).ToList().ForEach(x => title.persons.Add(new Person()
                        {
                            type = PersonType.translator,
                            name = x.Text,
                            url = x.GetAttribute("href")
                        }));
                        break;
                }
            }
        }

        public async void parse()
        {
            HttpClient client = new HttpClient();
            var response = client.GetAsync($"https://manga.ovh/yamiko/v2/books?page=1&sort=viewsCount%2Cdesc").Result.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            TitleJson[] urls = JsonConvert.DeserializeObject<TitleJson[]>(responseBody);

            foreach (var url in urls)
            {
                driver.Navigate().GoToUrl($"{page.baseUrl}{page.catalogUrl}/{url.slug}");
                getTitleInfo();
                getPersons();
                getChapters();
                getImages();
                break;
            }

        }
    }
}
