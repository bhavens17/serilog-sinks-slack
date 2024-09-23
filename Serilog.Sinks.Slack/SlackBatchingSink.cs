

using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using Serilog.Sinks.Slack.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Serilog.Sinks.Slack
{
	public class SlackBatchingSink : IBatchedLogEventSink, IDisposable
	{
		private readonly SlackMessageFormatter _formatter;
		private readonly SlackClient _client;
		private readonly SlackSinkOptions _options;

		public SlackBatchingSink(SlackMessageFormatter formatter, SlackClient client, SlackSinkOptions options)
		{
			_formatter = formatter;
			_client = client;
			_options = options;
		}

		public async Task EmitBatchAsync(IEnumerable<LogEvent> events)
		{
			foreach (var logEvent in events)
			{
				var message = _formatter.CreateMessage(logEvent);
				await _client.SendMessageAsync(_options.WebHookUrl, message);
			}
		}

		public Task OnEmptyBatchAsync()
		{
			return Task.FromResult(0);
		}

		public void Dispose()
		{
			_client.Dispose();
		}
	}
}