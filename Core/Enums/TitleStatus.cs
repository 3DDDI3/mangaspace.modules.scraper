using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Enums
{
    public enum TitleStatus
    {
        /// <summary>
        /// Онгоинг
        /// </summary>
        continues = 1,
        /// <summary>
        /// Анонс
        /// </summary>
        announcement = 2,
        /// <summary>
        /// Завершен
        /// </summary>
        finished = 3,
        /// <summary>
        /// Приостановлен
        /// </summary>
        suspended = 4,
        /// <summary>
        /// Прекращен
        /// </summary>
        terminated = 5,
        /// <summary>
        /// Лицензировано
        /// </summary>
        licensed=6,
        /// <summary>
        /// Нет переводчика
        /// </summary>
        noTranslator=7,
    }
}
