using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.DTO
{
    public class LogDTO
    {
        public string message { get; set; }
        public bool isLast { get; set; }
        public LogDTO(string? message = null, bool isLast = false)
        {
            this.message = message;
            this.isLast = isLast;
        }
    }
}
