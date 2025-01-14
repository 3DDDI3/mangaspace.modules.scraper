using Scraper.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Classes.General
{
    public class Chapter : IChapter
    {
        public string name { get; set; }
        public string volume { get; set; }
        public string number { get; set; }
        public string url { get; set; }
        public IPerson translator { get; set; }
        public string extensions { get; set; }
        public List<List<IImage>> images { get; set; }
        public Chapter()
        {
            translator = new Person();
            images = new List<List<IImage>>();
        }
    }
}
