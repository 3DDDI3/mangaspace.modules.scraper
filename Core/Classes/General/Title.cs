using Scraper.Core.Enums;
using Scraper.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Classes.General
{
    public class Title : ITitle
    {
        public string name { get; set; }
        public string altName { get; set; }
        public IImage cover { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public string releaseFormat { get; set; }
        public ushort releaseYear { get; set; }
        public TitleStatus titleStatus { get; set; }
        public TranslateStatus translateStatus { get; set; }
        public List<string> genres { get; set; }
        public string otherNames { get; set; }
        public AgeLimiter ageLimiter { get; set; }
        public List<string> contacts { get; set; }
        public List<IPerson> persons { get; set; }
        public List<IChapter> chapters { get; set; }
        public string country { get; set; }
    }
}
