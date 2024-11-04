using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Interfaces
{
    public interface IPage
    {
        public string baseUrl { get; set; }
        public string catalogUrl { get; set; }
        public string pageUrl { get; set; }
        public List<int> pages { get; set; }
    }
}
