using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.DTO
{
    public class ResponseDTO : BaseDTO
    {
        public extern ResponseDTO();
        public ResponseDTO(TitleDTO titleDTO, ScraperDTO scraperDTO, string? page = null) : base(titleDTO, scraperDTO, page) { }
    }
}
