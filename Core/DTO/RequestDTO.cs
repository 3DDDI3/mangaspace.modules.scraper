﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.DTO
{
    public class RequestDTO : BaseDTO
    {
        public RequestDTO(TitleDTO titleDTO, ScraperDTO scraperDTO, List<int> pages = null) : base(titleDTO, scraperDTO, pages) { }
    }
}
