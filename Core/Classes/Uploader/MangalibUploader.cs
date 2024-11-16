using Scraper.Core.Classes.General;
using Scraper.Core.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;


namespace Scraper.Core.Classes.Uploader
{
    public class MangalibUploader : IUploader
    {
        public SixLabors.ImageSharp.Image image { get; set; }
        public Stream incomingStream { get; set; }
        public MemoryStream outgoingStream { get; set; }
        private IFTPServer server { get; set; }

        public MangalibUploader(IFTPServer ftpClient)
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
                    try
                    {
                        incomingStream = httpClient.GetStreamAsync($"{chapter.images[i].path}.{chapter.images[i].extension}").Result;
                        image = SixLabors.ImageSharp.Image.Load(incomingStream);

                        var limiter = Math.Ceiling(Convert.ToDouble(image.Height) / 15000);

                        List<SixLabors.ImageSharp.Image> images = new List<SixLabors.ImageSharp.Image>();
                        if (image.Height > 15000)
                        {
                            for (int l = 0; l < limiter; l++)
                            {
                                if (l == limiter - 1) images.Add(image.Clone(ctx => ctx.Crop(new Rectangle(0, 15000 * l, image.Width, image.Height - 15000 * l))));
                                else images.Add(image.Clone(ctx => ctx.Crop(new Rectangle(0, 15000 * l, image.Width, 15000))));
                            }
                        }

                        for (int j = 0; j < images.Count; j++)
                        {
                            outgoingStream = new MemoryStream();
                            images[j].SaveAsWebp(outgoingStream, new WebpEncoder());
                            server.client.UploadStream(outgoingStream, $"{server.rootPath}{i + 1}_{j+1}.webp");
                        }

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
