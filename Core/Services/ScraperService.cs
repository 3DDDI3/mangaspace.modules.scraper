using Scraper.Core.DTO;
using Scraper.Core.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Services
{
    public class ScraperService
    {
        private readonly ActionHandlerFactory _handlerFactory;

        public ScraperService(ActionHandlerFactory handlerFactory)
        {
            _handlerFactory = handlerFactory;
        }

        public void Process(BaseDTO DTO)
        {
            var handler = _handlerFactory.GetHandler(DTO.scraperDTO.action);
            handler.Handle();
        }
    }
}
