using OpenQA.Selenium.Edge;
using Scraper.Core.Classes.General;

namespace Scraper.Core.Interfaces
{
    public interface IScraper
    {
        public string baseUrl { get; set; }
        public EdgeDriver driver { get; set; }
        public IPage page { get; set; }
        public ITitle title { get; set; }
        public Server server { get; set; }
        public void getChapters();
        public void getImages();
        public void getTitleInfo();
        public void getAllChapters();
        public void parseChapters();
        public void getPersons();
        public void parse();
    }
}
