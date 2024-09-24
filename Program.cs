using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Scraper.Core.Classes.General;
using Scraper.Core.Sources;

public class Service1 : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Service1> _logger;
    private Service2 _service2;

    public Service1(IServiceProvider serviceProvider, ILogger<Service1> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Инициализация и запуск Service2
                if (_service2 == null)
                {
                    _service2 = new Service2(new LoggerFactory().CreateLogger<Service2>());
                    Task.Run(() => _service2.ExecuteAsync(CancellationToken.None));
                }

                // Имитация работы службы
                _logger.LogInformation("Service1 работает.");
                await Task.Delay(2000, stoppingToken);

                // Имитация ошибки
                if (new Random().Next(0, 5) == 0)
                    throw new Exception("Ошибка в Service1.");

                var response = await new HttpClient().GetAsync("https://ya.ru");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service1 произошла ошибка. Перезапускаем Service2.");
                RestartService2();
            }
        }
    }

    private void RestartService2()
    {
        _logger.LogInformation("Service2 перезапускается.");
        // Если необходимо делать что-то перед перезапуском, сделайте это здесь.

        _service2?.Stop();
        _service2 = new Service2(new LoggerFactory().CreateLogger<Service2>());
        Task.Run(() => _service2.ExecuteAsync(CancellationToken.None));
    }
}

public class Service2
{
    private readonly ILogger<Service2> _logger;
    private bool _isRunning;

    public Service2(ILogger<Service2> logger)
    {
        _logger = logger;
        _isRunning = false;
    }

    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _isRunning = true;
        while (_isRunning && !stoppingToken.IsCancellationRequested)
        {
            // Логика работы Service2
            _logger.LogInformation("Service2 работает.");
            await Task.Delay(2000, stoppingToken);
        }
        _isRunning = false;
    }

    public void Stop()
    {
        _isRunning = false;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<Service1>();
                // Здесь Service2 больше не добавляется в зависимости
            });
}


//public class RabbitMqListener : BackgroundService
//{
//    private readonly ILogger<RabbitMqListener> _logger;
//    private readonly IConnection _connection;
//    private readonly IConnection connection;
//    private readonly IModel channel;

//    public RabbitMqListener(ILogger<RabbitMqListener> logger, IConfiguration configuration)
//    {
//        //return;

//        _logger = logger;

//        Configuration? conf = configuration.Get<Configuration>();

//        conf.serverConfiguration = configuration.GetSection("server").Get<ServerConfiguration>();
//        conf.scraperConfiguration = configuration.GetSection("mangaclub").Get<ScraperConfiguration>();

//        MangaClub mangaClub = new MangaClub(conf);
//        mangaClub.parse();

//        //conf.scraperConfiguration = configuration.GetSection("mangaovh").Get<ScraperConfiguration>();

//        //Mangaovh mangaovh = new Mangaovh(conf);
//        //mangaovh.parse();

//        //conf.scraperConfiguration = configuration.GetSection("remanga").Get<ScraperConfiguration>();

//        //Remanga remanga = new Remanga(conf);
//        //remanga.parse();



//        //conf.scraperConfiguration = configuration.GetSection("mangalib").Get<ScraperConfiguration>();
//        //EdgeOptions options = new EdgeOptions()
//        //{
//        //    Proxy = new Proxy()
//        //    {
//        //        Kind = ProxyKind.Manual,
//        //        IsAutoDetect = false,   
//        //        HttpProxy = "proxy.example.com:8080",
//        //    },
//        //    PageLoadStrategy = PageLoadStrategy.Eager,
//        //};

//        //Mangalib mangalib = new Mangalib(conf, options);
//        //mangalib.parse();


//        //var factory = new ConnectionFactory() { HostName = "localhost" };
//        //connection = factory.CreateConnection();
//        //channel = connection.CreateModel();
//        //const string message = "Hello World!";
//        //var body = Encoding.UTF8.GetBytes(message);

//        //channel.BasicPublish(exchange: string.Empty,
//        //                     routingKey: "test",
//        //                     basicProperties: null,
//        //                     body: body);
//        //channel.QueueDeclare(queue: "test", durable: false, exclusive: false, autoDelete: false, arguments: null);
//    }

//    protected override Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        _logger.LogInformation("RabbitMqListener service is starting.");
//        //var consumer = new EventingBasicConsumer(channel);
//        //consumer.Received += (model, ea) =>
//        //{
//        //    var body = ea.Body.ToArray();
//        //    var message = Encoding.UTF8.GetString(body);
//        //    Console.WriteLine("Получено сообщение: {0}", message);
//        //};

//        //channel.BasicConsume(queue: "test", autoAck: true, consumer: consumer);

//        return Task.CompletedTask;
//    }

//    public override Task StopAsync(CancellationToken cancellationToken)
//    {
//        //channel.Close();
//        //connection.Close();
//        return base.StopAsync(cancellationToken);
//    }
//}

//public class TimeLogger : BackgroundService
//{
//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        while (!stoppingToken.IsCancellationRequested)
//        {
//            Console.WriteLine("Текущее время: {0}", DateTime.Now);
//            await Task.Delay(5000, stoppingToken); // Пауза в 5 секунд
//        }
//    }
//}