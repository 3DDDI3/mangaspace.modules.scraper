using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium;
using RabbitMQ.Client;
using Scraper.Core.Sources;
using Configuration = Scraper.Core.Classes.General.Configuration;
using Scraper.Core.Classes.General;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        await host.RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
             .ConfigureLogging(logging =>
             {
                 logging.ClearProviders();
                 logging.AddConsole();
             })
            .ConfigureAppConfiguration(x =>
            {
                var configuration = new ConfigurationBuilder()
                //.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("settings.json", false, true)
                    .Build();
                x.AddConfiguration(configuration);
            })
            .ConfigureServices((hostContext, services) =>
            {  
                services.AddHostedService<RabbitMqListener>();
                //services.AddHostedService<TimeLogger>();
            });
}

public class RabbitMqListener : BackgroundService
{
    private readonly ILogger<RabbitMqListener> _logger;
    private readonly IConnection _connection;
    private readonly IConnection connection;
    private readonly IModel channel;

    public RabbitMqListener(ILogger<RabbitMqListener> logger, IConfiguration configuration)
    {
        //return;

        _logger = logger;

        Configuration? conf = configuration.Get<Configuration>();
        conf.scraperConfiguration = configuration.GetSection("remanga").Get<ScraperConfiguration>();
        conf.serverConfiguration = configuration.GetSection("server").Get<ServerConfiguration>();

        //Remanga remanga = new Remanga(conf);
        //remanga.parse();

        conf.scraperConfiguration = configuration.GetSection("mangalib").Get<ScraperConfiguration>();
        EdgeOptions options = new EdgeOptions()
        {
            Proxy = new Proxy()
            {
                Kind = ProxyKind.Manual,
                IsAutoDetect = false,   
                HttpProxy = "proxy.example.com:8080",
            },
            PageLoadStrategy = PageLoadStrategy.Eager,
        };

        Mangalib mangalib = new Mangalib(conf, options);
        mangalib.parse();


        //var factory = new ConnectionFactory() { HostName = "localhost" };
        //connection = factory.CreateConnection();
        //channel = connection.CreateModel();
        //const string message = "Hello World!";
        //var body = Encoding.UTF8.GetBytes(message);

        //channel.BasicPublish(exchange: string.Empty,
        //                     routingKey: "test",
        //                     basicProperties: null,
        //                     body: body);
        //channel.QueueDeclare(queue: "test", durable: false, exclusive: false, autoDelete: false, arguments: null);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RabbitMqListener service is starting.");
        //var consumer = new EventingBasicConsumer(channel);
        //consumer.Received += (model, ea) =>
        //{
        //    var body = ea.Body.ToArray();
        //    var message = Encoding.UTF8.GetString(body);
        //    Console.WriteLine("Получено сообщение: {0}", message);
        //};

        //channel.BasicConsume(queue: "test", autoAck: true, consumer: consumer);

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        //channel.Close();
        //connection.Close();
        return base.StopAsync(cancellationToken);
    }
}

public class TimeLogger : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Console.WriteLine("Текущее время: {0}", DateTime.Now);
            await Task.Delay(5000, stoppingToken); // Пауза в 5 секунд
        }
    }
}