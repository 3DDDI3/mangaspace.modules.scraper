using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.DTO
{
    public class ScraperDTO
    {
        public string action { get; set; }
        public string engine { get; set; }

        public ScraperDTO(string action, string engine)
        {
            this.action = action;
            this.engine = engine;
        }
    }
}
