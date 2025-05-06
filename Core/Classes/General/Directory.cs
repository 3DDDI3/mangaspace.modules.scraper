using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Classes.General
{
    public class CustomDirectory
    {
        public string rootPath { get; set; }
        public CustomDirectory(string rootPath) => this.rootPath = rootPath;
        public void createDirectory(string path)
        {
            if (!Directory.Exists(Path.Combine(rootPath, path)))
            {
                Directory.CreateDirectory(Path.Combine(rootPath, path));

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    File.SetUnixFileMode(
                       Path.Combine(rootPath, path),
                       UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |  // Владелец: rwx (7)
                       UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute | // Группа: rwx (7)
                       UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute                        // Остальные: r-x (5)
                    );
                }
            }
            rootPath = Path.Combine(rootPath, path);
        }

        public void createFile(string file, MemoryStream stream)
        {
            using (FileStream fileStream = new FileStream($"{Path.Combine(rootPath,file)}", FileMode.Create, FileAccess.Write))
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(fileStream);
            }
        }
    }
}
