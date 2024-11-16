using FluentFTP;
using Scraper.Core.Interfaces;
using SixLabors.ImageSharp.Formats.Webp;
using System.Net;

namespace Scraper.Core.Classes.Uploader
{
    public class RemangaUploader : IUploader
    {
        public SixLabors.ImageSharp.Image image { get; set; }
        public Stream incomingStream { get; set; }
        public MemoryStream outgoingStream { get; set; }
        private IFTPServer server { get; set; }

        public RemangaUploader(IFTPServer ftpClient)
        {
            server = ftpClient;
            server.connect();
        }
        public void upload(IChapter chapter)
        {
            if (!server.client.DirectoryExists($"{server.rootPath}{chapter.volume}"))
            {
                server.client.CreateDirectory($"{server.rootPath}{chapter.volume}");
                server.rootPath += $"{chapter.volume}/";
            }
            else server.rootPath += $"{chapter.volume}/";

            if (!server.client.DirectoryExists($"{server.rootPath}{chapter.number}"))
            {
                server.client.CreateDirectory($"{server.rootPath}{chapter.number}");
                server.rootPath += $"{chapter.number}/";
            }
            else server.rootPath += $"{chapter.number}/";


            for (int i = 0; i < chapter.images.Count; i++)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                    httpClient.DefaultRequestHeaders.Add("Referer", "https://remanga.org/"); // Пример добавления токена 
                    try
                    {
                        incomingStream = httpClient.GetStreamAsync($"{chapter.images[i].path}.{chapter.images[i].extension}").Result;
                        image = SixLabors.ImageSharp.Image.Load(incomingStream);

                        outgoingStream = new MemoryStream();
                        image.Save(outgoingStream, new WebpEncoder());
                        server.client.UploadStream(outgoingStream, $"{server.rootPath}{i + 1}.webp");

                        Console.WriteLine("Изображение успешно загружено на FTP-сервер.");
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
