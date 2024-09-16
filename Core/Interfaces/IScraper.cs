using OpenQA.Selenium.Edge;

namespace Scraper.Core.Interfaces
{
    public interface IScraper
    {
        public string baseUrl { get; set; }
        public EdgeDriver driver { get; set; }
        public IPage page { get; set; }
        public ITitle title { get; set; }
        public void getPages();
        public void getChapters();
        public void getImages();
        public void getTitleInfo();
        public void getPersons();
        public void parse();
    }
}
