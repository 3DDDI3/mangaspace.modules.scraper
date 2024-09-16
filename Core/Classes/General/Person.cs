using Scraper.Core.Enums;
using Scraper.Core.Interfaces;

namespace Scraper.Core.Classes.General
{
    public class Person : IPerson
    {
        public string name { get; set; }
        public PersonType type { get; set; }
        public string description { get; set; }
        public IImage image { get; set; }
        public string altName { get; set; }
        public string url { get; set; }
    }
}
