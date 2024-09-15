using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Interfaces
{
    public interface IChapter
    {
        public string name { get; set; }
        public string volume { get; set; }
        public string number { get; set; }
        public string url { get; set; }
        public IPerson translator { get; set; }
        public List<IImage> images { get; set; }
    }
}
