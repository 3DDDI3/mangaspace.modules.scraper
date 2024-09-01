using Scraper.Core.Enums;
using Scraper.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Classes
{
    public class Title : ITitle
    {
        public string name { get; set; }
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
        public Title()
        {
            authors = new List<IPerson>();
            publishers = new List<IPerson>();
            painters = new List<IPerson>();
            translators = new List<IPerson>();
        }
    }
}
