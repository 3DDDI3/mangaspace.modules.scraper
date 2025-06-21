using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using RestSharp;
using RussianTransliteration;
using Scraper.Core.Classes;
using Scraper.Core.Classes.General;
using Scraper.Core.Classes.RabbitMQ;
using Scraper.Core.Classes.Uploader;
using Scraper.Core.DTO;
using Scraper.Core.Enums;
using Scraper.Core.Interfaces;
using Scraper.Core.Json.Mangalib;
using Scraper.Core.Json.Mangaovh;
using Scraper.Core.Json.Remanga;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Chapter = Scraper.Core.Classes.General.Chapter;
using MangalibJson = Scraper.Core.Json.Mangalib;
using Page = Scraper.Core.Classes.General.Page;

namespace Scraper.Core.Sources
{
    public class Mangalib : IScraper
    {
        private IFTPServer ftpServer;
        private RMQ rmq;
        private Configuration conf;
        private ILogger logger;
        private Server externalServer;
        private Server externalServerAlt;
        private MangalibJson.Title _title;

        public string baseUrl { get; set; }
        public EdgeDriver driver { get; set; }
        public IPage page { get; set; }
        public ITitle title { get; set; }
        public Server server { get; set; }

        public Mangalib(Configuration conf, RMQ rmq, ILogger logger)
        {
            this.logger = logger;
            this.rmq = rmq;
            this.conf = conf;
            server = new Server(conf, logger, rmq);
            externalServer = new Server("https://api.cdnlibs.org/api/manga", logger, conf);
            externalServerAlt = new Server("https://api.lib.social/api/manga", logger, conf);

            title = new Classes.General.Title()
            {
                persons = new List<IPerson>(),
                contacts = new List<string>(),
                genres = new List<string>(),
                chapters = new List<IChapter>()
            };

            page = new Page() { baseUrl = conf.scraperConfiguration.baseUrl, catalogUrl = conf.scraperConfiguration.catalogUrl, pageUrl = conf.scraperConfiguration.pages };

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

            if (conf.appConfiguration.containerized)
            {
                edgeOptions.AddArguments("--no-sandbox", "--disable-dev-shm-usage", "--headless");
                var service = EdgeDriverService.CreateDefaultService("/usr/local/bin");
                service.HideCommandPromptWindow = true;
                driver = new EdgeDriver(service, edgeOptions);
            }
            else
                driver = new EdgeDriver(edgeOptions);

            logger.LogInformation("driver started");
        }

        public void getPages()
        {
            driver.Navigate().GoToUrl($"{page.baseUrl}{page.catalogUrl}");
        }

        public void parse()
        {
            getPages();

            List<KeyValuePair<string, string>> args = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("site_id[]", "1")
            };

            externalServer.externalExecute($"/", Method.Get, args);
            var urls = JsonConvert.DeserializeObject<dynamic>(Regex.Replace(externalServer.response.Content, @"(^{""data"":)|(,?((""meta"")|(""links"")):{""?[\w0-9\\\/.\:\?\=\,""]+""?},?}?)", ""));

            foreach (var url in urls)
            {
                driver.Navigate().GoToUrl($"{conf.scraperConfiguration.baseUrl}/manga/{url.slug}");
                getTitleInfo();
                logger.LogInformation("title parsed");
                getChapters();
                logger.LogInformation("chapter parsed");
                getImages();
                break;
            }
        }

        public void getTitleInfo()
        {
            var url = rmq.rmqMessage.RequestDTO.titleDTO.chapterDTO.Count == 0 ? $"{driver.Url.Split("/")[driver.Url.Split("/").Count() - 1]}/" : rmq.rmqMessage.RequestDTO.titleDTO.chapterDTO[0].url;
            url = Regex.Replace(url, @"chapter[s]{0,1}[\?a-zA-Z_=\d&]+$", "");

            rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Получение информации о тайтле по ссылке {url} начато"));

            List<KeyValuePair<string, string>> args = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("fields[]", "eng_name"),
                    new KeyValuePair<string, string>("fields[]", "otherNames"),
                    new KeyValuePair<string, string>("fields[]", "summary"),
                    new KeyValuePair<string, string>("fields[]", "releaseDate"),
                    new KeyValuePair<string, string>("fields[]", "type_id"),
                    new KeyValuePair<string, string>("fields[]", "genres"),
                    new KeyValuePair<string, string>("fields[]", "teams"),
                    new KeyValuePair<string, string>("fields[]", "authors"),
                    new KeyValuePair<string, string>("fields[]", "publisher"),
                    new KeyValuePair<string, string>("fields[]", "manga_status_id"),
                    new KeyValuePair<string, string>("fields[]", "status_id"),
                    new KeyValuePair<string, string>("fields[]", "artists"),
                    new KeyValuePair<string, string>("fields[]", "format"),
                };

