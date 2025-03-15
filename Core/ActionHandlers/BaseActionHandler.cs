using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scraper.Core.Classes.General;
using Scraper.Core.Classes.RabbitMQ;
using Scraper.Core.Factories;

namespace Scraper.Core.ActionHandlers
{
    public class BaseActionHandler
    {
        private readonly ActionHandlerFactory _actionHandlerFactory;

        public BaseActionHandler(ActionHandlerFactory actionHandlerFactory)
        {
            _actionHandlerFactory = actionHandlerFactory;
        }

        public void ExecuteAction(string actionName, Configuration conf, IConfiguration configuration, RMQ rmq, ILogger logger)
        {
            var handler = _actionHandlerFactory.GetHandler(actionName);

            handler.Handle(conf, configuration, rmq, logger);
        }
    }
}
