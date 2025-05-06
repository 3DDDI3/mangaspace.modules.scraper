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
using TitleJson = Scraper.Core.Json.Remanga.Title;
using ChapterJson = Scraper.Core.Json.Remanga.Chapter;
using System.Net;
using Scraper.Core.Classes.Uploader;
using Scraper.Core.Json.Mangalib;
using Scraper.Core.Json.Mangaovh;
using Title = Scraper.Core.Classes.General.Title;
using Page = Scraper.Core.Classes.General.Page;
using System.IO;

namespace Scraper.Core.Sources
{
    public class Remanga:IScraper
    {
        private IFTPServer ftpServer;
        private RMQ rmq;
        private Configuration conf;
        private Server externalServer;
        private CustomDirectory directory;
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
            this.externalServer = new Server("https://api.remanga.org/api/v2/titles/");

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

            List<KeyValuePair<string, string>> args = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("eng_name", title.altName),
                new KeyValuePair<string, string>("ru_name",title.name)
            };

            server.execute("v1.0/titles", Method.Get, args);

            var createdTitle = JsonConvert.DeserializeObject<dynamic>(Regex.Replace(server.response.Content, @"(^{""data"":\[)|(\]?,?((""meta"")|(""links"")):{""?[\w0-9\\\/.\:\?\=\,""\[\]\{\}\&\;\s]+""?\}?)", ""));

            server.execute($"v1.0/titles/{createdTitle.slug}/covers", title.covers, Method.Post);

            server.execute($"v1.0/titles/{createdTitle.slug}/genres", title, Method.Post);
            directory.rootPath = Path.Combine(conf.appConfiguration.production ? conf.appConfiguration.prod_root : conf.appConfiguration.local_root, "media");
            directory.createDirectory("persons");

            RemangaUploader uploader = new RemangaUploader(directory, conf);
            uploader.uploadPersonalImages(title.persons);

            server.execute($"v1.0/titles/{createdTitle.slug}/persons", title.persons, Method.Post);

            
            foreach (var chapter in chapterDTO)
            {
                externalServer.externalExecute($"/chapters/{Regex.Match(chapter.url, @"\d+$").Value}", Method.Get);
             
                var chapters = JsonConvert.DeserializeObject<Scraper.Core.Json.Remanga.Page>(externalServer.response.Content);
                title.chapters = new List<IChapter>();


                var __image = new List<List<IImage>>();
                foreach (var page in chapters.pages)
                {
                    var image = new List<IImage>();
                    image = page.Select(x => (IImage)new Image(x.link)).ToList();
                    __image.Add(image);
                }

                title.chapters.Add(new Chapter() { images=__image, name=chapters.name, volume=chapters.tome, number=chapters.index});
            }

            getImages();

            rmq.send("information", "informationLog", new LogDTO(null, true));
            rmq.send("information", "errorLog", new LogDTO(null, true));

