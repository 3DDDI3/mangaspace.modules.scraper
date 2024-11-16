using FluentFTP;
using Scraper.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Classes.General
{
    public class FTPServer : IFTPServer
    {
        public string url { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string rootPath { get; set; }
        public FtpClient client { get; set; }
    }
}
