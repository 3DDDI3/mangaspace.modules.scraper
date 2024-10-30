using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Scraper.Core.Classes.MessageReader
{
    public class MessageReader
    {
        private string message;
        public MessageReader(string message) => this.message = message;

        public List<int> convertMessageToArray()
        {
            string[] arr = message.Split(",");
            List<int> pages = new List<int>();
            Regex regex = new Regex(@"(\d+)\.{2}(\d+)");
            foreach (var item in arr)
            {
                var matches = regex.Match(item);

                if (!matches.Success)
                {
                    pages.Add(int.Parse(item));
                    continue;
                }

                for (int i = int.Parse(matches.Groups[1].Value); i <= int.Parse(matches.Groups[2].Value); i++)
                {
                    if (!pages.Contains(i)) pages.Add(i);
                }
            }
            return pages;
        }
    }
}
