using Microsoft.Extensions.DependencyInjection;
using Scraper.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Factories
{
    public class ActionHandlerFactory
    {
        private readonly Dictionary<string, IActionHandler> _handlers;

        public ActionHandlerFactory(IEnumerable<IActionHandler> handlers)
        {
            _handlers = handlers.ToDictionary(handler => handler.GetType().Name.Replace("Handler", "").ToLower(), handler => handler);
        }

        public IActionHandler GetHandler(string action)
        {
            if (_handlers.TryGetValue(action, out var handler))
            {
                return handler;
            }

            throw new NotSupportedException("Действие не поддерживается");
        }

    }
}
