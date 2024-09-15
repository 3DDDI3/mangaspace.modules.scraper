using FluentFTP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Interfaces
{
    public interface IServer
    {
        public string url { get; set; }
        public string username { get;set; }
        public string password { get; set; }
        public string rootPath { get; set; }
        public FtpClient client { get; set; }
        
        public void connect()
        {
            client = new FtpClient(url, username, password);
            client.Connect();
        }
        public void disconnect() => client.Disconnect();
    }
}
