using Scraper.Core.Interfaces;
using Scraper.Core.Sources;

namespace Scraper.Core.Handlers
{
    public class MessageHandler<T> where T:IScraper
    {
        private readonly T _scraper;
        private readonly Dictionary<string, Action> _commandMap;

        public MessageHandler(T scraper)
        {
            _scraper = scraper;

            _commandMap = new Dictionary<string, Action>
            {
                { "getChapters", _scraper.getAllChapters },
                { "parseChapters", _scraper.parseChapters },
                { "parseTitles", _scraper.parse }
            };
        }

        public void HandleMessage(string message)
        {
            if (_commandMap.TryGetValue(message, out var command))
            {
                command.Invoke();
            }
            else
            {
                // Обработка неизвестной команды
            }
        }
    }

}
