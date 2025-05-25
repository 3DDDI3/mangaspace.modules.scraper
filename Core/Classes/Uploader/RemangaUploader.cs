using FluentFTP;
using RussianTransliteration;
using Scraper.Core.Classes.General;
using Scraper.Core.Classes.RabbitMQ;
using Scraper.Core.DTO;
using Scraper.Core.Interfaces;
using Scraper.Core.Json.Mangalib;
using SixLabors.ImageSharp.Formats.Webp;
using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace Scraper.Core.Classes.Uploader
{
    public class RemangaUploader : IUploader
    {
        private Configuration conf;
        private RMQ rmq;
        public SixLabors.ImageSharp.Image image { get; set; }
        public Stream incomingStream { get; set; }
        public MemoryStream outgoingStream { get; set; }
        private CustomDirectory directory { get; set; }

        public RemangaUploader(CustomDirectory directory, Configuration conf, RMQ rmq)
        {
            this.directory = directory;
            this.conf = conf;
            this.rmq = rmq;
        }

        /// <summary>
        /// Скачивание изображений в главах
        /// </summary>
        /// <param name="chapter"></param>
        public void uploadChapterImages(IChapter chapter)
        {
            string extensions = "";

            for (int i = 0; i < chapter.images.Count; i++)
            {
                for (int j = 0; j < chapter.images[i].Count(); j++)
                {
                    directory.createDirectory(chapter.volume);
                    directory.createDirectory(chapter.number);
                    directory.createDirectory(RussianTransliterator.GetTransliteration(Regex.Replace(chapter.translator.name, @"[\/\\\*\&\]\[\|\.]+", "")));

                    using (HttpClient httpClient = new HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                        httpClient.DefaultRequestHeaders.Add("Referer", "https://remanga.org/"); // Пример добавления токена 
                        try
                        {
                            incomingStream = httpClient.GetStreamAsync($"{chapter.images[i][j].path}.{chapter.images[i][j].extension}").Result;
                            image = SixLabors.ImageSharp.Image.Load(incomingStream);

                            outgoingStream = new MemoryStream();
                            image.Save(outgoingStream, new WebpEncoder());

                            directory.createFile(Path.Combine($"{i + 1}_{j + 1}.webp"), outgoingStream);

                            Console.WriteLine($"Изображение {i+1}_{j+1}.webp {chapter.number} главы успешно скачано.");

                            rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Изображение {i + 1}_{j + 1}.webp {chapter.number} главы успешно скачано."));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка: {ex.Message}");
                            rmq.send("information", "errorLog", new LogDTO(ex.Message));
                        }

                        extensions += $"{i + 1}_{j + 1},";
                    }
                    directory.rootPath = directory.rootPath.Replace(Path.Combine(chapter.volume, chapter.number, RussianTransliterator.GetTransliteration(Regex.Replace(chapter.translator.name, @"[\/\\\*\&\]\[\|\.]+", ""))), "");
                }
            }
            chapter.url = $"{chapter.volume}/{chapter.number}";
            chapter.extensions = $"||{Regex.Replace(extensions, @"\,$", "")}|";

            rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Изображения для {chapter.number} главы успешно скачаны."));
        }

        /// <summary>
        /// Скачивание обложки тайтла
        /// </summary>
        /// <param name="images"></param>
        public void uploadCovers(List<IImage> images)
        {
            for (int i = 0; i < images.Count(); i++)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                    httpClient.DefaultRequestHeaders.Add("Referer", "https://remanga.org/"); // Пример добавления токена 
                    try
                    {
                        incomingStream = httpClient.GetStreamAsync($"{images[i].path}.{images[i].extension}").Result;
                        image = SixLabors.ImageSharp.Image.Load(incomingStream);

                        outgoingStream = new MemoryStream();
                        image.Save(outgoingStream, new WebpEncoder());

                        directory.createFile($"{i + 1}.webp", outgoingStream);

                        images[i].path = $"{i + 1}";
                        Console.WriteLine($"Обложка {i + 1}.webp успешно скачана.");
                        rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Обложка {i + 1}.webp успешно скачана."));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка: {ex.Message}");
                    }
                }
            }
        }

        public void uploadPersonalImages(List<IPerson> persons)
        {
            foreach (var person in persons)
            {
                directory.createDirectory(person.altName);

                for (int i = 0; i < person.images.Count(); i++)
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                        httpClient.DefaultRequestHeaders.Add("Referer", "https://remanga.org/"); // Пример добавления токена 
                        try
                        {
                            incomingStream = httpClient.GetStreamAsync($"{person.images[i].path}.{person.images[i].extension}").Result;
                            image = SixLabors.ImageSharp.Image.Load(incomingStream);

                            outgoingStream = new MemoryStream();
                            image.Save(outgoingStream, new WebpEncoder());

                            directory.createFile($"{i + 1}.webp", outgoingStream);

                            person.images[i].path = $"{i + 1}";
                            person.images[i].extension = "webp";

                            Console.WriteLine($"Изображение {i + 1}.webp персоны {person.name} скачана.");
                            rmq.send("information", "informationLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Изображение {i + 1}.webp персоны {person.name} скачана."));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка: {ex.Message}");
                        }
                    }
                }
            }
        }
    }
}
