using RussianTransliteration;
using Scraper.Core.Classes.General;
using Scraper.Core.Classes.RabbitMQ;
using Scraper.Core.DTO;
using Scraper.Core.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Configuration = Scraper.Core.Classes.General.Configuration;


namespace Scraper.Core.Classes.Uploader
{
    public class MangalibUploader : IUploader
    {
        private Configuration conf; 
        private CustomDirectory directory;
        private RMQ rmq;
        public SixLabors.ImageSharp.Image image { get; set; }
        public Stream incomingStream { get; set; }
        public MemoryStream outgoingStream { get; set; }

        public MangalibUploader(CustomDirectory directory, Configuration conf, RMQ rmq)
        {
            this.directory = directory;
            this.conf = conf;
            this.rmq = rmq;
        }

        public void uploadChapterImages(IChapter chapter)
        {
            string webp = "";

            for (int i = 0; i < chapter.images.Count; i++)
            {
                for (int j = 0; j < chapter.images[i].Count; j++)
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                        httpClient.DefaultRequestHeaders.Add("Referer", "https://mangalib.me"); // Пример добавления токена 
                        try
                        {
                            incomingStream = httpClient.GetStreamAsync($"{chapter.images[i][j].path}.{chapter.images[i][j].extension}").Result;
                            image = SixLabors.ImageSharp.Image.Load(incomingStream);

                            outgoingStream = new MemoryStream();
                            image.Save(outgoingStream, new WebpEncoder());

                            directory.createFile($"{i + 1}.webp", outgoingStream);

                            webp += $"{i + 1},";

                            Console.WriteLine($"Изображение {i + 1}.webp успешно загружено на сервер.");
                            rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Изображение {i + 1}.webp успешно скачано."));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка: {ex.Message}");
                        }
                    }
                }
            }
            chapter.url = $"{chapter.volume}/{chapter.number}";
            chapter.extensions = $"||{webp.Substring(0, webp.LastIndexOf(","))}|";
        }

        public void uploadCovers(List<IImage> images)
        {

            for (int i = 0; i < images.Count(); i++)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    try
                    {
                        incomingStream = httpClient.GetStreamAsync($"{images[i].path}.{images[i].extension}").Result;
                        image = SixLabors.ImageSharp.Image.Load(incomingStream);

                        outgoingStream = new MemoryStream();
                        image.Save(outgoingStream, new WebpEncoder());

                        directory.createFile($"{i + 1}.webp", outgoingStream);

                        Console.WriteLine($"Обложка {i + 1}.webp успешно скачана.");
                        rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Обложка {i + 1}.webp успешно скачана."));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка: {ex.Message}");
                    }
                }
                images[i].path = $"{i + 1}";
                images[i].extension = "webp";
            }
        }

        public void uploadPersonalImages(List<IPerson> persons) {
            foreach (var person in persons)
            {
                directory.createDirectory(person.altName);
               
                for (int i = 0; i < person.images.Count(); i++)
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(person.images[i].path) || string.IsNullOrEmpty(person.images[i].extension))
                                continue;

                            incomingStream = httpClient.GetStreamAsync($"{person.images[i].path}.{person.images[i].extension}").Result;
                            image = SixLabors.ImageSharp.Image.Load(incomingStream);

                            outgoingStream = new MemoryStream();
                            image.Save(outgoingStream, new WebpEncoder());

                            directory.createFile($"{i + 1}.webp", outgoingStream);

                            person.images[i].path = $"{i + 1}";
                            person.images[i].extension = "webp";
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка: {ex.Message}");
                        }
                    }
                }
                directory.rootPath = Path.Combine(conf.appConfiguration.path, "media", "persons");
            }
        }
    }
}
