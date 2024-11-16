using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Classes.General
{
    public class Server
    {
        public RestClient client { get; set; }
        public RestRequest request { get; set; }
        public RestResponse response { get; set; }

        private Configuration conf;
        public Server(Configuration conf) {
            this.conf = conf;
            client = new RestClient(new Uri(conf.apiConfiguration.baseUrl));
        }

        public void init(string resource)
        {
            request = new RestRequest(resource);
            request.AddHeaders(new Dictionary<string, string>()
            {
                ["Accept"] = "application/json",
                ["Authorization"] = $"Bearer {conf.apiConfiguration.token}",
            });
        }
    }
}
