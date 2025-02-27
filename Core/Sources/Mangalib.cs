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
using Microsoft.Extensions.Logging;
using Scraper.Core.Classes.RabbitMQ;
using Scraper.Core.DTO;
using RestSharp;
using RussianTransliteration;
using Scraper.Core.Json.Mangaovh;
using System.Xml.Linq;
using Scraper.Core.Json.Remanga;

namespace Scraper.Core.Sources
{
    public class Mangalib : IScraper
    {
        private IFTPServer ftpServer;
        private RMQ rmq;
        private Configuration conf;
        private ILogger logger;
        private Server externalServer;
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
            externalServer = new Server("https://api2.mangalib.me/api/manga");

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
            var url = rmq.rmqMessage.RequestDTO.titleDTO.chapterDTO[0].url.Split("/")[0];

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
            _title = JsonConvert.DeserializeObject<MangalibJson.Title>(Regex.Replace(externalServer.response.Content, @"(^{""data"":)|(,""meta"":{""\w+"":""\w+""}}$)", ""));

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

            getPersons();

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

            rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Получение информации о главе {title.name} завершено успешно"));

            externalServer.externalExecute($"{url}/covers", Method.Get, args);

            MangalibJson.Covers[] covers = JsonConvert.DeserializeObject<MangalibJson.Covers[]>(Regex.Replace(externalServer.response.Content, @"(^{""data"":)|(}$)", ""));

            title.cover = new List<IImage>();

            foreach (var cover in covers)
            {
                title.cover.Add(new Image(cover.cover.md));
            }

            ftpServer.rootPath = @$"\\wsl$\Ubuntu\home\laravel\mangaspace\src\storage\app\media\";
            if (!Directory.Exists($"{ftpServer.rootPath}{RussianTransliterator.GetTransliteration(Regex.Replace(title.name, @"[\/\\\*\&\]\[\|]+", ""))}"))
                Directory.CreateDirectory($"{ftpServer.rootPath}{RussianTransliterator.GetTransliteration(Regex.Replace(title.name, @"[\/\\\*\&\]\[\|]+", ""))}");

            ftpServer.rootPath += @$"{RussianTransliterator.GetTransliteration(Regex.Replace(title.name, @"[\/\\\*\&\]\[\|]+", ""))}\";

            if (!Directory.Exists(@$"{ftpServer.rootPath}\covers"))
                Directory.CreateDirectory(@$"{ftpServer.rootPath}\covers");
            //ftpServer.rootPath = $"{ftpServer.rootPath}{title.altName}/";
            ftpServer.connect();
        }

        public void getPersons()
        {
            foreach (var person in _title.authors)
            {
                title.persons.Add(new Person()
                {
                    name = person.name,
                    type = PersonType.author,
                    image = new Image(person.cover.md)
                });
            }

            foreach (var person in _title.publisher)
            {
                title.persons.Add(new Person()
                {
                    name = person.name,
                    type = PersonType.publishing,
                    image = new Image(person.cover.md)
                });
            }


            foreach (var person in _title.artists)
            {
                title.persons.Add(new Person()
                {
                    name = person.name,
                    type = PersonType.painter,
                    image = new Image(person.cover.md)
                });
            }
        }

