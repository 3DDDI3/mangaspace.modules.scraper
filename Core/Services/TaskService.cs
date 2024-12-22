using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    public class TaskService : BackgroundService
    {
        private readonly ILogger<TaskService> _logger;
        private readonly Configuration _conf;
        private readonly RMQ _rmq;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly IServiceProvider _serviceProvider;
        private readonly ActionHandlerFactory _actionHandlerFactory;

        public TaskService(ILogger<TaskService> logger, IConfiguration conf, ActionHandlerFactory actionHandlerFactory)
        {
            _logger = logger;
            _conf = conf.Get<Configuration>();
            _conf.rabbitMQConfiguration = conf.GetSection("rabbitmq").Get<RabbitMQConfiguration>();
            _conf.serverConfiguration = conf.GetSection("server").Get<ServerConfiguration>();
            _conf.scraperConfiguration = conf.GetSection("remanga").Get<ScraperConfiguration>();
            _conf.apiConfiguration = conf.GetSection("api").Get<ApiConfiguration>();
            _conf.appConfiguration = conf.GetSection("app").Get<AppConfiguration>();
            _rmq = new RMQ(_conf);
            _cancellationTokenSource = new CancellationTokenSource();
            _actionHandlerFactory = actionHandlerFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                // Регистрация обработчика сообщений
                _rmq.consumer.Received += (model, ea) =>
                {
                    _rmq.eventArgs = ea;
                    _rmq.rmqMessage = new RMQMessage(ea);

                    _logger.LogInformation($"Получено задание {_rmq.rmqMessage.jobId} с сообщением: {Encoding.UTF8.GetString(ea.Body.ToArray())}");

                    _actionHandlerFactory.GetHandler(_rmq.rmqMessage.RequestDTO.scraperDTO.engine).Handle();

                    _rmq.channel.BasicAck(ea.DeliveryTag, false);
                };

                // Начало получения сообщений
                _rmq.channel.BasicConsume(queue: "getChapterRequest", autoAck: false, consumerTag: "getChapterRequest", false, false, null, _rmq.consumer);
                await Task.Delay(500, stoppingToken); // Позволяет корректно обрабатывать отмену
            }
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
