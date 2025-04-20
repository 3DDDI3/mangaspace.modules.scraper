using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Scraper.Core.Classes.RabbitMQ;
using Scraper.Core.DTO;
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
        public Server(Configuration conf, ILogger logger, RMQ rmq)
        {
            this.conf = conf;
            this.logger = logger;
            this.rmq = rmq;
            client = new RestClient(new Uri(conf.apiConfiguration.baseUrl));
        }

        public Server(string resorceBaseUrl)
        {
            client = new RestClient(new Uri(resorceBaseUrl));
        }

        /// <summary>
        /// Отправка запроса к api ресурса
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="obj"></param>
        /// <param name="method"></param>
        public void execute(string resource, object obj, Method method)
        {
            try
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
                {
                    JObject jObj = JObject.Parse(response.Content);
                    logger.LogError($"Ошибка при выполнении запроса {conf.apiConfiguration.baseUrl}/{resource}: {jObj["message"].ToString()}");
                    rmq.send("information", "errorLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Ошибка при попытке выполнения запроса {conf.apiConfiguration.baseUrl}/{resource}: {jObj["message"].ToString()}", false));
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"{ex.Message}\n {ex.InnerException}\n {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Отправка запроса к api ресурса
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="args"></param>
        /// <param name="method"></param>
        public void execute(string resource, Method method, List<KeyValuePair<string, string>>? args = null)
        {
            request = new RestRequest(resource, method);

            if (args != null)
                foreach (var arg in args)
                {
                    request.AddParameter(arg.Key, arg.Value);
                } 

            request.AddHeaders(new Dictionary<string, string>()
            {
                ["Accept"] = "application/json",
                ["Authorization"] = $"Bearer {conf.apiConfiguration.token}",
                ["User-Agent"] = $"{conf.appConfiguration.name}/{conf.appConfiguration.version} ({RuntimeInformation.OSDescription};{RuntimeInformation.OSArchitecture};{RuntimeInformation.FrameworkDescription}) lang=ru-RU",
            });

            response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                JObject jObj = JObject.Parse(response.Content);
                logger.LogError($"Ошибка при выполнении запроса {conf.apiConfiguration.baseUrl}/{resource}: {jObj["message"].ToString()}");
                rmq.send("information", "errorLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Ошибка при попытке выполнения запроса {conf.apiConfiguration.baseUrl}/{resource}: {jObj["message"].ToString()}", false));
            }
        }

        /// <summary>
        /// Отправка запроса к внешнему ресурсу
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="args"></param>
        /// <param name="method"></param>
        public void externalExecute(string resource, Method method, List<KeyValuePair<string, string>>? args = null)
        {
            request = new RestRequest(resource, method);

            request.AddHeaders(new Dictionary<string, string>()
            {
                ["Accept"] = "application/json"
            });

            if (args != null)
                foreach (var arg in args)
                {
                    request.AddParameter(arg.Key, arg.Value);
                }

            response = client.Execute(request);
        }

    }
}