            driver.Close();
        }

        public void getTitleInfo()
        {
            var str = Regex.Replace(driver.Url, @"(https://remanga.org\/manga\/)|(\/main)|(\/chapters)", "");

            externalServer.externalExecute($"/{str}", Method.Get);
            var _title = JsonConvert.DeserializeObject<TitleJson>(externalServer.response.Content);

            title = new Title()
            {
                persons = new List<IPerson>(),
                contacts = new List<string>(),
                genres = new List<string>(),
                covers = new List<IImage>(),
                chapters = new List<IChapter>()
            };

            title.name = _title.main_name;
            title.covers = new List<IImage>() { new Image($"https://remanga.org{_title.cover.high}") };

            rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Получение информации о тайтле {title.name}"));

            string path = conf.appConfiguration.production ? conf.appConfiguration.prod_root : conf.appConfiguration.local_root;
            directory = new CustomDirectory(path);
            directory.createDirectory("media");
            directory.createDirectory("titles");

            directory.createDirectory(RussianTransliterator.GetTransliteration(Regex.Replace(title.name, @"[\/\\\*\&\]\[\|\.]+", "")));

            rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Создание папки для тайтла на сервере"));

            directory.createDirectory("covers");

            rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Создание папки для тайтла завершено успешно"));

            rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Скачивание обложек тайтла"));

            RemangaUploader remangaUploader = new RemangaUploader(directory, conf);
            remangaUploader.uploadCovers(title.covers);

            rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Скачивание обложек тайтла успешно завершено"));

            title.type = _title.type.name;

            title.releaseYear = ushort.Parse(_title.issue_year);

            title.description = _title.description;

            title.otherNames = _title.another_name;

            switch (_title.status.name)
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

            switch (_title.translate_status.name)
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

            switch (_title.age_limit.name)
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

            title.genres = _title.genres.Select(x => x.name).ToList();

            rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Получение информации о тайтле {title.name} завершено успешно"));
        }
        
        public void getPersons()
        {
            try
            {
                driver.Navigate().GoToUrl($"{Regex.Replace(driver.Url, @"\?p=chapters$", "")}?p=about");
            }
            catch (Exception ex)
            {
                rmq.send("information", "errorLog", new LogDTO(ex.Message));
            }

            var str = Regex.Replace(driver.Url, @"(https://remanga.org\/manga\/)|(\/main)|(\/chapters)", "");

            externalServer.externalExecute($"/{str}", Method.Get);
            var _title = JsonConvert.DeserializeObject<TitleJson>(externalServer.response.Content);

            foreach (var item in _title.branches)
            {
                foreach (var publisher in item.publishers)
                {
                    title.persons.Add(new Person()
                    {
                        url = null,
                        name = publisher.name,
                        type = PersonType.translator,
                        altName = RussianTransliterator.GetTransliteration(Regex.Replace(publisher.name, @"[\/\\\*\&\]\[\|\.]+", "")),
                        images = new List<IImage>() { new Image($"https://remanga.org/media/{publisher.cover.high}") }
                    });
                }
            }
        }

        public void getChapters()
        {
            var str = Regex.Replace(driver.Url, @"(https://remanga.org\/manga\/)|(\/main)|(\/chapters)", "");
            externalServer.externalExecute($"/{str}", Method.Get);
            var _title = JsonConvert.DeserializeObject<TitleJson>(externalServer.response.Content);

            foreach (var branch in _title.branches)
            {
                List<KeyValuePair<string, string>> args = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("branch_id", branch.id),
                };

                externalServer.externalExecute($"/chapters", Method.Get, args);
                var chapter = JsonConvert.DeserializeObject<ChapterJson>(externalServer.response.Content);

                var url = Regex.Replace(driver.Url, @"(\/main)|(\/chapters)$", "");

                foreach (var item in chapter.results)
                {
                    title.chapters.Add(new Chapter()
                    {
                        volume = item.tome,
                        number = item.chapter,
                        url = $"{url}/{item.id}",
                        translator = new Person()
                        {
                            name = item.publishers[0].name,
                            type = PersonType.translator
                        }
                    });

                    if (rmq.rmqMessage.RequestDTO.scraperDTO.action == "getChapters")
                    {
                        var title = new ResponseDTO(
                            new TitleDTO("", new List<ChapterDTO>() {
                                    new ChapterDTO(
                                        $"{url}/{item.id}",
                                        item.index,
                                        item.tome,
                                        item.publishers[0].name,
                                        null,
                                        true,
                                        chapter.next == null && item.Equals(chapter.results.Last()) ? true : false
                                    )
                            }),
                            new ScraperDTO("", "")
                        );

                        rmq.send("scraper", "getChapterResponse", title);
                    }
                }

                var _chapter = chapter;

                while (_chapter.next != null)
                {
                    var page = Regex.Match(_chapter.next, $@"page=(\d+)");

                    args = new List<KeyValuePair<string, string>>()
                    {
                        new KeyValuePair<string, string>("branch_id", branch.id),
                        new KeyValuePair<string, string>("page", page.Groups[1].Value),
                    };

                    externalServer.externalExecute($"/chapters", Method.Get, args);
                    _chapter = JsonConvert.DeserializeObject<ChapterJson>(externalServer.response.Content);

                    foreach (var item in _chapter.results)
                    {
                        title.chapters.Add(new Chapter()
                        {
                            volume = item.tome,
                            number = item.id,
                            url = $"{url}/{item.id}",
                            translator = new Person()
                            {
                                name = item.publishers[0].name,
                                type = PersonType.translator
                            }
                        });

                        if (rmq.rmqMessage.RequestDTO.scraperDTO.action == "getChapters")
                        {
                            var title = new ResponseDTO(
                                new TitleDTO("", new List<ChapterDTO>() {
                                    new ChapterDTO(
                                        $"{url}/{item.id}",
                                        item.index,
                                        item.tome,
                                        item.publishers[0].name,
                                        null,
                                        false,
                                       _chapter.next == null && item.Equals(_chapter.results.Last()) ? true : false
                                    )
                                }),
                                new ScraperDTO("", "")
                            );

                            rmq.send("scraper", "getChapterResponse", title);
                        }
                    }
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

                //Translator translatorJson = null;

                //try
                //{
                //    using (HttpClient client = new HttpClient())
                //    {
                //        using (var response = client.GetAsync($"https://api.remanga.org/api/publishers/{chapterJson.content.publishers[0].dir}").Result.EnsureSuccessStatusCode())
                //        {
                //            string responseBody = await response.Content.ReadAsStringAsync();
                //            translatorJson = JsonConvert.DeserializeObject<Translator>(responseBody);
                //        }
                //    }
                //}
                //catch (Exception ex)
                //{
                //    rmq.send("information", "errorLog", new LogDTO(ex.Message));
                //}

                //chapter.number = chapterJson.content.chapter;
                //chapter.volume = chapterJson.content.tome;
                //chapter.name = chapterJson.content.name;
                //chapter.translator.name = translatorJson.content.name;
                //chapter.translator.description = translatorJson.content.description;
                //chapter.translator.type = PersonType.translator;
                //chapter.translator.images = new List<IImage>() { new Image($"{conf.scraperConfiguration.baseUrl}{translatorJson.content.img.high}") };

                string jpg = "", jpeg = "", png = "", webp = "";

                //for (int i = 0; i < chapterJson.content.pages.Length; i++)
                //{
                //    var page = chapterJson.content.pages[i];
                //    var subImages = new List<IImage>();
                //    for (int j = 0; j < page.Length; j++)
                //    {
                //        subImages.Add(new Image(page[j].link));

                //        switch (Regex.Matches(page[j].link, @".(\w+)$")[0].Groups[1].Value)
                //        {
                //            case "webp":
                //                webp += $"{i + 1}_{j + 1},";
                //                break;
                //            case "jpg":
                //                jpg += $"{i + 1}_{j + 1},";
                //                break;
                //            case "jpeg":
                //                jpeg += $"{i + 1}_{j + 1},";
                //                break;
                //            case "png":
                //                png += $"{i + 1}_{j + 1},";
                //                break;
                //        }
                //    }
                //    chapter.images.Add(subImages);
                //}

                webp = webp.Length > 0 ? webp.Substring(0, webp.LastIndexOf(",")) : webp;
                jpeg = jpeg.Length > 0 ? jpeg.Substring(0, jpeg.LastIndexOf(",")) : jpeg;
                jpg = jpg.Length > 0 ? jpg.Substring(0, jpg.LastIndexOf(",")) : jpg;
                png = png.Length > 0 ? png.Substring(0, png.LastIndexOf(",")) : png;

                chapter.extensions = $"{jpeg}|{jpg}|{webp}|{png}";

                var _chapter = JsonConvert.SerializeObject(chapter);

                try
                {
                    //RemangaUploader uploader = new RemangaUploader(directory, conf);
                    //uploader.uploadChapterImages(chapter);

                    //server.execute("v1.0/titles", new Dictionary<string, string>() { ["eng_name"] = title.altName, ["ru_name"] = title.name }, Method.Get);
                    //var createdTitle = JsonConvert.DeserializeObject<Title[]>(server.response.Content)[0];
                    //server.execute($"v1.0/titles/{createdTitle.slug}/chapters", chapter, Method.Post);
                    //server.execute($"v1.0/titles/{createdTitle.slug}/chapters/{chapter.number}/images", chapter, Method.Post);
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
                                "",
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
