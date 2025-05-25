using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Json.Remanga
{
    public class RemangaTitle
    {
        public RemangaTitleResult[] results {  get; set; }
    }

    public class RemangaTitleResult
    {
        public string dir { get; set; }
    }
}
