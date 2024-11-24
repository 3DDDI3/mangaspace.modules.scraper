using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Scraper.Core.Classes.RabbitMQ;
using System.Runtime.InteropServices;

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

        public void execute(string resource, object obj, Method method)
        {
            request = new RestRequest(resource, method);

            request.AddHeaders(new Dictionary<string, string>()
            {
                ["Accept"] = "application/json",
                ["Authorization"] = $"Bearer {conf.apiConfiguration.token}",
                ["User-Agent"] = $"{conf.appConfiguration.name}/{conf.appConfiguration.version} ({RuntimeInformation.OSDescription};{RuntimeInformation.OSArchitecture};{RuntimeInformation.FrameworkDescription}) lang=ru-RU",
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
            {
                if (String.IsNullOrEmpty(arg.Value))
                    continue;
                request.AddUrlSegment(arg.Key, arg.Value);
            }

            request.AddHeaders(new Dictionary<string, string>()
            {
                ["Accept"] = "application/json",
                ["Authorization"] = $"Bearer {conf.apiConfiguration.token}",
                ["User-Agent"] = $"{conf.appConfiguration.name}/{conf.appConfiguration.version} ({RuntimeInformation.OSDescription};{RuntimeInformation.OSArchitecture};{RuntimeInformation.FrameworkDescription}) lang=ru-RU",
            });

            response = client.Execute(request);
            if (!response.IsSuccessful)
                logger.LogError($"Ошибка при выполнении запроса {conf.apiConfiguration.baseUrl}/{resource}:\n{response.Content}");
        }
    }
}
