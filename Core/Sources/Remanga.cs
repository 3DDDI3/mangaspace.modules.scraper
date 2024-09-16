using FluentFTP;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OpenQA.Selenium;
using RussianTransliteration;
using Scraper.Core.Enums;
using Scraper.Core.Interfaces;
using SixLabors.ImageSharp.Formats.Webp;
using System.Net;
using Scraper.Core.Classes;
using System.Text.RegularExpressions;
using ImageSharp = SixLabors.ImageSharp.Image;
using Scraper.Core.Classes.Uploader;
using Scraper.Core.Classes.General;
using OpenQA.Selenium.Edge;

namespace Scraper.Core.Sources
{
    public class Remanga:IScraper
    {
        private IServer server;
        public string baseUrl { get; set; }
        public EdgeDriver driver { get; set; }
        public IPage page { get; set; }
        public ITitle title { get; set; }

        public Remanga(Configuration conf)
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
            startDriver();
        }

        private void startDriver(EdgeOptions edgeOptions = null)
        {
            edgeOptions = edgeOptions ?? new EdgeOptions();
            driver = new EdgeDriver(edgeOptions);
        }

        public void getPages()
        {
            driver.Navigate().GoToUrl($"{page.baseUrl}{page.catalogUrl}");
            driver.FindElement(By.XPath("//input[@class='SwitchBase_input__9Z5ZO Switch_input__bHF07']")).Click();
            page.pageRange = new PageRange()
            {
                from = 1,
                to = int.Parse(driver.FindElement(By.XPath("//div[@class='Pagination_pagination__bJbKa']/button[contains(@class, 'Button_button___CisL Button_button___CisL Button_text__IGNQ6 Button_text-primary__WgBRV hidden-xs')][last()]")).Text)
            };           
        }

        public void parse()
        {
            getPages();

            for (int i = page.pageRange.from; i <= page.pageRange.to; i++)
            {
                driver.Navigate().GoToUrl($"{page.baseUrl}/{page.catalogUrl}{page.pageUrl}{i}");
                var titles = driver.FindElements(By.XPath("//div[@class='Grid_gridItem__aPUx1 p-1']/a")).Select(x => x.GetAttribute("href")).ToList();
                foreach (var _title in titles)
                {
                    driver.Navigate().GoToUrl(_title);
                    getTitleInfo();
                    getPersons();
                    getChapters();
                    getImages();
                    break;
                }
                break;
            }
        }

