using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Json.Remanga
{
    public class Translator
    {
       public TranslatorContent content { get; set; }
    }

    public class TranslatorContent
    {
        public string name { get; set; }
        public Img img { get; set; }
        public string description { get; set; }
        public Links links { get; set; }
    }

    public class Links
    {
        public string vk { get; set; }
        public string fb { get; set; }
        public string youtube { get; set; }
        public string twitter { get; set; }
        public string insta { get; set; }
        public string discord { get; set; }
    }

    public class Img
    {
        public string mid { get; set; }
        public string high { get; set; }
    }
}
