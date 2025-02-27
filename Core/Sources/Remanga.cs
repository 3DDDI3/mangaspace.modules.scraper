using Newtonsoft.Json;
using OpenQA.Selenium;
using RussianTransliteration;
using Scraper.Core.Enums;
using Scraper.Core.Interfaces;
using System.Text.RegularExpressions;
using Scraper.Core.Classes.General;
using OpenQA.Selenium.Edge;
using Scraper.Core.DTO;
using Microsoft.Extensions.Logging;
using RestSharp;
using Scraper.Core.Classes.RabbitMQ;
using Scraper.Core.Json.Remanga;
using Chapter = Scraper.Core.Classes.General.Chapter;
using System.Net;
using Scraper.Core.Classes.Uploader;
using System.Reflection.Metadata;
using OpenQA.Selenium.Support.UI;
using System;
using System.Reflection;

namespace Scraper.Core.Sources
{
    public class Remanga:IScraper
    {
        private IFTPServer ftpServer;
        private RMQ rmq;
        private Configuration conf;
        public string baseUrl { get; set; }
        public EdgeDriver driver { get; set; }
        public IPage page { get; set; }
        public ITitle title { get; set; }
        public Server server { get; set; }

        private ILogger logger;

        public Remanga(Configuration conf, RMQ rmq, ILogger logger)
        {
            this.logger = logger;
            this.rmq = rmq;
            this.conf = conf;
            server = new Server(conf, logger, rmq);

            title = new Title()
            { 
                persons = new List<IPerson>(),
                contacts = new List<string>(),
                genres = new List<string>(),
                chapters = new List<IChapter>()
            };

            page = new Page()
            {
                baseUrl = conf.scraperConfiguration.baseUrl,
                catalogUrl = conf.scraperConfiguration.catalogUrl,
                pageUrl = conf.scraperConfiguration.pages,
                pages = rmq.rmqMessage.RequestDTO.pages
            };

            ftpServer = new FTPServer()
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
            edgeOptions.AddArgument("--log-level=3");
            driver = new EdgeDriver(edgeOptions);
        }

        public void parse()
        {
            for (int i = 0; i <= page.pages.Count; i++)
            {
                rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Получение тайтлов на {i + 1} странице"));

                driver.Navigate().GoToUrl($"{page.baseUrl}/{page.catalogUrl}{page.pageUrl}{page.pages[i]}");
                var titles = driver.FindElements(By.XPath("//div[@class='Grid_gridItem__aPUx1 p-1']/a"))
                    .Select(x => x.GetAttribute("href"))
                    .ToList();

                rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Получение тайтлов на {i + 1} странице завершено")); ;
              
                foreach (var _title in titles)
                {
                    driver.Navigate().GoToUrl(_title);
                    getTitleInfo();

                    server.execute("v1.0/titles", title, Method.Post);
                    server.execute("v1.0/titles", new Dictionary<string, string>() { ["eng_name"] = title.altName, ["ru_name"] = title.name }, Method.Get);
                    var createdTitle = JsonConvert.DeserializeObject<Title[]>(server.response.Content)[0];
                    rmq.send("scraper", "parseTitleResponse", new ResponseDTO(
                        new TitleDTO(
                            null,
                            new List<ChapterDTO>(),
                            createdTitle.name),
                        new ScraperDTO("", "")
                        )
                    );

                    getChapters();                   
                    getPersons();
                    //getImages();

                    break;
                    break;
                }
                break;
            }

            //rmq.send("information", "errorLog", new LogDTO(null, true));
            driver.Quit();
        }
        
        /// <summary>
        /// Получение всех глав тайтла для дальнейшего парсинта
        /// </summary>
        public void getAllChapters()
        {
            driver.Navigate().GoToUrl(rmq.rmqMessage.RequestDTO.titleDTO.url);
            getChapters();
            rmq.send("information", "errorLog", new LogDTO(null, true));
            driver.Quit();
        }

        /// <summary>
        /// Парсинг конктретных глав
        /// </summary>
        public void parseChapters()
        {
            var chapterDTO = rmq.rmqMessage.RequestDTO.titleDTO.chapterDTO;

            try
            {
                driver.Navigate().GoToUrl(Regex.Replace(chapterDTO[0].url, @"\/\d+$", ""));
            }
            catch (Exception ex)
            {
                rmq.send("information", "errorLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Ошибка при попытке откртия страницы {driver.Url}"));
            }

            getTitleInfo();
            getPersons();

            server.execute("v1.0/titles", title, Method.Post);

            foreach (var chapter in chapterDTO)
            {
                title.chapters = new List<IChapter>() { new Chapter() { url = chapter.url } };
            }

            getImages();

