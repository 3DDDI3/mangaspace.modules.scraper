using Scraper.Core.Enums;

namespace Scraper.Core.Interfaces
{
    public interface IPerson
    {
        public string name { get; set; }
        public string? altName { get; set; }
        public string url { get; set; }
        public PersonType type { get; set; }
        public string? description { get; set; }
        public List<IImage>? images { get; set; }
        public List<string> contacts { get; set; }
    }
}
