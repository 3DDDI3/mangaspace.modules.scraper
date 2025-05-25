using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.DTO
{
    public class TitleDTO
    {
        public string url { get;set;}
        public string name { get;set;}
        public List<ChapterDTO> chapterDTO { get; set;}
        public TitleDTO(string url, List<ChapterDTO> chapterDTO = null, string? name = null)
        {
            this.url = url;
            this.chapterDTO = chapterDTO;
            this.name = name;
        }
    }
}
