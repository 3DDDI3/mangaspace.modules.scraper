using Scraper.Core.Sources;

namespace Scraper.Core.Handlers
{
    public class MessageHandler
    {
        private readonly Remanga _remanga;
        private readonly Dictionary<string, Action> _commandMap;

        public MessageHandler(Remanga remanga)
        {
            _remanga = remanga;

            _commandMap = new Dictionary<string, Action>
            {
                { "getChapters", _remanga.getAllChapters },
                { "parseChapters", _remanga.parseChapters },
                { "parseTitles", _remanga.parse }
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
