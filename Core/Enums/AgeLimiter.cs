using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Enums
{
    public enum AgeLimiter
    {
        /// <summary>
        /// 0+
        /// </summary>
        all = 1,
        /// <summary>
        /// 16+
        /// </summary>
        minor = 2,
        /// <summary>
        /// 18+
        /// </summary>
        adult = 3
    }
}
