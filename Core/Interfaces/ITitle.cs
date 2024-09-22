using Scraper.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Interfaces
{
    public interface ITitle
    {
        /// <summary>
        /// Русское название
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// Английское название
        /// </summary>
        public string altName { get; set; }
        public IImage cover { get; set; }
        public string otherNames { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public string releaseFormat { get; set; }
        public ushort releaseYear { get; set; }
        public AgeLimiter ageLimiter { get; set; }
        public TitleStatus titleStatus { get; set; }
        public TranslateStatus translateStatus { get; set; }
        public List<IChapter> chapters { get; set; }
        public List<string> contacts { get; set; }
        public List<string> genres { get; set; }
        public List<IPerson> persons { get; set; }
        public string country { get; set; }
    }
}
