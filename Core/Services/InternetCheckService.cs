using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        private Timer _timer;

        public InternetCheckService(ILogger<InternetCheckService> logger, ScraperService taskService)
        {
            _logger = logger;
            _taskService = taskService;
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
