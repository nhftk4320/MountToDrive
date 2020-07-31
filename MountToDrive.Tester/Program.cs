using DokanNet;
using DokanNet.Logging;
using MountToDrive.Core;
using MountToDrive.MemoryStoragePlugin;
using System;

namespace MountToDrive.Tester
{
    class Program
    {
        static void Main(string[] _)
        {
            ILogger logger = new Logger(Logger.LogLevelEnum.Warn);

            using MemoryStorage memoryStorage = new MemoryStorage("memStoragae", 5000);

            DokanImplementer dokanImplementer = new DokanImplementer(memoryStorage);
            try
            {
                Dokan.Mount(dokanImplementer, "R:\\", 0, 32, logger);
                Console.ReadLine();
            }
            finally
            {
                Dokan.Unmount('R');
            }
        }
    }
}
