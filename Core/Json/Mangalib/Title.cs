using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Json.Mangalib
{
    public class Title
    {
        public string rus_name { get; set; }
        public string eng_name { get; set; }
        public List<string> otherNames { get; set; }
        public Cover cover { get; set; }
        public Type type { get; set; }
        public Restriction ageRestriction { get; set; }
        public string summary { get; set; }
        public string releaseDate { get; set; }
        public Translator[] teams { get; set; }
        public Genre[] genres { get; set; }
        public Translator[] publisher { get; set; }
        public Translator[] authors { get; set; }
        public Translator[] artists { get; set; }
        public Format[] format { get; set; }
        public Status status { get; set; }
        public Status scanlateStatus { get; set; }
    }

    public class Type
    {
        public string label { get; set; }
    }

    public class Restriction
    {
        public string label { get; set; }
    }

    public class Genre
    {
        public string name { get; set; }
    }

    public class Format
    {
        public string name { get; set; }
    }

    public class Status
    {
        public string label { get; set; }
    }
}
