using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Interfaces
{
    public interface IPage
    {
        public string catalogUrl { get; set; }
        public List<string> pages { get; set; }
        public IPageRange pageRange { get; set; }
        public void getPages();
    }
}
