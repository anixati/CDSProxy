using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace CrmWebApiProxy
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Log.Logo();
                using (var whost = CreateWebHostBuilder(args))
                {
                    
                    var authr = whost.Services.GetService<ICRMAuthenticator>();
                    authr.EnsureConnection();
                    Log.Info($"Successfully logged in as User : {authr.UserName } as CRM User ID: {authr.UserId}");
                    whost.RunAsync().GetAwaiter().GetResult();
                    Log.Info("Shutting down..");
                }
            }
            catch (Exception ex)
            {
                Log.Info("Error!");
                Log.Error(ex.ToString());
                Console.ReadKey();
            }
        }

        public static IWebHost CreateWebHostBuilder(string[] args)
        {
            var config = new ConfigurationBuilder()
                            .AddCommandLine(args)
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                            .Build();
            var builder = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseConfiguration(config)
                .ConfigureLogging((hostingContext, logging) =>
                  {
                      logging.ClearProviders();
                      logging.AddConsole();
                  })
                            .UseKestrel()
                            .UseStartup<ProxyLoader>()
                            .Build();
            return builder;
        }
    }
}