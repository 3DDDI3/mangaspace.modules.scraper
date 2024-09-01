
namespace Scraper.Core.Classes
{
    public class Configuration
    {
        public string? token {  get; set; }
        public ScraperConfiguration? scraperConfiguration { get; set; }
    }

    public class ScraperConfiguration
    {
        public string? baseUrl { get; set; }
        public string? catalogUrl { get; set; }
    }
}
