using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Scraper.Core.Classes.General;
using Scraper.Core.Classes.RabbitMQ;
using Scraper.Core.Sources;

public class InternetCheckService : IHostedService, IDisposable
{
    private readonly ILogger<InternetCheckService> _logger;
    private readonly TaskService _taskService;
    private Timer _timer;

    public InternetCheckService(ILogger<InternetCheckService> logger, TaskService taskService)
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

public class TaskService : BackgroundService
{
    private readonly ILogger<TaskService> _logger;
    private readonly Configuration _conf;
    private readonly RMQ _rmq;

    public TaskService(ILogger<TaskService> logger, IConfiguration conf)
    {
        _logger = logger;
        _conf = conf.Get<Configuration>();
        _conf.rabbitMQConfiguration = conf.GetSection("rabbitmq").Get<RabbitMQConfiguration>();
        _conf.serverConfiguration = conf.GetSection("server").Get<ServerConfiguration>();
        _conf.scraperConfiguration = conf.GetSection("remanga").Get<ScraperConfiguration>();
        _conf.apiConfiguration = conf.GetSection("api").Get<ApiConfiguration>();
        _conf.appConfiguration = conf.GetSection("app").Get<AppConfiguration>();
        _rmq = new RMQ(_conf);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
       
        _rmq.consumer.Received += (model, ea) =>
        {
            _rmq.eventArgs = ea;
            _rmq.rmqMessage = new RMQMessage(ea);

            _logger.LogInformation($"Получено задание {_rmq.rmqMessage.jobId} с сообщением: {Encoding.UTF8.GetString(ea.Body.ToArray())}");
                
            Remanga remanga = new Remanga(_conf, _rmq, _logger);
            remanga.parseChapters();



            byte[] message = Encoding.UTF8.GetBytes("test_message");

            _rmq.channel.BasicPublish(exchange: "scraper", routingKey: "response", basicProperties: null, body: message);

            _rmq.channel.BasicAck(ea.DeliveryTag, false);
        };

        _rmq.channel.BasicConsume(queue: "request", autoAck: false, consumer: _rmq.consumer);
    }

    public void Restart()
    {
        _logger.LogInformation("Restarting TaskService...");
        // Здесь можно реализовать логику остановки / перезапуска
    }
}

public class Program
{
    public static Task Main(string[] args) =>
        Host.CreateDefaultBuilder(args)
             .ConfigureLogging(logging =>
             {
                 logging.ClearProviders();
                 logging.AddConsole();
             })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<TaskService>(); // Изменено на Singleton
                services.AddHostedService<InternetCheckService>();
                services.AddHostedService<TaskService>(); // Теперь используется тот же экземпляр
            })
            .ConfigureAppConfiguration(x =>
            {
                var configuration = new ConfigurationBuilder()
                    //.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("settings.json", false, true)
                    .Build();
                x.AddConfiguration(configuration);
            })
            .Build()
            .RunAsync();
}
