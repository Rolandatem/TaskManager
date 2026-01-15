using Incstores.Common.Extensions;
using Incstores.Common.Settings;
using Incstores.EntityActivityLogger.Settings;
using Incstores.Notification.Models;
using Incstores.Notification.Settings;
using Incstores.Shipping.Tracking.Load.Recipes.Settings;
using Incstores.Shipping.Tracking.Load.Settings;
using Incstores.Shipping.Tracking.Process.Recipes.Settings;
using Incstores.Shipping.Tracking.Process.Settings;
using IncStores.Notification;
using IncStores.TaskManager.Core.Settings;
using IncStores.TaskManager.Core.Tools;
using IncStores.TaskManager.DataLayer.Settings;
using IncStores.TaskManager.GeneralRecipes.Settings;
using IncStores.TaskManager.RecipeRunnerService;
using IncStores.TaskManager.RecipeRunnerService.Models;
using IncStores.TaskManager.RecipeRunnerService.Settings;
using IncStores.TaskManager.RecipeRunnerService.SignalR;
using IncStores.TaskManager.WindowsServiceHost.Models;
using IncStores.TaskManager.WindowsServiceHost.Tools;
using MicroKnights.Log4NetHelper;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Authentication;
using System.ServiceModel;
using System.Threading.Tasks;

namespace IncStores.TaskManager.WindowsServiceHost
{
    public class Program
    {
        private static CommandLineArguments _commandLineArguments = null;
        public static IWebHost webHost { get; set; }

        public static async Task Main(string[] args)
        {
            //Debugger.Launch();
            _commandLineArguments = new CommandLineArguments(args);

            await StartServerAsync();
        }

