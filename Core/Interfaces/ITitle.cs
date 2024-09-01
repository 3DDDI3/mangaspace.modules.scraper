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
        public string description { get; set; }
        public string type { get; set; }
        public string releaseFormat { get; set; }
        public ushort releaseYear { get; set; }
        public TitleStatus titleStatus { get; set; }
        public TranslateStatus translateStatus { get; set; }
        public List<IPerson> authors { get; set; }
        public List<IPerson> publishers { get; set; }
        public List<IPerson> painters { get; set; }
        public List<IPerson> translators { get; set; }
    }
}
