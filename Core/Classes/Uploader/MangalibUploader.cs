using RussianTransliteration;
using Scraper.Core.Classes.General;
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
        private string rootPath;
        public SixLabors.ImageSharp.Image image { get; set; }
        public Stream incomingStream { get; set; }
        public MemoryStream outgoingStream { get; set; }
        //private IFTPServer server { get; set; }

        public MangalibUploader(
            //IFTPServer ftpClient,
            string rootPath,
            Configuration conf)
        {
            //server = ftpClient;
            this.conf = conf;
            this.rootPath = rootPath;
            //server.connect();
        }

        public void uploadChapterImages(IChapter chapter)
        {
            var path = "";

            if (!conf.appConfiguration.production)
            {
                CustomDirectory directory = new CustomDirectory(rootPath);

                if (!Directory.Exists(@$"{rootPath}{chapter.number}\{RussianTransliterator.GetTransliteration(chapter.translator.name)}") && String.IsNullOrEmpty(chapter.volume))
                {
                    directory.createDirectory($@"{chapter.number}\{RussianTransliterator.GetTransliteration(chapter.translator.name)}");
                    path += @$"{chapter.number}\{RussianTransliterator.GetTransliteration(chapter.translator.name)}\";
                }
                else
                {
                    directory.createDirectory(@$"{chapter.volume}\{chapter.number}\{RussianTransliterator.GetTransliteration(chapter.translator.name)}");
                    path += @$"{chapter.volume}\{chapter.number}\{RussianTransliterator.GetTransliteration(chapter.translator.name)}\";
                }

                chapter.url = $"{rootPath.Replace(conf.appConfiguration.production ? conf.appConfiguration.prod_root : conf.appConfiguration.local_root, "")}{path}";
            }
            //else
            //{
            //    if (!String.IsNullOrEmpty(chapter.volume))
            //    {
            //        if (!server.client.DirectoryExists($"{server.rootPath}{chapter.volume}"))
            //        {
            //            server.client.CreateDirectory($"{server.rootPath}{chapter.volume}");
            //            path += $"{chapter.volume}/";
            //        }
            //    }
            //    else path += $"{chapter.volume}/";

            //    if (!server.client.DirectoryExists($"{server.rootPath}{chapter.number}") && String.IsNullOrEmpty(chapter.volume))
            //    {
            //        server.client.CreateDirectory($"{server.rootPath}{chapter.number}");
            //        path += $"{chapter.number}/";
            //    }
            //    else
            //    {
            //        server.client.CreateDirectory($"{server.rootPath}/{chapter.volume}/{chapter.number}");
            //        path += $"{chapter.volume}/{chapter.number}/";
            //    }

            //    chapter.url = $"{server.rootPath}{path}";
            //}

            var webp = "";

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

                                using (FileStream fileStream = new FileStream($"{path}{i + 1}.webp", FileMode.Create, FileAccess.Write))
                                {
                                    outgoingStream.Seek(0, SeekOrigin.Begin);
                                    outgoingStream.CopyTo(fileStream);
                                }

                            webp += $"{i + 1},";
                            Console.WriteLine("Изображение успешно загружено на FTP-сервер.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка: {ex.Message}");
                        }
                    }
                }
            }
            chapter.url = chapter.url.Replace(@$"{RussianTransliterator.GetTransliteration(chapter.translator.name)}\", string.Empty);
            chapter.extensions = $"||{webp.Substring(0, webp.LastIndexOf(","))}|";
        }
        
        public void uploadCovers(List<IImage> images)
        {
            
            for (int i = 0; i < images.Count(); i++)
            {
                Console.WriteLine($"\n{rootPath}");
                using (HttpClient httpClient = new HttpClient())
                {
                    try
                    {
                        incomingStream = httpClient.GetStreamAsync($"{images[i].path}.{images[i].extension}").Result;
                        image = SixLabors.ImageSharp.Image.Load(incomingStream);

                        outgoingStream = new MemoryStream();
                        image.Save(outgoingStream, new WebpEncoder());

                        using (FileStream fileStream = new FileStream(!conf.appConfiguration.production ? @$"{rootPath}covers\{i + 1}.webp" : @$"{rootPath}covers/{i + 1}.webp", FileMode.Create, FileAccess.Write))
                        {
                            outgoingStream.Seek(0, SeekOrigin.Begin);
                            outgoingStream.CopyTo(fileStream);
                            images[i].path = @$"{i + 1}";
                            images[i].extension = "webp";
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\nОшибка: {ex.Message}, {ex.InnerException} {rootPath}covers/{i + 1}.webp");
                    }
                }
            }
        }

        public void uploadPersonalImages(List<IPerson> persons) {
            string path = conf.appConfiguration.production ? conf.appConfiguration.prod_root : conf.appConfiguration.local_root;

            CustomDirectory directory = new CustomDirectory(path);
            if (!Directory.Exists(@$"{path}persons"))
                directory.createDirectory("persons");
            
            path += !conf.appConfiguration.production ? @"persons\" : @"persons/";

            foreach (var person in persons)
            {
                if (!Directory.Exists(@$"{path}{person.altName}"))
                {
                    directory.createDirectory(!conf.appConfiguration.production ? @$"{path}\{person.altName}" : @$"{path}/{person.altName}");
                    path = !conf.appConfiguration.production ? @$"{path}{person.altName}\" : @$"{path}{person.altName}/";
                }

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

                            using (FileStream fileStream = new FileStream(!conf.appConfiguration.production ? @$"{path}\{i + 1}.webp" : @$"{path}/{i + 1}.webp", FileMode.Create, FileAccess.Write))
                            {
                                outgoingStream.Seek(0, SeekOrigin.Begin);
                                outgoingStream.CopyTo(fileStream);
                                person.images[i].path = $"{person.altName}/{i + 1}";
                                person.images[i].extension = "webp";
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка: {ex.Message}");
                        }
                    }
                }
                path = path.Replace(!conf.appConfiguration.production ? $@"{person.altName}\" : $@"{person.altName}/", "");
            }
        }
    }
}
