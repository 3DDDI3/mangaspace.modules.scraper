using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scraper.Core.Classes.General;
using Scraper.Core.Classes.RabbitMQ;
using Scraper.Core.Interfaces;

namespace Scraper.Core.ActionHandlers
{
    public class MangalibHandler : IActionHandler
    {
        public void Handle(Configuration conf, RMQ rmq, ILogger logger)
        {
        }
    }
}
