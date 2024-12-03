using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using Scraper.Core.DTO;
using System.Text;

namespace Scraper.Core.Classes.RabbitMQ
{
    public class RMQMessage
    {
        public RequestDTO RequestDTO { get; set; }
        private IDictionary<string, object> headers { get; set; }
        public string jobId { get; set; }
        public RMQMessage(BasicDeliverEventArgs eventArgs)
        {
            RequestDTO = JsonConvert.DeserializeObject<RequestDTO>(Encoding.UTF8.GetString(eventArgs.Body.ToArray()));
            headers = eventArgs.BasicProperties.Headers;
            jobId = Encoding.UTF8.GetString((byte[])headers.Where(x => x.Key == "id").First().Value);
        }
    }
}
