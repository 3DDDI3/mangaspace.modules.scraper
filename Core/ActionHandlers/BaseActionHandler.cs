using Scraper.Core.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.ActionHandlers
{
    public class BaseActionHandler
    {
        private readonly ActionHandlerFactory _actionHandlerFactory;

        public BaseActionHandler(ActionHandlerFactory actionHandlerFactory)
        {
            _actionHandlerFactory = actionHandlerFactory;
        }

        public void ExecuteAction(string actionName)
        {
            var handler = _actionHandlerFactory.GetHandler(actionName);

            handler.Handle();
        }
    }
}
