using Scraper.Core.Enums;
using Scraper.Core.Interfaces;

namespace Scraper.Core.Classes
{
    public class Person : IPerson
    {
        public string name { get; set; }
        public PersonType type { get; set; }
        public string description { get; set; }
        public IImage image { get; set; }
        public Person()
        {
            image = new Image();
        }
    }
}
