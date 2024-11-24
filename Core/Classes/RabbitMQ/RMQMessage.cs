using RabbitMQ.Client.Events;
using System.Text;

namespace Scraper.Core.Classes.RabbitMQ
{
    public class RMQMessage
    {
        public string message { get; set; }
        private IDictionary<string, object> headers { get; set; }
        public string jobId { get; set; }
        public RMQMessage(BasicDeliverEventArgs eventArgs)
        {
            message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            headers = eventArgs.BasicProperties.Headers;
            jobId = Encoding.UTF8.GetString((byte[])headers.Where(x => x.Key == "id").First().Value);
        }
    }
}
