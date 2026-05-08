using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TimeControl.Agent.WinForms.Models;

namespace TimeControl.Agent.WinForms.Services
{
    internal sealed class ApiClient : IDisposable
    {
        private const int BatchSize = 500;
        private const string IngestEndpoint = "/api/ingest";
        private readonly HttpClient _httpClient;
        private AgentSettings _settings;

        public ApiClient()
        {
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        }

        public void Configure(AgentSettings settings)
        {
            _settings = settings;
            _settings.ApiUrl = NormalizeApiBaseUrl(_settings.ApiUrl);
            _httpClient.DefaultRequestHeaders.Clear();
            if (!string.IsNullOrWhiteSpace(settings.Token))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + settings.Token);
            }
        }

        public static string NormalizeApiBaseUrl(string apiUrl)
        {
            var normalized = string.IsNullOrWhiteSpace(apiUrl) ? string.Empty : apiUrl.Trim().TrimEnd('/');
            normalized = StripKnownEndpoint(normalized, IngestEndpoint + "/config");
            normalized = StripKnownEndpoint(normalized, IngestEndpoint + "/last");
            normalized = StripKnownEndpoint(normalized, IngestEndpoint);
            normalized = StripKnownEndpoint(normalized, "/api");
            return normalized.TrimEnd('/');
        }

        public async Task TestAsync()
        {
            EnsureConfigured();
            var response = await _httpClient.GetAsync(ConfigUrl());
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("API retornou " + (int)response.StatusCode + " " + response.ReasonPhrase + ".");
            }
        }

        public async Task<DateTime?> GetLastEventDateAsync()
        {
            EnsureConfigured();
            var response = await _httpClient.GetAsync(LastUrl());
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Token invalido.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("API retornou " + (int)response.StatusCode + " " + response.ReasonPhrase + ".");
            }

            return Json.ReadLastEventDate(await response.Content.ReadAsStringAsync());
        }

        public async Task<int> SendAsync(List<MonitoringEvent> events)
        {
            EnsureConfigured();
            var sent = 0;
            for (var i = 0; i < events.Count; i += BatchSize)
            {
                var batch = events.GetRange(i, Math.Min(BatchSize, events.Count - i));
                var payload = Json.SerializeEvents(ToDtos(batch));
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(IngestUrl(), content);
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("Token invalido.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    throw new InvalidOperationException("API retornou " + (int)response.StatusCode + ": " + body);
                }

                sent += batch.Count;
            }

            return sent;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        private IEnumerable<ActivityEventDto> ToDtos(IEnumerable<MonitoringEvent> events)
        {
            foreach (var item in events)
            {
                yield return new ActivityEventDto
                {
                    EventDateUtc = item.EventDate.ToUniversalTime(),
                    Title = string.IsNullOrWhiteSpace(item.Title) ? "(sem titulo)" : item.Title,
                    Process = string.IsNullOrWhiteSpace(item.Process) ? "unknown" : item.Process,
                    Station = string.IsNullOrWhiteSpace(item.Computer) ? Environment.MachineName : item.Computer,
                    OsUsername = string.IsNullOrWhiteSpace(item.UserName) ? Environment.UserName : item.UserName,
                    PointerX = item.MouseX,
                    PointerY = item.MouseY
                };
            }
        }

        private void EnsureConfigured()
        {
            if (_settings == null || string.IsNullOrWhiteSpace(_settings.ApiUrl) || string.IsNullOrWhiteSpace(_settings.Token))
            {
                throw new InvalidOperationException("Configure o dominio da API e o token.");
            }
        }

        private static string StripKnownEndpoint(string value, string suffix)
        {
            return value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                ? value.Substring(0, value.Length - suffix.Length)
                : value;
        }

        private string IngestUrl()
        {
            return _settings.ApiUrl.TrimEnd('/') + IngestEndpoint;
        }

        private string ConfigUrl()
        {
            return IngestUrl() + "/config";
        }

        private string LastUrl()
        {
            return IngestUrl() + "/last";
        }
    }
}
