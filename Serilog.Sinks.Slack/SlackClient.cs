using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using Serilog.Sinks.Slack.Models;

namespace Serilog.Sinks.Slack
{
	public class SlackClient : IDisposable
	{
		private readonly HttpClient _httpClient;

		public SlackClient()
		{
			_httpClient = new HttpClient();
		}

		public async Task SendMessageAsync(string webhookUrl, Message message)
		{
			var json = JsonConvert.SerializeObject(message);
			await _httpClient.PostAsync(webhookUrl, new StringContent(json));
		}

		public void Dispose()
		{
			_httpClient.Dispose();
		}
	}
}