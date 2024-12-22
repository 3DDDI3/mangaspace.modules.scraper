using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.DTO
{
    public abstract class BaseDTO
    {
        public string? page { get; set; }
        public TitleDTO titleDTO { get; set; }
        public ScraperDTO scraperDTO { get; set; }

        public BaseDTO(TitleDTO titleDTO, ScraperDTO scraperDTO, string? page = null)
        {
            this.titleDTO = titleDTO;
            this.scraperDTO = scraperDTO;
            this.page = page;
        }
    }
}