        public void getTitleInfo()
        {
            if (driver.FindElements(By.XPath("//div[@class='flex p-4']/button[@class='Button_button___CisL Button_button___CisL Button_contained__8_KFk Button_contained-primary__IViyX Button_fullWidth__Dgoqh']")).Count > 0)
                driver.FindElement(By.XPath("//div[@class='flex p-4']/button[@class='Button_button___CisL Button_button___CisL Button_contained__8_KFk Button_contained-primary__IViyX Button_fullWidth__Dgoqh']")).Click();

            title.name = driver.FindElement(By.XPath("//h1[@class='Typography_h3___I3IT']")).Text;

            server.connect();

            if (!server.client.DirectoryExists($"{server.rootPath}{RussianTransliterator.GetTransliteration(Regex.Replace(title.name, @"[\/\\\*\&\]\[\|]+", ""))}"))
                server.client.CreateDirectory($"{server.rootPath}{RussianTransliterator.GetTransliteration(Regex.Replace(title.name, @"[\/\\\*\&\]\[\|]+", ""))}");
                
            server.rootPath += $"{RussianTransliterator.GetTransliteration(Regex.Replace(title.name, @"[\/\\\*\&\]\[\|]+", ""))}/";

            server.disconnect();

            var arr = driver.FindElement(By.XPath("//div[@class='flex flex-col items-start gap-1']/h5")).Text.Split(" ");
            title.type = arr[0];
            title.releaseYear = ushort.Parse(arr[1]);
            var block = driver.FindElements(By.XPath("//button[@class='Typography_body1__YTqxB Typography_color-textSecondary__iFFB7 cursor-pointer mt-1 p-0 border-0']"));
            if (block.Count > 0) block[0].Click();
            title.description = driver.FindElement(By.XPath("//div[@class='flex flex-col gap-4']")).FindElements(By.XPath("//p[@class='editor-paragraph']/span")).Count > 0
                ? driver.FindElement(By.XPath("//div[@class='flex flex-col gap-4']")).FindElement(By.XPath("//p[@class='editor-paragraph']/span")).Text
                : driver.FindElement(By.XPath("//div[@class='flex flex-col gap-4']")).FindElement(By.XPath("//div[@class='Typography_body1__YTqxB']/p")).Text;
            driver.FindElement(By.XPath("//div[@class='flex items-end relative cursor-pointer']")).Click();
            title.otherNames = driver.FindElement(By.XPath("//div[@class='flex items-end relative']/p[1]")).Text;
            driver.FindElement(By.XPath("//span[@class='Chip_chip__0JxfA Chip_white__lcP51']")).Click();
            title.genres = driver.FindElements(By.XPath("//a[@class='Chip_chip__0JxfA Chip_gray__IEsKT']")).Select(x => x.Text).ToList();
            foreach (var item in driver.FindElements(By.XPath("//div[@class='flex flex-col items-start']/p[1]")))
            {
                switch (item.Text)
                {
                    case "Выпуск":
                        switch (item.FindElements(By.XPath("parent::div/p[2]"))[0].Text)
                        {
                            case "Анонс":
                                title.titleStatus = TitleStatus.announcement;
                                break;

                            case "Заморожен":
                                title.titleStatus = TitleStatus.suspended;
                                break;

                            case "Нет переводчика":
                                title.titleStatus = TitleStatus.noTranslator;
                                break;

                            case "Продолжается":
                                title.titleStatus = TitleStatus.continues;
                                break;

                            case "Лицензировано":
                                title.titleStatus = TitleStatus.licensed;
                                break;

                            case "Закончен":
                                title.titleStatus = TitleStatus.finished;
                                break;
                        }
                        break;

                    case "Перевод":
                        switch (item.FindElements(By.XPath("parent::div/p[2]"))[0].Text)
                        {
                            case "Закончен":
                                title.translateStatus = TranslateStatus.finished;
                                break;

                            case "Заморожен":
                                title.translateStatus = TranslateStatus.frezed;
                                break;

                            case "Нет переводчика":
                                title.translateStatus = TranslateStatus.noTranslator;
                                break;

                            case "Продолжается":
                                title.translateStatus = TranslateStatus.continues;
                                break;

                            case "Не переводится (лицензировано)":
                                title.translateStatus = TranslateStatus.licensed;
                                break;
                        }
                        break;

                    case "PG":
                        switch (item.FindElements(By.XPath("parent::div/p[2]"))[0].Text)
                        {
                            case "0+":
                                title.ageLimiter = AgeLimiter.all;
                                break;

                            case "16+":
                                title.ageLimiter = AgeLimiter.minor;
                                break;

                            case "18+":
                                title.ageLimiter = AgeLimiter.adult;
                                break;
                        }
                        break;
                }
            }
        }

