using Incstores.Common.Extensions;
using Incstores.Notification.Interfaces;
using Incstores.Notification.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IncStores.TaskManager.RecipeRunnerService.Tools
{
    public interface IGeneralTools
    {
        Task SendSystemWatcherSMSMessageAsync(string message);
        Task WritePhysicalFileExceptionAsync(Exception ex, string sender = "");
    }

    internal class GeneralTools : IGeneralTools
    {
        #region "Member Variables"
        readonly ITwilioUtil _twilioUtil = null;
        readonly List<TwilioPhoneNumber> _systemWatcherPhoneNumbers = null;
        #endregion

        #region "Constructor"
        public GeneralTools(
            ITwilioUtil twilioUtil,
            IOptionsSnapshot<List<TwilioPhoneNumber>> systemWatcherPhoneNumbers)
        {
            _twilioUtil = twilioUtil;
            _systemWatcherPhoneNumbers = systemWatcherPhoneNumbers.Get("systemCrashPhoneNumbers");
        }
        #endregion

        public async Task SendSystemWatcherSMSMessageAsync(string message)
        {
            await _twilioUtil.BroadcastNotificationAsync(
                _systemWatcherPhoneNumbers
                    .Select(w => w.Number)
                    .ToList(),
                message);
        }

        public async Task WritePhysicalFileExceptionAsync(Exception ex, string sender = "")
        {
            string logFileName = "ERROR_LOG.TXT";
            using StreamWriter writer = File.AppendText(logFileName);
            await writer.WriteLineAsync($"NEW ISSUE: {DateTime.Now}");
            if (String.IsNullOrWhiteSpace(sender) == false)
            {
                await writer.WriteLineAsync($"REPORTER: {sender}");
            }
            await writer.WriteLineAsync(ex.DetailedMessage());
            await writer.WriteLineAsync();
        }
    }
}
