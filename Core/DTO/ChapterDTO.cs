using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.DTO
{
    public class ChapterDTO
    {
        public string url { get; set; }
        public string? name { get; set; }
        public string number { get; set; }
        public string translator { get; set; }
        public string volume { get; set; }

        /// <summary>
        /// true, если нужно отобравзить информацию о тайтле
        /// </summary>
        public bool isFirst { get; set; }

        /// <summary>
        /// true, если возвращается последняя глава
        /// </summary>
        public bool isLast { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="number"></param>
        /// <param name="name">true, если нужно отобравзить информацию о тайтле</param>
        /// <param name="isLast">true, если возвращается последняя глава</param>
        public ChapterDTO(string url, string number, string volume, string? translator = null, string? name = null, bool isFirst = false, bool isLast = false)
        {
            this.name = name;
            this.url = url;
            this.number = number;
            this.volume = volume;
            this.translator = translator;
            this.isFirst = isFirst;
            this.isLast = isLast;
        }
    }
}
