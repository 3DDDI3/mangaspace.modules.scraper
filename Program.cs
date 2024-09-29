using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

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
    private readonly IConnection connection;
    private readonly IModel channel;

    public TaskService(ILogger<TaskService> logger)
    {
        _logger = logger;

        var factory = new ConnectionFactory() { HostName = "localhost" };
        connection = factory.CreateConnection();
        channel = connection.CreateModel();

        //const string message = "Hello World!";
        //var body = Encoding.UTF8.GetBytes(message);

        //channel.BasicPublish(exchange: string.Empty,
        //                     routingKey: "test",
        //                     basicProperties: null,
        //                     body: body);
        //channel.QueueDeclare(queue: "test", durable: false, exclusive: false, autoDelete: false, arguments: null);

    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var headers = ea.BasicProperties.Headers;
            Console.WriteLine($"Получено сообщение: {message} с заголовком: {headers}");
        };

        channel.BasicConsume(queue: "test", autoAck: false, consumer: consumer);

        //_logger.LogInformation("TaskService is running.");
        //await Task.Delay(1000, stoppingToken);
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
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<TaskService>(); // Изменено на Singleton
                services.AddHostedService<InternetCheckService>();
                services.AddHostedService<TaskService>(); // Теперь используется тот же экземпляр
            })
            .Build()
            .RunAsync();
}
