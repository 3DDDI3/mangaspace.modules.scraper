using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scraper.Core.Classes.General;
using Scraper.Core.Classes.RabbitMQ;

namespace Scraper.Core.Interfaces
{
    public interface IActionHandler
    {
        void Handle(Configuration conf, IConfiguration configuration, RMQ rmq, ILogger logger) { }
    }
}
