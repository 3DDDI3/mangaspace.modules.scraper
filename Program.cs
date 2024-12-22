using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scraper.Core.ActionHandlers;
using Scraper.Core.Classes.General;
using Scraper.Core.Classes.RabbitMQ;
using Scraper.Core.Factories;
using Scraper.Core.Interfaces;
using Scraper.Core.Services;


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
                //services.AddSingleton<RMQ>(); // Регистрация RMQ
                services.AddTransient<ActionHandlerFactory>(); // Регистрация обработчика Remanga
                services.AddTransient<BaseActionHandler>(); // Регистрация фабрики
                services.AddTransient<IActionHandler, MangalibHandler>();
                services.AddTransient<IActionHandler, RemangaHandler>();
                services.AddTransient<TaskService>(); // Изменено на Transient
                services.AddHostedService<InternetCheckService>();
                services.AddHostedService<TaskService>();
            })
            .ConfigureAppConfiguration(x =>
            {
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("settings.json", false, true)
                    .Build();
                x.AddConfiguration(configuration);
            })
            .Build()
            .RunAsync();
}


