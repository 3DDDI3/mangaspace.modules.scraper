using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Scraper.Core.Classes.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Classes.RabbitMQ
{
    /// <summary>
    /// Работа с RabbtiMQ
    /// </summary>
    public class RMQ
    {
        private IConnection connection { get; set; }
        private ConnectionFactory factory { get; set; }
        public IModel channel { get; set; }
        public BasicDeliverEventArgs eventArgs { get; set; }
        public RMQMessage rmqMessage { get; set; }
        public EventingBasicConsumer consumer { get; set; }

        public RMQ(Configuration conf)
        {
            factory = new ConnectionFactory()
            {
                UserName = conf.rabbitMQConfiguration.username,
                Password = conf.rabbitMQConfiguration.password,
                Port = conf.rabbitMQConfiguration.port,
                HostName = conf.rabbitMQConfiguration.hostname
            }; 
            connection = factory.CreateConnection();
            channel = connection.CreateModel();
        }

        public void send(string exchange, string routingKey, object message)
        { 
            byte[] _message = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

            var properties = channel.CreateBasicProperties();

            if (rmqMessage != null)
                properties.Headers = new Dictionary<string, object>
                {
                    { "job_id", rmqMessage.jobId }
                };

            channel.BasicPublish(exchange, routingKey, properties, _message);
        }
    }
}
