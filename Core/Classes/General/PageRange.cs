using Scraper.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Classes.General
{
    public class PageRange : IPageRange
    {
        public int from { get; set; }
        public int to { get; set; }
    }
}
