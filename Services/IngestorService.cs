using Ingestor.Interfaces;
using Ingestor.Models;
using Polly;
using Polly.Retry;
using Quartz.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Ingestor.Services
{
    public class IngestorService : IIngestorService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<IngestorService> _logger;

        public IngestorService(IHttpClientFactory httpClientFactory, ILogger<IngestorService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }
        public async Task RetrieveDataAsync()
        {
            try
            {
                var response = await FetchDataWithRetryAsync();
                if (response != null)
                {
                    var messages = JsonSerializer.Deserialize<List<MeterData>>(response,
                     new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                

            }
            catch (Exception ex)
            {
            }
        }

        private async Task<string> FetchDataWithRetryAsync() // Возвращаем string
        {
            try
            {
                var client = _httpClientFactory.CreateClient("WeakApi");

                var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
                    .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                    {
                        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                            .Handle<HttpRequestException>()
                            .Handle<InvalidOperationException>()
                            .HandleResult(r => (int)r.StatusCode >= 400),
                        MaxRetryAttempts = 3,
                        Delay = TimeSpan.FromSeconds(2),
                        BackoffType = DelayBackoffType.Exponential
                    })
                    .Build();

                var response = await pipeline.ExecuteAsync(async token =>
                {
                    var res = await client.GetAsync("meters", token);

                    if (res.IsSuccessStatusCode)
                    {
                        var content = await res.Content.ReadAsStringAsync(token);
                        if (string.IsNullOrWhiteSpace(content) || content == "[]")
                        {
                            throw new InvalidOperationException("Empty data");
                        }
                        return res;
                    }
                    return res;
                });

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Weak api really weak: {ex.Message}");
                return null;
            }
        }
    }
}