        public static async Task StartServerAsync()
        {
            try
            {
                if (_commandLineArguments.Environment.Exists())
                { Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", _commandLineArguments.Environment); }

                bool isService = !(Debugger.IsAttached || _commandLineArguments.RunAsConsole);
                if (isService)
                {
                    var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
                    var pathToContentRoot = Path.GetDirectoryName(pathToExe);
                    Directory.SetCurrentDirectory(pathToContentRoot);
                }

                webHost = CreateHostBuilder(_commandLineArguments.Args)
                    .UseUrls("http://*:9001")
                    .UseKestrel(options =>
                    {
                        options.ConfigureHttpsDefaults(connectionOptions =>
                        {
                            connectionOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
                        });
                    })
                    .Build();

                if (isService)
                {
                    webHost.RunAsService();
                }
                else
                {
                    await webHost.RunAsync();
                    //webHost.Run();
                }
            }
            catch (OperationCanceledException ex)
            {

            }
            catch (Exception ex)
            {
                await NotifyApplicationStartupCrash(ex);
            }
        }

        public static IWebHostBuilder CreateHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostContext, configurationBuilder) =>
                {
                    IHostEnvironment env = hostContext.HostingEnvironment;

                    //--Default Prod load
                    configurationBuilder.AddJsonFile("settings/appsettings.json", false, true);
                    configurationBuilder.AddJsonFile("settings/orderPostProcessorRecipes.json", false, true);
                    configurationBuilder.AddJsonFile("settings/createSalesOrderRecipes.json", false, true);
                    configurationBuilder.AddJsonFile("settings/suiteTalkAPI.json", false, true);
                    configurationBuilder.AddJsonFile("settings/netsuiteSyncRecipes.json", false, true);
                    configurationBuilder.AddJsonFile("settings/shippingTrackingLoadSettings.json", false, true);

                    //--Development
                    if (env.IsProduction() == false)
                    {
                        configurationBuilder
                            .AddJsonFile($"settings/appsettings.CommonDevelopment.json", false, true);
                    }

                    configurationBuilder.AddJsonFile($"settings/appsettings.{env.EnvironmentName}.json", false, true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration config = hostContext.Configuration;
                    services
                        //--Option models
                        .Configure<ConnectionStrings>(config.GetSection("connectionStrings"))
                        .Configure<TwilioSettings>(config.GetSection("twilioSettings"))
                        .Configure<List<TwilioPhoneNumber>>("systemCrashPhoneNumbers", config.GetSection("systemCrashNotificationTextNumbers"))
                        //.Configure<PostProcessorSettings>(config.GetSection("PostProcessorSettings"))
                        .Configure<MaintenanceWindowSettings>(config.GetSection("maintenanceWindowSettings"))
                        .Configure<ShippingTrackingLoadSettings>(config.GetSection("shippingTrackingLoadSettings"))

                        //.Configure<NetSuiteSettings>(config.GetSection("NetSuiteSettings"))
                        //.Configure<CreateSalesOrderSettings>(config.GetSection("CreateSalesOrderSettings"))
                        //.Configure<NetSuiteSettings>(config.GetSection("NetSuiteSettings"))
                        //.Configure<SuiteTalkAPIConfiguration>(config.GetSection("SuiteTalkAPI"))

                        //--Windows Services
                        .AddHostedService<RecipeRunnerWindowsService>()

                        //--Tool Services
                        //.AddAuditHelperService()
                        .AddSingleton<IAuditHelper, ServiceAuditHelper>()

                        //--Add Data Layer services.
                        .AddTaskManagerDataLayerServices()

                        //--Add Recipe Libraries
                        .AddGeneralRecipesServices()

                        //--App Services
                        .AddTaskManagerCoreServices(config)
                        .AddRecipeRunnerServices(config)
                        .AddEntityActivityLogging()
                        .AddNotification()

                        // Shipping Services
                        .AddShippingLoadTracking()
                        .AddShippingLoadTrackingRecipes()
                        .AddShippingProcessTracking()
                        .AddShippingProcessTrackingRecipes()

                        // Order Recipes
                        //.AddOrderPostProcessors()
                        //.AddOrderPostProcessorRecipes()

                        // NetSuite Recipes and Dependencies
                        //.AddNetsuiteSalesOrderRecipes()
                        //.AddNetSuiteSalesOrder()
                        //.AddSuiteTalk()
                        //.AddSynchronization()
                        //.AddNetsuiteSynchronizationRecipes()


                        //--Add SignalR
                        .AddSignalR();

                    // Add Google Maps API  <-- used by the Order PostProcessors
                    services
                        .AddHttpClient("googleMaps", c =>
                        {
                            c.BaseAddress = new Uri("https://maps.googleapis.com/maps/api/distancematrix/");
                        });

                    //--Add Web Services
                    services
                        .AddTransient<INotify>(provider =>
                        {
                            NotifyClient client = new NotifyClient();
                            client.Endpoint.Address = new EndpointAddress(config["services:notificationServiceUrl"]);
                            client.Endpoint.Binding = new BasicHttpBinding()
                            {
                                MaxReceivedMessageSize = 2147483647
                            };

                            return client;
                        });
                })
                .ConfigureLogging((hostContext, logging) =>
                {
                    IHostEnvironment env = hostContext.HostingEnvironment;
                    IConfiguration config = hostContext.Configuration;

                    logging
                        .AddLog4Net("settings/log4net.config")
                        .SetMinimumLevel(
                            _commandLineArguments.OverrideMinLogLevel ??
                            (env.IsProduction() ? LogLevel.Error : LogLevel.Debug));

                    log4net.GlobalContext.Properties["environment"] = env.EnvironmentName;
                    log4net.Config.BasicConfigurator.Configure(new NotifyOfErrorAppender());

                    //--Uncomment below to debug MicroKnights.Log4Net
                    //InternalDebugHelper.EnableInternalDebug((source, args2) =>
                    //{
                    //    Console.WriteLine(args2.LogLog);
                    //});

                    AdoNetAppenderHelper.SetConnectionString(config["connectionStrings:log4net"]);
                })
                .Configure((applicationBuilder) =>
                {
                    applicationBuilder.UseRouting();
                    applicationBuilder.UseEndpoints(endpoints =>
                    {
                        endpoints.MapHub<TaskManagerRecipeHub>("/maintaskmanagerhub");
                    });
                });

        public static async Task NotifyApplicationStartupCrash(Exception ex)
        {
            //--Write a file to the server containing information from the exception.
            await TaskManagerTools.WritePhysicalFileExceptionAsync(ex);

            //--Notify system watchers.
            await TaskManagerTools.SendManualTwilioMessageAsync("A system exception has caused the TaskManager to crash. A detailed file has been generated on the server.");
        }
    }
}
