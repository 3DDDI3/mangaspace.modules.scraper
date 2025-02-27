using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scraper.Core.Classes.General;
using Scraper.Core.Classes.RabbitMQ;
using Scraper.Core.Handlers;
using Scraper.Core.Interfaces;
using Scraper.Core.Sources;

namespace Scraper.Core.ActionHandlers
{
    public class MangalibHandler : IActionHandler
    {
        public void Handle(Configuration conf, RMQ rmq, ILogger logger)
        {
            var manglib = new Mangalib(conf, rmq, logger);
            var messageHandler = new MessageHandler<Mangalib>(manglib);
            messageHandler.HandleMessage(rmq.rmqMessage.RequestDTO.scraperDTO.action);
        }
    }
}
