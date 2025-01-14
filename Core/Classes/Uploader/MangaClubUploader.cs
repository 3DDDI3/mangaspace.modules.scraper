using Scraper.Core.Classes.General;
using Scraper.Core.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;

namespace Scraper.Core.Classes.Uploader
{
    public class MangaClubUploader : IUploader
    {
        public SixLabors.ImageSharp.Image image { get; set; }
        public Stream incomingStream { get; set; }
        public MemoryStream outgoingStream { get; set; }

        private IFTPServer server { get; set; }

        public MangaClubUploader(IFTPServer ftpClient)
        {
            server = ftpClient;
            server.connect();
        }

        public void uploadChapterImages(IChapter chapter)
        {
            server.rootPath = $"{server.rootPath}{chapter.volume}/";

            if (!server.client.DirectoryExists(server.rootPath))
                server.client.CreateDirectory(server.rootPath);

            server.rootPath = $"{server.rootPath}{chapter.number}/";
            if (!server.client.DirectoryExists(server.rootPath))
                server.client.CreateDirectory(server.rootPath);

            for (int i = 0; i < chapter.images.Count; i++)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    try
                    {
                        //incomingStream = httpClient.GetStreamAsync($"{chapter.images[i].path}.{chapter.images[i].extension}").Result;
                        //image = SixLabors.ImageSharp.Image.Load(incomingStream);
                        //outgoingStream = new MemoryStream();
                        //image.SaveAsWebp(outgoingStream, new WebpEncoder() { FileFormat = WebpFileFormatType.Lossless });
                        //server.client.UploadStream(outgoingStream, $"{server.rootPath}{i + 1}.webp");
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