            rmq.send("information", "informationLog", new LogDTO(null, true));
            rmq.send("information", "errorLog", new LogDTO(null, true));

            driver.Close();
        }

        public void getTitleInfo()
        {
            title = new Title()
            {
                persons = new List<IPerson>(),
                contacts = new List<string>(),
                genres = new List<string>(),
                cover = new List<IImage>(),
                chapters = new List<IChapter>()
            };

            title.name = driver.FindElement(By.XPath("//h1[@class='Typography_h3___I3IT']")).Text;

            rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Получение информации о тайтле {title.name}"));

            if (driver.FindElements(By.XPath("//div[@class='relative aspect-[2/3] overflow-hidden rounded-[16px] bg-[var(--bg-primary)] shadow-xl']/img")).Count() > 0)
                title.cover.Add(new Image(driver.FindElement(By.XPath("//div[@class='relative aspect-[2/3] overflow-hidden rounded-[16px] bg-[var(--bg-primary)] shadow-xl']/img")).GetAttribute("src")));

            if (driver.FindElements(By.XPath("//div[@class='flex p-4']/button[@class='Button_button___CisL Button_button___CisL Button_contained__8_KFk Button_contained-primary__IViyX Button_fullWidth__Dgoqh']")).Count > 0)
                driver.FindElement(By.XPath("//div[@class='flex p-4']/button[@class='Button_button___CisL Button_button___CisL Button_contained__8_KFk Button_contained-primary__IViyX Button_fullWidth__Dgoqh']")).Click();

            if (driver.FindElements(By.XPath("//div[@class='CookieConsent_consentContainer__Wi3DI']")).Count() > 0)
                driver.FindElement(By.XPath("//button[@class='Button_button___CisL Button_button___CisL Button_contained__8_KFk CookieConsent_consentButton__sAqzY']")).Click();

            ftpServer.connect();

            if (conf.appConfiguration.production)
            {
                try
                {
                    if (!ftpServer.client.DirectoryExists($"{ftpServer.rootPath}{RussianTransliterator.GetTransliteration(Regex.Replace(title.name, @"[\/\\\*\&\]\[\|]+", ""))}"))
                        ftpServer.client.CreateDirectory($"{ftpServer.rootPath}{RussianTransliterator.GetTransliteration(Regex.Replace(title.name, @"[\/\\\*\&\]\[\|]+", ""))}");

                    ftpServer.rootPath += $"{RussianTransliterator.GetTransliteration(Regex.Replace(title.name, @"[\/\\\*\&\]\[\|]+", ""))}/";
                }
                catch (Exception ex)
                {
                    rmq.send("information", "errorLog", new LogDTO(ex.Message));
                }
            }
            else
            {
                rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Создание папки для тайтла на сервере"));
                ftpServer.rootPath = @$"\\wsl$\Ubuntu\home\laravel\mangaspace\src\storage\app\media\";
                if (!Directory.Exists($"{ftpServer.rootPath}{RussianTransliterator.GetTransliteration(Regex.Replace(title.name, @"[\/\\\*\&\]\[\|]+", ""))}"))
                    Directory.CreateDirectory($"{ftpServer.rootPath}{RussianTransliterator.GetTransliteration(Regex.Replace(title.name, @"[\/\\\*\&\]\[\|]+", ""))}");

                ftpServer.rootPath += @$"{RussianTransliterator.GetTransliteration(Regex.Replace(title.name, @"[\/\\\*\&\]\[\|]+", ""))}\";

                if (!Directory.Exists(@$"{ftpServer.rootPath}\covers"))
                    Directory.CreateDirectory(@$"{ftpServer.rootPath}\covers");

                rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Создание папки для тайтла завершено успешно"));

                rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Скачивание обложек тайтла"));

                RemangaUploader remangaUploader = new RemangaUploader(ftpServer, conf);
                remangaUploader.uploadCovers(title.cover);

                rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Скачивание обложек тайтла успешно завершено"));
            }

            ftpServer.disconnect();

            var arr = driver.FindElement(By.XPath("//div[@class='flex flex-col items-start gap-1']/h5")).Text.Split(" ");
            title.type = arr[0];
            title.releaseYear = ushort.Parse(arr[1]);
            var block = driver.FindElements(By.XPath("//button[@class='Typography_body1__YTqxB Typography_color-textSecondary__iFFB7 cursor-pointer mt-1 p-0 border-0']"));
            if (block.Count > 0) 
                block[0].Click();
            title.description = driver.FindElement(By.XPath("//div[@class='flex flex-col gap-4']")).FindElement(By.XPath("//div[@class='Typography_body1__YTqxB']")).Text;
            if (driver.FindElements(By.XPath("//div[@class='flex items-end relative cursor-pointer']")).Count > 0)
                driver.FindElement(By.XPath("//div[@class='flex items-end relative cursor-pointer']")).Click();
            title.otherNames = driver.FindElement(By.XPath("//div[@class='flex items-end relative']/p[1]")).Text;
            if (driver.FindElements(By.XPath("//span[@class='Chip_chip__0JxfA Chip_white__lcP51']")).Count() > 0) 
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
                                title.translateStatus = TranslateStatus.freezed;
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
            rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Получение информации о тайтле {title.name} завершено успешно"));
        }

        
        public void getPersons()
        {
            /**
             * Прописать логику получения информации о переводчике из страницы главы
             */

            try
            {
                driver.Navigate().GoToUrl($"{Regex.Replace(driver.Url, @"\?p=chapters$", "")}?p=about");
            }
            catch (Exception ex)
            {
                rmq.send("information", "errorLog", new LogDTO(ex.Message));
            }

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
                try
                {
                    driver.Navigate().GoToUrl(person.url);
                }
                catch (Exception ex)
                {
                    rmq.send("information", "errorLog", new LogDTO(ex.Message));
                }

                if (driver.FindElements(By.XPath("//div[@class='Avatar_avatar__hG0bH Avatar_colorDefault__MHL29 Avatar_avatar__CanNK']/img")).Count() > 0)
                    person.image = new Image(driver.FindElement(By.XPath("//div[@class='Avatar_avatar__hG0bH Avatar_colorDefault__MHL29 Avatar_avatar__CanNK']/img")).GetAttribute("src"));
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
            try
            {
                driver.Navigate().GoToUrl($"{driver.Url}?p=chapters");
            }
            catch (Exception ex)
            {
                rmq.send("information", "errorLog", new LogDTO(ex.Message));
            }

            if (driver.FindElements(By.XPath("//button[@class='Button_button___CisL Button_button___CisL Button_contained__8_KFk Button_contained-primary__IViyX Button_fullWidth__Dgoqh']")).Count() > 1)
                driver.FindElements(By.XPath("//button[@class='Button_button___CisL Button_button___CisL Button_contained__8_KFk Button_contained-primary__IViyX Button_fullWidth__Dgoqh']"))[1].Click();

            var pageYOffset = 0;
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

            List<IWebElement> chapters = new List<IWebElement>();

            while (pageYOffset < int.Parse(js.ExecuteScript("return document.getElementsByTagName(\"body\")[0].scrollHeight").ToString()))
            {
                pageYOffset = int.Parse(js.ExecuteScript("return document.getElementsByTagName(\"body\")[0].scrollHeight").ToString());
                Thread.Sleep(1000);
                js.ExecuteScript("window.scrollTo(0, document.getElementsByTagName(\"body\")[0].scrollHeight);");
                break;
            }

            chapters = driver.FindElements(By.XPath("//div[@class='Chapters_container__5S4y_'][1]/a")).Take(9).ToList();

            foreach (var chapter in chapters)
            {
                //chapter.FindElements(By.XPath(".//div[@class='Chapters_info__UlRTg']"))
                if (chapter.FindElements(By.XPath(".//div[@class='Chapters_info__UlRTg']/*[local-name()='svg' and @class='SvgIcon_root__n_a0S Chapters_icon__W0D5Y Chapters_defaultMoneyColorIcon__RnQRw']")).Count == 1)
                    continue;

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
               
                if (rmq.rmqMessage.RequestDTO.scraperDTO.action == "getChapters")
                {
                    var _title = new ResponseDTO(
                        new TitleDTO("", new List<ChapterDTO>() { new ChapterDTO(
                            chapter.GetAttribute("href"),
                            Regex.Matches(chapter.FindElement(By.XPath(".//p[@class='Typography_body1__YTqxB Typography_color-inherit__Wstd_ Chapters_title__ocJer']")).Text, @"Глава\s*(\d+)\s*•?")[0].Groups[1].Value,
                            chapter.FindElement(By.XPath(".//div[@class='flex']/p")).Text,
                            null,
                            false,
                            chapter.Equals(chapters.Last()) ? true : false
                            )
                        }),
                        new ScraperDTO("", "")
                    );

                    rmq.send("scraper", "getChapterResponse", _title);
                }
            }
        }

