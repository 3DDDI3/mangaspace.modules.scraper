using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Enums
{
    public enum TranslateStatus
    {
        /// <summary>
        /// Продолжается
        /// </summary>
        continues = 1,
        /// <summary>
        /// Завершен
        /// </summary>
        finished = 2,
        /// <summary>
        /// Заморожен
        /// </summary>
        frezed = 3,
        /// <summary>
        /// Прекращен
        /// </summary>
        terminated = 4,
        /// <summary>
        /// Лицензировано
        /// </summary>
        licensed=5,
        /// <summary>
        /// Нет переводчика
        /// </summary>
        noTranslator=6,
    }
}
