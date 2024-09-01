using Scraper.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Classes
{
    public class Chapter : IChapter
    {
        public string name {  get; set; }
        public string volume { get; set; }
        public string number { get; set; }
        public IPerson person { get; set; }
        public Chapter() => person = new Person();
    }
}
