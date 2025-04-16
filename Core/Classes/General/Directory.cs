using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Classes.General
{
    public class CustomDirectory
    {
        private string rootPath;
        public CustomDirectory(string rootPath) => this.rootPath = rootPath;
        public void createDirectory(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                File.SetUnixFileMode(
                   $"{rootPath}/{path}",
                   UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |  // Владелец: rwx (7)
                   UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute | // Группа: rwx (7)
                   UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute                        // Остальные: r-x (5)
                );

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
                Directory.CreateDirectory(@$"{rootPath}\{path}");
            
        }
    }
}
