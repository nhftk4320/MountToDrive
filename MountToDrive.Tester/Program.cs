using DokanNet;
using DokanNet.Logging;
using MountToDrive.AnalysisTools;
using MountToDrive.Core;
using MountToDrive.Plugins.GoogleDriveStorage;
using MountToDrive.Plugins.MemoryStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MountToDrive.Tester
{
    class Program
    {
        static void Main(string[] _)
        {
            TestGDriveStorage();
        }

        static void TestGDriveStorage()
        {
            ILogger logger = new Logger(Logger.LogLevelEnum.Error);

            using GoogleDriveApiHandler driveApiHandler = new GoogleDriveApiHandler("credentials.json");
            using GoogleDriveStorage driveStorage = new GoogleDriveStorage(driveApiHandler, (msg, args) => logger.Info(msg, args));

            DokanImplementer dokanImplementer = new DokanImplementer(driveStorage, (msg, args) => logger.Info(msg, args));

            Dokan.Mount(dokanImplementer, "R:\\", DokanOptions.NetworkDrive | DokanOptions.WriteProtection, 1, logger);
        }

        static void TestMeasureMemoryStorage()
        {
            Task dokanTask = null;
            ILogger logger = new Logger(Logger.LogLevelEnum.Warn);

            using MemoryStorage memoryStorage = new MemoryStorage("memStoragae", 5000);
            PluginMeasureWrapper pluginMeasureWrapper = new PluginMeasureWrapper(memoryStorage);

            DokanImplementer dokanImplementer = new DokanImplementer(pluginMeasureWrapper, (_, __) => { });

            using Timer timer = new Timer(_ => PrintCounterAndReset(pluginMeasureWrapper), null, 2000, 1000);

            try
            {
                dokanTask = Task.Run(() => Dokan.Mount(dokanImplementer, "R:\\", 0, 32, logger));
                Console.ReadLine();
            }
            finally
            {
                Dokan.Unmount('R');
                try
                {
                    dokanTask?.Wait();
                }
                catch (TaskCanceledException) { }
            }

            PrintCounter(pluginMeasureWrapper.CountersByMethod);
        }

        static void PrintCounterAndReset(PluginMeasureWrapper pluginMeasureWrapper)
        {
            //Console.Clear();
            Console.SetCursorPosition(0, 0);
            PrintCounter(pluginMeasureWrapper.CountersByMethod);
            //pluginMeasureWrapper.ResetCounter();
        }

        static void PrintCounter(Dictionary<string, StrongBox<int>> counterOfFunc)
        {
            foreach (var counterOfFuncPart in counterOfFunc.OrderBy(counterOfFuncPart => counterOfFuncPart.Value.Value))
            {
                Console.WriteLine("{0}: {1} calls                            ", counterOfFuncPart.Key, counterOfFuncPart.Value.Value);
            }
        }
    }
}
