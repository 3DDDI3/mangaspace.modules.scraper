using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.DTO
{
    public class RequestDTO
    {
        public string pages { get; set; }
        public TitleDTO titleDTO { get; set; }
        public ScraperDTO scraperDTO { get; set; }
    }
}
