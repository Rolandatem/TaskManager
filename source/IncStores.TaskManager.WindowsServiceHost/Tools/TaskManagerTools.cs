using Incstores.Common.Extensions;
using Incstores.Notification.Interfaces;
using Incstores.Notification.Models;
using Incstores.Notification.Settings;
using IncStores.Notification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IncStores.TaskManager.WindowsServiceHost.Tools
{
    public static class TaskManagerTools
    {
        public static async Task WritePhysicalFileExceptionAsync(Exception ex)
        {
            string logFileName = "ERROR_LOG.TXT";
            using StreamWriter writer = File.AppendText(logFileName);
            await writer.WriteLineAsync($"NEW ISSUE: {DateTime.Now}");
            await writer.WriteLineAsync(ex.DetailedMessage());
            await writer.WriteLineAsync();
        }

        public static async Task SendManualTwilioMessageAsync(string message)
        {
            string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            //--Manually read appsettings.json for phone number list.
            IConfigurationBuilder configBuilder = new ConfigurationBuilder()
                .AddJsonFile("settings/appsettings.json", false, true);

            if (env.ToLower() != "production")
            {
                configBuilder
                    .AddJsonFile("settings/appsettings.CommonDevelopment.json", false, true);
            }

            configBuilder.AddJsonFile($"settings/appsettings.{env}.json", false, true);

            IConfiguration config = configBuilder.Build();

            IServiceProvider serviceProvider = new ServiceCollection()
                .Configure<List<TwilioPhoneNumber>>(config.GetSection("systemCrashNotificationTextNumbers"))
                .Configure<TwilioSettings>(config.GetSection("twilioSettings"))
                .AddNotification()
                .BuildServiceProvider();

            List<string> toNotify = serviceProvider
                .GetService<IOptions<List<TwilioPhoneNumber>>>()
                .Value
                .Select(item => item.Number)
                .ToList();

            TwilioSettings twilioSettings = serviceProvider
                .GetService<IOptions<TwilioSettings>>()
                .Value;

            ITwilioUtil twilioUtil = serviceProvider.GetService<ITwilioUtil>();

            await twilioUtil.BroadcastNotificationAsync(toNotify, message);
        }
    }
}
