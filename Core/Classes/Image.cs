using Scraper.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Classes
{
    public class Image : IImage
    {
        public string path { get; set; }
        public string extension { get; set; }
    }
}