        public void getPersons()
        {
            foreach (var item in driver.FindElements(By.XPath("//div[@class='flex flex-row gap-4 overflow-x-auto flex-nowrap md:flex-wrap']"))[0].FindElements(By.XPath(".//a")))
            {
                switch (item.FindElement(By.XPath(".//p[@class='Typography_caption___iNir Typography_color-textSecondary__iFFB7 Typography_lineClamp-1__ijYgf Typography_lineClamp__Pa1wi']")).Text)
                {
                    case "Автор":
                        title.persons.Add(new Person()
                        {
                            url = item.GetAttribute("href"),
                            type = PersonType.author
                        });
                        break;

                    case "Издатель":
                        title.persons.Add(new Person()
                        {
                            url = item.GetAttribute("href"),
                            type = PersonType.publishing
                        });
                        break;

                    case "Художник":
                        title.persons.Add(new Person()
                        {
                            url = item.GetAttribute("href"),
                            type = PersonType.painter
                        });
                        break;
                }
            }

            foreach (var item in driver.FindElements(By.XPath("//div[@class='flex flex-row gap-4 overflow-x-auto flex-nowrap md:flex-wrap']"))[1].FindElements(By.XPath(".//a")))
            {
                title.persons.Add(new Person()
                {
                    url = item.GetAttribute("href"),
                    type = PersonType.translator
                });
            }

            foreach (var person in title.persons)
            {
                driver.Navigate().GoToUrl(person.url);
                if (person.type == PersonType.translator)
                {
                    person.name = driver.FindElement(By.XPath("//h1[@class='Typography_h3___I3IT Typography_lineClamp-1__ijYgf Typography_lineClamp__Pa1wi Team_name__FLyYc']")).Text;
                    person.altName = driver.FindElements(By.XPath("//p[@class='Typography_body1__YTqxB Team_tagline__wIfbi']")).Count() > 0 ? driver.FindElement(By.XPath("//p[@class='Typography_body1__YTqxB Team_tagline__wIfbi']")).Text : null;
                    person.image = new Image(driver.FindElement(By.XPath("//div[@class='Avatar_avatar__hG0bH Avatar_colorDefault__MHL29 Team_avatar__lj_mG']/img")).GetAttribute("src"));
                    title.contacts = driver.FindElements(By.XPath("//a[@class='Button_button___CisL Button_button___CisL Button_text__IGNQ6 Button_text-primary__WgBRV Team_socialButton__2oZZA']")).Select(x => x.GetAttribute("href")).ToList();
                    person.description = driver.FindElements(By.XPath("//div[@class='Team_content__Tshsc']/div[@class='Team_section__CdfV4']")).Count > 0 ? driver.FindElement(By.XPath("//div[@class='Team_content__Tshsc']/div[@class='Team_section__CdfV4']")).Text : null;
                }
                else
                {
                    person.name = driver.FindElement(By.XPath("//h1[@class='Typography_h3___I3IT Typography_lineClamp-1__ijYgf Typography_lineClamp__Pa1wi']")).Text;
                    person.altName = driver.FindElements(By.XPath("//h6[@class='Typography_h6__VMBDX Typography_color-textSecondary__iFFB7 Typography_gutterBottom__Q_9Ve AltName_altname__3NUlt']")).Count > 0
                        ? driver.FindElement(By.XPath("//h6[@class='Typography_h6__VMBDX Typography_color-textSecondary__iFFB7 Typography_gutterBottom__Q_9Ve AltName_altname__3NUlt']")).Text
                        : null;
                    if (driver.FindElements(By.XPath("//button[@class='Typography_body1__YTqxB Typography_color-textSecondary__iFFB7 cursor-pointer mt-1 p-0 border-0']")).Count > 0)
                        driver.FindElement(By.XPath("//button[@class='Typography_body1__YTqxB Typography_color-textSecondary__iFFB7 cursor-pointer mt-1 p-0 border-0']")).Click();
                    person.description = driver.FindElements(By.XPath("//div[@class='m-0.5 mb-1']/div[@class='Section_section__sLePo']/div")).Count > 0
                        ? driver.FindElement(By.XPath("//div[@class='m-0.5 mb-1']/div[@class='Section_section__sLePo']/div")).Text
                        : null;
                    person.image = driver.FindElements(By.XPath("//div[@class='Avatar_avatar__hG0bH Avatar_colorDefault__MHL29 Avatar_avatar__CanNK']/img")).Count() > 0
                        ? new Image(driver.FindElement(By.XPath("//div[@class='Avatar_avatar__hG0bH Avatar_colorDefault__MHL29 Avatar_avatar__CanNK']/img")).GetAttribute("src"))
                        : null;

                }
            }
        }
        public void getChapters()
        {
            driver.Navigate().GoToUrl($"{page.baseUrl}{page.catalogUrl}/omniscient-reader?p=chapters");
            var chapterCount = int.Parse(driver.FindElement(By.XPath("//div[@class='flex flex-col items-start'][1]/p[2]")).Text);

            while (chapterCount != driver.FindElements(By.XPath("//div[@class='Chapters_container__5S4y_'][1]/a")).Count)
            {
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                js.ExecuteScript("window.scrollTo(0, document.getElementsByTagName(\"body\")[0].scrollHeight)");
            }

            var i = 0;

            foreach(var chapter in driver.FindElements(By.XPath("//div[@class='Chapters_container__5S4y_'][1]/a")))
            {
                //chapter.FindElements(By.XPath(".//div[@class='Chapters_info__UlRTg']"))
                if (chapter.FindElements(By.XPath(".//div[@class='Chapters_info__UlRTg']/span")).Count == 0) continue;
                title.chapters.Add(new Chapter()
                {
                    volume = chapter.FindElement(By.XPath(".//span[@class='Chapters_tome__tBNYU']")).Text,
                    number = Regex.Matches(chapter.FindElement(By.XPath(".//p[@class='Typography_body1__YTqxB Typography_color-inherit__Wstd_ Chapters_title__ocJer']")).Text, @"Глава\s*(\d+)\s*•?")[0].Groups[1].Value,
                    url = chapter.GetAttribute("href"),
                    translator = new Person()
                    {
                        name = chapter.FindElement(By.XPath(".//div[@class='flex']/p")).Text,
                        type = PersonType.translator
                    }
                });
                i++;
                if (i==2) break;
            }
        }

        public async void getImages()
        {
            title.chapters.Reverse();
            foreach (var chapter in title.chapters)
            {
                var chapterNumber = Regex.Matches(chapter.url, @"\/\d+")[0].Value;

                HttpClient client = new HttpClient();
                var response = client.GetAsync($"https://api.remanga.org/api/titles/chapters{chapterNumber}").Result.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                Classes.General.Json json = JsonConvert.DeserializeObject<Classes.General.Json>(responseBody);

                foreach (var page in json.content.pages)
                {
                    foreach (var image in page)
                    {
                        chapter.images.Add(new Image(image.link));
                    }
                }

                RemangaUploader uploader = new RemangaUploader(server);
                uploader.upload(chapter);

                chapter.images = new List<IImage> { };
            }
        }
    }
}
