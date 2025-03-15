using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Scraper.Core.ActionHandlers;
using Scraper.Core.Classes.General;
using Scraper.Core.Classes.RabbitMQ;
using Scraper.Core.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Services
{
    public class ScraperService : BackgroundService
    {
        private readonly ILogger<ScraperService> _logger;
        private readonly Configuration _conf;
        private readonly IConfiguration conf;
        private readonly RMQ _rmq;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly IServiceProvider _serviceProvider;
        private readonly ActionHandlerFactory _actionHandlerFactory;

        public ScraperService(ILogger<ScraperService> logger, IConfiguration conf, ActionHandlerFactory actionHandlerFactory)
        {
            _logger = logger;
            this.conf = conf;
            _cancellationTokenSource = new CancellationTokenSource();
            _actionHandlerFactory = actionHandlerFactory;

            _conf = conf.Get<Configuration>();
            _conf.rabbitMQConfiguration = conf.GetSection("rabbitmq").Get<RabbitMQConfiguration>();
            _conf.serverConfiguration = conf.GetSection("server").Get<ServerConfiguration>();
            _conf.apiConfiguration = conf.GetSection("api").Get<ApiConfiguration>();
            _conf.appConfiguration = conf.GetSection("app").Get<AppConfiguration>();
            _conf.scraperConfiguration = conf.GetSection("remanga").Get<ScraperConfiguration>();
            _rmq = new RMQ(_conf);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queues = new List<string> { "getChapterRequest", "parseChapterRequest", "parseTitleRequest" }; // Задайте список очередей

            foreach (var queue in queues)
            {
                ListenQueue(queue);
            }

            // Ожидание отмены
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private void ListenQueue(string queue)
        {
            _rmq.consumer = new EventingBasicConsumer(_rmq.channel);

            _rmq.consumer.Received += (model, ea) =>
            {
                _rmq.eventArgs = ea;
                _rmq.rmqMessage = new RMQMessage(ea);

                _logger.LogInformation($"Получено задание {_rmq.rmqMessage.jobId} с сообщением: {Encoding.UTF8.GetString(ea.Body.ToArray())}");
                _actionHandlerFactory.GetHandler(_rmq.rmqMessage.RequestDTO.scraperDTO.engine).Handle(_conf, conf, _rmq, _logger);
                _rmq.channel.BasicAck(ea.DeliveryTag, true);
            };

            _rmq.channel.BasicConsume(queue, false, _rmq.consumer);
        }

        public void Restart()
        {
            _logger.LogInformation("Attempting to restart TaskService...");
            StopAsync(CancellationToken.None).Wait(); // Остановка текущего сервиса
            _cancellationTokenSource = new CancellationTokenSource(); // Новый токен отмены
            Task.Run(() => ExecuteAsync(CancellationToken.None)); // Перезапуск сервиса
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource.Cancel(); // Отмена токена
            return base.StopAsync(cancellationToken);
        }
    }

}