            externalServer.externalExecute($"/{url}", Method.Get, args);
            if (Regex.Match(externalServer.response.Content, "\"meta\"").Success)
                _title = JsonConvert.DeserializeObject<MangalibJson.Title>(Regex.Replace(externalServer.response.Content, @"(^{""data"":)|(,""meta"":{""\w+"":""\w+""}}$)", ""));
            else
                _title = JsonConvert.DeserializeObject<MangalibJson.Title>(Regex.Replace(externalServer.response.Content, @"(^{""data"":)|(\}$)", ""));


            title.name = _title.rus_name;
            title.altName = _title.eng_name;
            title.otherNames = string.Join(",", _title.otherNames);
            title.description = _title.summary;
            title.type = _title.type.label;

            switch (_title.scanlateStatus.label)
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

            switch (_title.status.label)
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

            title.releaseYear = ushort.Parse(_title.releaseDate);

            foreach (var genre in _title.genres)
            {
                title.genres.Add(genre.name);
            }

            switch (_title.ageRestriction.label)
            {
                case "16+":
                    title.ageLimiter = AgeLimiter.minor;
                    break;

                case "18+":
                    title.ageLimiter = AgeLimiter.adult;
                    break;
            }

            title.releaseFormat = string.Join(",", _title.format.Select(x => x.name));
            title.path = RussianTransliterator.GetTransliteration(Regex.Replace(title.name, @"[\/\\\*\&\]\[\|\.]+", ""));

            rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Получение информации о тайтле {title.name} завершено успешно"));

            externalServer.externalExecute($"{url}covers", Method.Get, args);

            MangalibJson.Covers[] covers = JsonConvert.DeserializeObject<MangalibJson.Covers[]>(Regex.Replace(externalServer.response.Content, @"(^{""data"":)|(}$)", ""));

            title.covers = new List<IImage>();

            foreach (var cover in covers)
            {
                title.covers.Add(new Image(cover.cover.orig));
            }

            CustomDirectory directory = new CustomDirectory(conf.appConfiguration.path);
            directory.createDirectory("media");
            directory.createDirectory("titles");
            directory.createDirectory(title.path);
            directory.createDirectory("covers");
            MangalibUploader uploader = new MangalibUploader(directory, conf, rmq);
            uploader.uploadCovers(title.covers);

            server.execute("v1.0/titles", title, Method.Post);

