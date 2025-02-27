using Scraper.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Scraper.Core.Classes.General
{
    public class Image : IImage
    {
        public string path { get; set; }
        public string extension { get; set; }
        public Image(string? url)
        {
            if (string.IsNullOrEmpty(url)) return;
            Regex regex = new Regex(@"^(https?://[^*]+)\.([a-z]{3,4})$");
            var matches = regex.Matches(url);
            path = matches[0].Groups[1].Value;
            extension = matches[0].Groups[2].Value;
        }
    }
}
