using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Json.Remanga
{
    public class Title
    {
        public Branch[] branches { get; set; }
        public string secondary_name { get; set; }
        public string main_name { get; set; }
        public string another_name { get; set; }
        public Type type { get; set; }
        public AgeLimit age_limit { get; set; }
        public Status status { get; set; }
        public _TranslateStatus translate_status { get; set; }
        public string description { get; set; }
        public string issue_year {  get; set; }
        public _Cover cover { get; set; }
        public Genres[] genres { get; set; }
    }

    public class Type
    {
        public string name { get; set; }
    }

    public class AgeLimit
    {
        public string name { get; set; }
    }

    public class Status
    {
        public string name { get; set; }
    }

    public class _TranslateStatus
    {
        public string name { get; set; }
    }
    public class _Cover
    {
        public string low {  get; set; }
        public string mid { get; set; }
        public string high { get; set; }
    }

    public class Genres
    {
        public string name { get; set; }
    }
}