            args = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("eng_name", title.altName),
                new KeyValuePair<string, string>("ru_name",title.name)
            };

            server.execute("v1.0/titles", Method.Get, args);
            var createdTitle = JsonConvert.DeserializeObject<dynamic>(Regex.Replace(server.response.Content, @"(^{""data"":\[)|(\]?,?((""meta"")|(""links"")):{""?[\w0-9\\\/.\:\?\=\,""\[\]\{\}\&\;\s]+""?\}?)", ""));

            server.execute($"v1.0/titles/{createdTitle.slug}/covers", title.covers, Method.Post);

            server.execute($"v1.0/titles/{createdTitle.slug}/genres", title, Method.Post);

            getPersons();
        }

        public void getPersons()
        {
            foreach (var person in _title.authors)
            {
                title.persons.Add(new Person()
                {
                    name = person.name,
                    type = PersonType.author,
                    images = new List<IImage>() { new Image(person.cover.md) },
                    altName = RussianTransliterator.GetTransliteration(person.name)
                });
            }

            foreach (var person in _title.publisher)
            {
                title.persons.Add(new Person()
                {
                    name = person.name,
                    type = PersonType.publishing,
                    images = new List<IImage>() { new Image(person.cover.md) },
                    altName = RussianTransliterator.GetTransliteration(person.name)
                });
            }


            foreach (var person in _title.artists)
            {
                title.persons.Add(new Person()
                {
                    name = person.name,
                    type = PersonType.painter,
                    images = new List<IImage>() { new Image(person.cover.md) },
                    altName = RussianTransliterator.GetTransliteration(person.name)
                });
            }

            List<KeyValuePair<string, string>> args = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("eng_name", title.altName),
                new KeyValuePair<string, string>("ru_name",title.name)
            };

            CustomDirectory directory = new CustomDirectory(conf.appConfiguration.path);
            directory.createDirectory("media");
            directory.createDirectory("persons");

            MangalibUploader uploader = new MangalibUploader(directory, conf, rmq);
            uploader.uploadPersonalImages(title.persons);

            server.execute("v1.0/titles", Method.Get, args);
            var createdTitle = JsonConvert.DeserializeObject<dynamic>(Regex.Replace(server.response.Content, @"(^{""data"":\[)|(\]?,?((""meta"")|(""links"")):{""?[\w0-9\\\/.\:\?\=\,""\[\]\{\}\&\;\s]+""?\}?)", ""));
            server.execute($"v1.0/titles/{createdTitle.slug}/persons", title.persons, Method.Post);

        }

        public void getChapters()
        {           
            if (rmq.rmqMessage.RequestDTO.scraperDTO.action == "parseChapters")
            {
                var url = Regex.Replace(rmq.rmqMessage.RequestDTO.titleDTO.chapterDTO[0].url, "https://mangalib.me/ru/", "");
                var _url = url.Split("/");
                url = _url[0];

                driver.Navigate().GoToUrl($"https://mangalib.me/{url}");
                
                getTitleInfo();

                foreach (var chapter in rmq.rmqMessage.RequestDTO.titleDTO.chapterDTO)
                {
                    externalServer.externalExecute(chapter.url, Method.Get);
                    MangalibJson.Chapter _chapter = JsonConvert.DeserializeObject<MangalibJson.Chapter>(Regex.Replace(externalServer.response.Content, @"(""data"":)|(^{)|(}$)", ""));
                    List<List<IImage>> images = new List<List<IImage>>();

                    foreach (var image in _chapter.pages)
                    {
                        List<IImage> _images = new List<IImage>() { new Image($"https://img33.imgslib.link{image.url}") };
                        images.Add(_images);
                    }

                    title.chapters.Add(new Chapter()
                    {
                        images = images,
                        name = _chapter.name,
                        number = _chapter.number,
                        volume = _chapter.volume.ToString(),
                        translator = new Person() { name = _chapter.teams.First().name, images = new List<IImage>() { new Image(_chapter.teams.First().cover.orig) }, type = PersonType.translator, altName = RussianTransliterator.GetTransliteration(_chapter.teams[0].name)}
                    });
                }
            }
            else
            {
                var url = rmq.rmqMessage.RequestDTO.titleDTO.url != null ? Regex.Replace(Regex.Replace(rmq.rmqMessage.RequestDTO.titleDTO.url, @"\?[a-z&=\d]+$", ""), "https://mangalib.me/ru/", "") : driver.Url.Split("/")[driver.Url.Split("/").Length - 1];
                var _url = url.Split("/");
                url = _url[_url.Length - 1];

                logger.LogInformation($"current url: {url}");

                externalServer.externalExecute($"/{url}/chapters", RestSharp.Method.Get);

                MangalibJson.Chapters[] chapters = JsonConvert.DeserializeObject<MangalibJson.Chapters[]>(Regex.Replace(externalServer.response.Content, @"(""data"":)|(^{)|(}$)", ""));
                chapters = chapters.Take(5).ToArray();

                foreach (var chapter in chapters)
                {
                    foreach (var branch in chapter.branches)
                    {
                        List<List<IImage>> images = new List<List<IImage>>();
                        externalServer.externalExecute($"/{url}/chapter", Method.Get,
                            new List<KeyValuePair<string, string>>() {
                            new KeyValuePair<string, string>("branch_id", branch.branch_id.ToString()),
                            new KeyValuePair<string, string>( "volume", chapter.volume.ToString() ),
                            new KeyValuePair<string, string>( "number", chapter.number)
                            });
                        MangalibJson.Chapter _chapter = null;

                        _chapter = JsonConvert.DeserializeObject<MangalibJson.Chapter>(Regex.Replace(externalServer.response.Content, @"(""data"":)|(^{)|(}$)", ""));

                        List<IImage> _images = null;
                        foreach (var image in _chapter.pages)
                        {
                            _images = new List<IImage>() { new Image($"https://img33.imgslib.link{image.url}") };
                            images.Add(_images);
                        }

                        title.chapters.Add(new Chapter() {
                            name = _chapter.name, 
                            volume = _chapter.volume, 
                            number = _chapter.number, 
                            translator = new Person() { 
                                name = _chapter.teams[0].name, 
                                altName = _chapter.teams[0].name,
                                type = PersonType.translator,
                            }, 
                            images = images 
                        });
                        
                        var _title = new ResponseDTO(
                            new TitleDTO("", new List<ChapterDTO>() {
                                    new ChapterDTO(
                                        $"{externalServer.client.Options.BaseUrl}/{url}/chapter?branch_id={branch.branch_id}&number={_chapter.number}&volume={_chapter.volume}",
                                        chapter.number,
                                        chapter.volume.ToString(),
                                        _chapter.teams.First().name,
                                        chapter.name,
                                        chapter.Equals(chapters.First()),
                                        chapter.Equals(chapters.Last()) && branch.Equals(chapter.branches.Last())
                                    )
                            }),
                            new ScraperDTO("", "")
                        );

                        rmq.send("scraper", "getChapterResponse", _title);

                        Thread.Sleep(500);

                    }
                }
            }        
        }

        /// <summary>
        /// TODO Реализовать скачивание изображений
        /// </summary>
        public void getImages()
        {
            CustomDirectory directory = new CustomDirectory(Path.Combine(conf.appConfiguration.path, "media", "titles"));

            MangalibUploader uploader = new MangalibUploader(directory, conf, rmq);

            List<KeyValuePair<string, string>> args = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("eng_name", title.altName),
                new KeyValuePair<string, string>("ru_name",title.name)
            };

            server.execute("v1.0/titles", Method.Get, args);
            var createdTitle = JsonConvert.DeserializeObject<dynamic>(Regex.Replace(server.response.Content, @"(^{""data"":\[)|(\]?,?((""meta"")|(""links"")):{""?[\w0-9\\\/.\:\?\=\,""\[\]\{\}\&\;\s]+""?\}?)", ""));

            foreach (var chapter in title.chapters)
            {
                directory.rootPath = Path.Combine(conf.appConfiguration.path, "media", "titles", title.path);
                directory.createDirectory(chapter.volume);
                directory.createDirectory(chapter.number);
                directory.createDirectory(chapter.translator.altName);
                
                uploader.uploadChapterImages(chapter);
                rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Скачивание изображений {chapter.number}-ой главы завершено"));

                try
                {
                    server.execute($"v1.0/titles/{createdTitle.slug}/persons", new List<IPerson>() { chapter.translator }, Method.Post);
                    server.execute($"v1.0/titles/{createdTitle.slug}/chapters", chapter, Method.Post);
                    server.execute($"v1.0/titles/{createdTitle.slug}/chapters/{chapter.number}/images", chapter, Method.Post);
                }
                catch (Exception ex)
                {
                    logger.LogInformation($"Ошибка при выполении запроса {server.client.BuildUri(server.request).ToString()} - {ex.Message}");
                    rmq.send("information", "errorLog", new LogDTO(ex.Message));
                }

                chapter.images = new List<List<IImage>>();

                ResponseDTO responseDTO = new ResponseDTO(
                   new TitleDTO("",
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

                string queue = string.Empty;

                switch (rmq.rmqMessage.RequestDTO.scraperDTO.action)
                {
                    case "parseTitles":
                        queue = "parseTitleResponse";
                        break;

                    case "getChapters":
                        queue = "getChapterResponse";
                        break;

                    case "parseChapters":
                        queue = "parseChapterResponse";
                        break;
                }

                rmq.send("scraper", queue, responseDTO);

                rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Получение информации о главе {chapter.number} завершено успешно"));
            }
        }                                              

        public void getAllChapters()
        {
            getChapters();
            rmq.send("information", "errorLog", new LogDTO(null, true));
            driver.Quit();
        }

        public void parseChapters()
        {
            getChapters();
            getImages();
            driver.Quit();
        }
    }
}
