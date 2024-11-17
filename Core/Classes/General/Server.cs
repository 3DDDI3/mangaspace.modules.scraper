using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Scraper.Core.Classes.RabbitMQ;

namespace Scraper.Core.Classes.General
{
    public class Server
    {
        private Configuration conf;
        private ILogger logger;
        private RMQ rmq;
        public RestClient client { get; set; }
        public RestRequest request { get; set; }
        public RestResponse response { get; set; }
        public Server(Configuration conf, ILogger logger, RMQ rmq) {
            this.conf = conf;
            this.logger = logger;
            this.rmq = rmq;
            client = new RestClient(new Uri(conf.apiConfiguration.baseUrl));
        }

        public void execute<T>(string resource, T obj, Method method)
        {
            request = new RestRequest(resource, method);

            request.AddHeaders(new Dictionary<string, string>()
            {
                ["Accept"] = "application/json",
                ["Authorization"] = $"Bearer {conf.apiConfiguration.token}",
            });
            request.AddJsonBody(JsonConvert.SerializeObject(obj));
            response = client.Execute(request);
            if (!response.IsSuccessful)
                logger.LogError($"Ошибка при выполнении запроса {conf.apiConfiguration.baseUrl}/{resource}:\n{response.Content}");
        }

        public void execute(string resource, IDictionary<string, string> args, Method method)
        {
            request = new RestRequest(resource, method);

            foreach (var arg in args)
                request.AddUrlSegment(arg.Key, arg.Value);

            request.AddHeaders(new Dictionary<string, string>()
            {
                ["Accept"] = "application/json",
                ["Authorization"] = $"Bearer {conf.apiConfiguration.token}",
            });

            response = client.Execute(request);
            if (!response.IsSuccessful)
                logger.LogError($"Ошибка при выполнении запроса {conf.apiConfiguration.baseUrl}/{resource}:\n{response.Content}");
        }
    }
}