        public async void getImages()
        {
            title.chapters.Reverse();
            foreach (var chapter in title.chapters)
            {
                var chapterNumber = Regex.Matches(chapter.url, @"\/\d+")[0].Value;

                Json.Remanga.Chapter chapterJson = null;

                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        var res = client.GetAsync($"https://api.remanga.org/api/titles/chapters{chapterNumber}").Result;

                        if (res.StatusCode == HttpStatusCode.Unauthorized)
                            continue;

                        using (var response = res.EnsureSuccessStatusCode())
                        {
                            string responseBody = await response.Content.ReadAsStringAsync();
                            chapterJson = JsonConvert.DeserializeObject<Json.Remanga.Chapter>(responseBody);
                        }
                    }
                }
                catch (Exception ex)
                {
                    rmq.send("information", "errorLog", new LogDTO(ex.Message));
                }

                Translator translatorJson = null;

                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        using (var response = client.GetAsync($"https://api.remanga.org/api/publishers/{chapterJson.content.publishers[0].dir}").Result.EnsureSuccessStatusCode())
                        {
                            string responseBody = await response.Content.ReadAsStringAsync();
                            translatorJson = JsonConvert.DeserializeObject<Translator>(responseBody);
                        }
                    }
                }
                catch (Exception ex)
                {
                    rmq.send("information", "errorLog", new LogDTO(ex.Message));
                }

                chapter.number = chapterJson.content.chapter;
                chapter.volume = chapterJson.content.tome;
                chapter.name = chapterJson.content.name;
                chapter.translator.name = translatorJson.content.name;
                chapter.translator.description = translatorJson.content.description;
                chapter.translator.type = PersonType.translator;
                chapter.translator.image = new Image($"{conf.scraperConfiguration.baseUrl}{translatorJson.content.img.high}");

                string jpg = "", jpeg = "", png = "", webp = "";

                for (int i = 0; i < chapterJson.content.pages.Length; i++)
                {
                    var page = chapterJson.content.pages[i];
                    var subImages = new List<IImage>();
                    for (int j = 0; j < page.Length; j++)
                    {
                        subImages.Add(new Image(page[j].link));

                        switch (Regex.Matches(page[j].link, @".(\w+)$")[0].Groups[1].Value)
                        {
                            case "webp":
                                webp += $"{i + 1}_{j + 1},";
                                break;
                            case "jpg":
                                jpg += $"{i + 1}_{j + 1},";
                                break;
                            case "jpeg":
                                jpeg += $"{i + 1}_{j + 1},";
                                break;
                            case "png":
                                png += $"{i + 1}_{j + 1},";
                                break;
                        }
                    }
                    chapter.images.Add(subImages);
                }

                webp = webp.Length > 0 ? webp.Substring(0, webp.LastIndexOf(",")) : webp;
                jpeg = jpeg.Length > 0 ? jpeg.Substring(0, jpeg.LastIndexOf(",")) : jpeg;
                jpg = jpg.Length > 0 ? jpg.Substring(0, jpg.LastIndexOf(",")) : jpg;
                png = png.Length > 0 ? png.Substring(0, png.LastIndexOf(",")) : png;

                chapter.extensions = $"{jpeg}|{jpg}|{webp}|{png}";

                var _chapter = JsonConvert.SerializeObject(chapter);

                try
                {
                    RemangaUploader uploader = new RemangaUploader(ftpServer, conf);
                    uploader.uploadChapterImages(chapter);

                    server.execute("v1.0/titles", new Dictionary<string, string>() { ["eng_name"] = title.altName, ["ru_name"] = title.name }, Method.Get);
                    var createdTitle = JsonConvert.DeserializeObject<Title[]>(server.response.Content)[0];
                    server.execute($"v1.0/titles/{createdTitle.slug}/chapters", chapter, Method.Post);
                    server.execute($"v1.0/titles/{createdTitle.slug}/chapters/{chapter.number}/images", chapter, Method.Post);
                }
                catch (Exception ex)
                {
                    rmq.send("information", "errorLog", new LogDTO(ex.Message));
                }

                ResponseDTO responseDTO = new ResponseDTO(
                    new TitleDTO(null,
                        new List<ChapterDTO>() {
                            new ChapterDTO(null,
                                chapter.number,
                                chapter.translator.name,
                                title.name,
                                chapter.Equals(title.chapters.First()) ? true : false,
                                chapter.Equals(title.chapters.Last()) ? true : false
                            )
                        },
                        title.name
                    ),
                    new ScraperDTO(
                        rmq.rmqMessage.RequestDTO.scraperDTO.action,
                        rmq.rmqMessage.RequestDTO.scraperDTO.engine
                    )
                );

                if (rmq.rmqMessage.RequestDTO.scraperDTO.action == "parseChapters")
                    rmq.send("scraper", "parseChapterResponse", responseDTO);

                if (rmq.rmqMessage.RequestDTO.scraperDTO.action == "parseTitles")
                    rmq.send("scraper", "parseTitleResponse", responseDTO);
            }
        }
    }
}
