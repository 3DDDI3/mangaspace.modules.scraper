using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Interfaces
{
    public interface IUploader
    {
        public Image image { get; set; }
        public Stream incomingStream { get; set; }
        public MemoryStream outgoingStream { get; set; }
        public void uploadChapterImages(IChapter chapter);
    }
}
