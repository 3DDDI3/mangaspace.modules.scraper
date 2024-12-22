using Newtonsoft.Json.Bson;
using Scraper.Core.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Interfaces
{
    public interface IActionHandler
    {
        void Handle();
    }
}
