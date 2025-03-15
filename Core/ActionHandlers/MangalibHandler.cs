using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scraper.Core.Classes.General;
using Scraper.Core.Classes.RabbitMQ;
using Scraper.Core.Handlers;
using Scraper.Core.Interfaces;
using Scraper.Core.Sources;
using SixLabors.ImageSharp;
using Configuration = Scraper.Core.Classes.General.Configuration;

namespace Scraper.Core.ActionHandlers
{
    public class MangalibHandler : IActionHandler
    {
        public void Handle(Configuration conf, IConfiguration configuration, RMQ rmq, ILogger logger)
        {
            conf.scraperConfiguration = configuration.GetSection("mangalib").Get<ScraperConfiguration>();
            var manglib = new Mangalib(conf, rmq, logger);
            var messageHandler = new MessageHandler<Mangalib>(manglib);
            messageHandler.HandleMessage(rmq.rmqMessage.RequestDTO.scraperDTO.action);
        }
    }
}
