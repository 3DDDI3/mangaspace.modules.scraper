namespace Scraper.Core.Classes.General
{
    public class Configuration
    {
        public ApiConfiguration apiConfiguration { get; set; }
        public AppConfiguration appConfiguration { get; set; }
        public ScraperConfiguration scraperConfiguration { get; set; }
        public ServerConfiguration serverConfiguration { get; set; }
        public RabbitMQConfiguration rabbitMQConfiguration { get;set; }
    }

    public class ScraperConfiguration
    {
        public string baseUrl { get; set; }
        public string catalogUrl { get; set; }
        public string pages { get; set; }
        public string authorization {  get; set; }   
    }

    public class ApiConfiguration
    {
        public string token { get; set; }
        public string baseUrl { get; set; }
    }

    public class AppConfiguration
    {
        public string name { get; set; }
        public string version { get; set; }
        public string path {  get; set; }
        public bool containerized { get; set; }
    }

    public class ServerConfiguration
    {
        public string url { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string rootPath { get; set; }
    }

    public class RabbitMQConfiguration
    {
        public string username { get; set; }
        public string password { get; set; }
        public int port { get; set; }
        public string hostname { get; set; }
        public string requestedHeartbeat { get; set; }
    }
}
