using Gravitybox.gFileSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.gFileSystem.Service
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Any(x => x == "-console" || x == "/console"))
            {
                try
                {
                    var service = new PersistentService();
                    service.Start();
                    Console.WriteLine("Press <ENTER> to stop...");
                    Console.ReadLine();
                    service.Stop();
                }
                catch (Exception ex)
                {
                    //LoggerCQ.LogError(ex, "Failed to start service from console.");
                    throw;
                }
            }
            else
            {
                try
                {
                    var servicesToRun = new ServiceBase[]
                                        {
                                            new PersistentService()
                                        };
                    ServiceBase.Run(servicesToRun);
                }
                catch (Exception ex)
                {
                    //LoggerCQ.LogError(ex, "Failed to start service.");
                }
            }
        }
    }
}
