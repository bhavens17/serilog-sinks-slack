using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.PeriodicBatching;
using Serilog.Sinks.Slack.Enums;
using Serilog.Sinks.Slack.Helpers;
using Serilog.Sinks.Slack.Models;

namespace Serilog.Sinks.Slack
{
    /// <summary>
    /// Implements <see cref="ILogEventSink"/>, <see cref="IBatchedLogEventSink"/>, <see cref="IDisposable"/> and provides means needed for sending Serilog log events to Slack.
    /// </summary>
    public class SlackSink : ILogEventSink, IBatchedLogEventSink, IDisposable
    {
        private readonly HttpClient _client = new HttpClient();

        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        private readonly SlackSinkOptions _options;
        private readonly SlackMessageFormatter _textFormatter;
        private readonly PeriodicBatchingSink _periodicBatchingSink;
        private bool _disposed = false;

#if NETFRAMEWORK
        static SlackSink()
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
        }
#endif

        /// <summary>
        /// Initializes new instance of <see cref="SlackSink"/>.
        /// </summary>
        /// <param name="options">Slack sink options object.</param>
        /// <param name="textFormatter">Formatter used to convert log events to text.</param>
        public SlackSink(SlackSinkOptions options, ITextFormatter textFormatter)
        {
			_textFormatter = new SlackMessageFormatter(options, textFormatter);
			var client = new SlackClient();
			var batchingSink = new SlackBatchingSink(_textFormatter, client, options);

			_periodicBatchingSink = new PeriodicBatchingSink(batchingSink, options.ToPeriodicBatchingSinkOptions());
        }

        /// <summary>
        /// Implements <see cref="ILogEventSink.Emit"/> method and forwards the <see cref="LogEvent"/> to the underlying <see cref="PeriodicBatchingSink"/>.
        /// </summary>
        /// <param name="logEvent">The <see cref="LogEvent"/>.</param>
        void ILogEventSink.Emit(LogEvent logEvent)
        {
            _periodicBatchingSink.Emit(logEvent);
        }

        /// <summary>
        /// Implements <see cref="IBatchedLogEventSink.EmitBatchAsync"/> method and uses <see cref="HttpClient"/> to post <see cref="LogEvent"/> to Slack.
        /// </summary>
        /// <param name="events">Collection of <see cref="LogEvent"/>.</param>
        /// <returns>Awaitable task.</returns>
        async Task IBatchedLogEventSink.EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            foreach (var logEvent in events)
            {
                if (logEvent.Level < _options.MinimumLogEventLevel) continue;
                var message = _textFormatter.CreateMessage(logEvent);
                var json = JsonConvert.SerializeObject(message, _jsonSerializerSettings);
                await _client.PostAsync(_options.WebHookUrl, new StringContent(json));
            }
        }

        /// <summary>
        /// Implements <see cref="IBatchedLogEventSink.OnEmptyBatchAsync"/> method and performs a no-op.
        /// </summary>
        /// <returns>Awaitable task.</returns>
        Task IBatchedLogEventSink.OnEmptyBatchAsync()
        {
            return Task.Delay(0);
        }

        public void Dispose()
        {
			_periodicBatchingSink.Dispose();
		}
        
    }
}