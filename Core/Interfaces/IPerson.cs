using Scraper.Core.Enums;

namespace Scraper.Core.Interfaces
{
    public interface IPerson
    {
        public string name { get; set; }
        public PersonType type { get; set; }
        public string description { get; set; }
        public IImage image { get; set; }
    }
}
