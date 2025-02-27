using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scraper.Core.Classes.General;
using Scraper.Core.Classes.RabbitMQ;
using Scraper.Core.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Services
{
    public class InternetCheckService : IHostedService, IDisposable
    {
        private readonly ILogger<InternetCheckService> _logger;
        private readonly ScraperService _taskService;
        private readonly Configuration _conf;
        private readonly RMQ _rmq;
        private Timer _timer;

        public InternetCheckService(ILogger<InternetCheckService> logger, ScraperService taskService, IConfiguration conf)
        {
            _logger = logger;
            _taskService = taskService;
            _conf = conf.Get<Configuration>();
            _conf.rabbitMQConfiguration = conf.GetSection("rabbitmq").Get<RabbitMQConfiguration>();
            _rmq = new RMQ(_conf);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(CheckInternetConnection, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            return Task.CompletedTask;
        }

        private void CheckInternetConnection(object state)
        {
            if (!IsInternetAvailable())
            {
                _logger.LogError("Internet connection lost. Attempting to restart TaskService...");
                _taskService.Restart();
            }
        }

        private bool IsInternetAvailable()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    using (var response = client.GetAsync("https://ya.ru").Result)
                    {
                        return response.IsSuccessStatusCode;
                    }
                }
            }
            catch
            {
                _rmq.send("information", "errorLog", new LogDTO($"<b>[{DateTime.Now.ToString("HH:mm:ss")}]:</b> Ошибка: отсутствие интернет соеднинения", false));
                return false;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
