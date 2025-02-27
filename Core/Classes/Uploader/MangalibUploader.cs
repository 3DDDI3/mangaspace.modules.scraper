using Scraper.Core.Classes.General;
using Scraper.Core.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using Configuration = Scraper.Core.Classes.General.Configuration;


namespace Scraper.Core.Classes.Uploader
{
    public class MangalibUploader : IUploader
    {
        private Configuration conf;
        public SixLabors.ImageSharp.Image image { get; set; }
        public Stream incomingStream { get; set; }
        public MemoryStream outgoingStream { get; set; }
        private IFTPServer server { get; set; }

        public MangalibUploader(IFTPServer ftpClient, Configuration conf)
        {
            server = ftpClient;
            this.conf = conf;
            server.connect();
        }
        public void uploadChapterImages(IChapter chapter)
        {
            var path = "";

            if (!conf.appConfiguration.production)
            {
                if (!String.IsNullOrEmpty(chapter.volume))
                {
                    if (!Directory.Exists($"{server.rootPath}{chapter.volume}"))
                    {
                        Directory.CreateDirectory($"{server.rootPath}{chapter.volume}");
                        path += @$"{chapter.volume}\";
                    }
                }
                else path += @$"{chapter.volume}\";

                if (!Directory.Exists($"{server.rootPath}{chapter.number}") && String.IsNullOrEmpty(chapter.volume))
                {
                    Directory.CreateDirectory($"{server.rootPath}{chapter.number}");
                    path += @$"{chapter.number}\";
                }
                else
                {
                    Directory.CreateDirectory(@$"{server.rootPath}\{chapter.volume}\{chapter.number}");
                    path += @$"{chapter.volume}\{chapter.number}\";
                }

                chapter.url = $"{server.rootPath.Replace(@"\\wsl$\Ubuntu\home\laravel\mangaspace\src\storage\app\media\", "")}{path}";
            }
            else
            {
                if (!String.IsNullOrEmpty(chapter.volume))
                {
                    if (!server.client.DirectoryExists($"{server.rootPath}{chapter.volume}"))
                    {
                        server.client.CreateDirectory($"{server.rootPath}{chapter.volume}");
                        path += $"{chapter.volume}/";
                    }
                }
                else path += $"{chapter.volume}/";

                if (!server.client.DirectoryExists($"{server.rootPath}{chapter.number}") && String.IsNullOrEmpty(chapter.volume))
                {
                    server.client.CreateDirectory($"{server.rootPath}{chapter.number}");
                    path += $"{chapter.number}/";
                }
                else
                {
                    server.client.CreateDirectory($"{server.rootPath}/{chapter.volume}/{chapter.number}");
                    path += $"{chapter.volume}/{chapter.number}/";
                }

                chapter.url = $"{server.rootPath}{path}";
            }

            var webp = "";

            for (int i = 0; i < chapter.images.Count; i++)
            {
                for(int j=0; j< chapter.images[i].Count; j++) 
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        try
                        {
                            incomingStream = httpClient.GetStreamAsync($"{chapter.images[i][j].path}.{chapter.images[i][j].extension}").Result;
                            image = SixLabors.ImageSharp.Image.Load(incomingStream);

                            outgoingStream = new MemoryStream();
                            image.Save(outgoingStream, new WebpEncoder());

                            if (conf.appConfiguration.production)
                                server.client.UploadStream(outgoingStream, $"{server.rootPath}{path}{i + 1}.webp");
                            else
                                using (FileStream fileStream = new FileStream($"{server.rootPath}{path}{i + 1}.webp", FileMode.Create, FileAccess.Write))
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
            chapter.extensions = $"||{webp.Substring(0, webp.LastIndexOf(","))}|";
        }
    }
}