        public void getChapters()
        {
            var url = driver.Url.Split("/")[driver.Url.Split("/").Length - 1];

            if (rmq.rmqMessage.RequestDTO.scraperDTO.action == "parseChapters")
            {
                getTitleInfo();

                server.execute("v1.0/titles", title, Method.Post);

                List<List<IImage>> images = new List<List<IImage>>();
                foreach (var chapter in rmq.rmqMessage.RequestDTO.titleDTO.chapterDTO)
                {
                    externalServer.externalExecute(chapter.url, Method.Get);
                    MangalibJson.Chapter _chapter = JsonConvert.DeserializeObject<MangalibJson.Chapter>(Regex.Replace(externalServer.response.Content, @"(""data"":)|(^{)|(}$)", ""));

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
                        translator = new Person() { name = _chapter.teams.First().name, image = new Image(_chapter.teams.First().cover.md), type = PersonType.translator }
                    });
                }
            }
            else
            {
                externalServer.externalExecute($"/{url}/chapters", RestSharp.Method.Get);

                MangalibJson.Chapters[] chapters = JsonConvert.DeserializeObject<MangalibJson.Chapters[]>(Regex.Replace(externalServer.response.Content, @"(""data"":)|(^{)|(}$)", ""));
                chapters = chapters.Take(5).ToArray();

                List<List<IImage>> images = new List<List<IImage>>();

                foreach (var chapter in chapters)
                {
                    foreach (var branch in chapter.branches)
                    {
                        externalServer.externalExecute($"/{url}/chapter", Method.Get,
                            new List<KeyValuePair<string, string>>() {
                            new KeyValuePair<string, string>("branch_id", branch.branch_id.ToString()),
                            new KeyValuePair<string, string>( "volume", chapter.volume.ToString() ),
                            new KeyValuePair<string, string>( "number", chapter.number)
                            });
                        MangalibJson.Chapter _chapter = null;

                        _chapter = JsonConvert.DeserializeObject<MangalibJson.Chapter>(Regex.Replace(externalServer.response.Content, @"(""data"":)|(^{)|(}$)", ""));

                        List<IImage> _images = new List<IImage>();
                        foreach (var image in _chapter.pages)
                        {
                            _images.Add(new Image($"https://img33.imgslib.link{image.url}"));
                        }
                        images.Add(_images);

                        title.chapters.Add(new Chapter()
                        {
                            images = images,
                            name = chapter.name,
                            number = chapter.number,
                            volume = chapter.volume.ToString(),
                            translator = new Person() { name = _chapter.teams.First().name, image = new Image(_chapter.teams.First().cover.md), type = PersonType.translator }
                        });

                        var _title = new ResponseDTO(
                            new TitleDTO("", new List<ChapterDTO>() { new ChapterDTO(
                                $"{url}/chapter?branch_id={branch.branch_id}&volume={chapter.volume}&number={chapter.number}",
                                chapter.number,
                                _chapter.teams.First().name,
                                chapter.name,
                                chapter.Equals(chapters.First()) && branch.Equals(chapter.branches.First())?true:false,
                                //title.chapters.Count==10?true:false
                                chapter.Equals(chapters.Last()) && branch.Equals(chapter.branches.Last())?true:false
                                )
                               }),
                            new ScraperDTO("", "")
                        );

                        rmq.send("scraper", "getChapterResponse", _title);
                        Thread.Sleep(500);

                    }

                    //if (title.chapters.Count > 10)
                    //    break;

                }
            }
                      
        }

           

        /// <summary>
        /// TODO Реализовать скачивание изображений
        /// </summary>
        public void getImages()
        {
            ftpServer.rootPath = @$"\\wsl$\Ubuntu\home\laravel\mangaspace\src\storage\app\media\";
            if (!Directory.Exists($"{ftpServer.rootPath}{RussianTransliterator.GetTransliteration(Regex.Replace(title.name, @"[\/\\\*\&\]\[\|]+", ""))}"))
                Directory.CreateDirectory($"{ftpServer.rootPath}{RussianTransliterator.GetTransliteration(Regex.Replace(title.name, @"[\/\\\*\&\]\[\|]+", ""))}");

            ftpServer.rootPath += @$"{RussianTransliterator.GetTransliteration(Regex.Replace(title.name, @"[\/\\\*\&\]\[\|]+", ""))}\";

            if (!Directory.Exists(@$"{ftpServer.rootPath}\covers"))
                Directory.CreateDirectory(@$"{ftpServer.rootPath}\covers");

            MangalibUploader uploader = new MangalibUploader(ftpServer, conf);

            foreach (var chapter in title.chapters)
            {
                rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Получение информации о главе {chapter.number} начато"));
                uploader.uploadChapterImages(chapter);

                List<KeyValuePair<string, string>> args = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("eng_name", title.altName),
                    new KeyValuePair<string, string>("ru_name",title.name)
                };

                server.execute("v1.0/titles", Method.Get, args);
                var createdTitle = JsonConvert.DeserializeObject<Classes.General.Title>(server.response.Content);

                server.execute($"v1.0/titles/{createdTitle.slug}/chapters", chapter, Method.Post);
                server.execute($"v1.0/titles/{createdTitle.slug}/chapters/{chapter.number}/images", chapter, Method.Post);

                chapter.images = new List<List<IImage>>();

                /*
                 * @TODO подкорректировать отправку сообщения в RMQ
                 */
                ResponseDTO responseDTO = new ResponseDTO(
                   new TitleDTO("",
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
                rmq.send("scraper", "parseChapterResponse", responseDTO);

                rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Получение информации о главе {chapter.number} завершено успешно"));
            }
        }

        public void getAllChapters()
        {
            driver.Navigate().GoToUrl(rmq.rmqMessage.RequestDTO.titleDTO.url);
            getChapters();
            rmq.send("information", "errorLog", new LogDTO(null, true));
            driver.Quit();
        }

        public void parseChapters()
        {
            getChapters();
            getImages();
        }
    }
}
