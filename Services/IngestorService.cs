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
            var response = await FetchDataWithRetryAsync();
            try
            {
                if (response != null)
                {
                    var messages = JsonSerializer.Deserialize<List<MeterData>>(response,
                     new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"pupupu: {ex.Message} + {response}");
            }
        }

        private async Task<string> FetchDataWithRetryAsync()
        {
            var client = _httpClientFactory.CreateClient("WeakApi");

            var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
                .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<InvalidOperationException>()
                        .HandleResult(r => (int)r.StatusCode >= 500),

                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(2),
                    BackoffType = DelayBackoffType.Exponential,
                    OnRetry = args =>
                    {
                        _logger.LogWarning($"Attempt {args.AttemptNumber}. Error: {args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString()}");
                        return default;
                    }
                })
                .Build();

            try
            {
                var response = await pipeline.ExecuteAsync(async ct =>
                {
                    var res = await client.GetAsync("meters", ct);

                    if (res.IsSuccessStatusCode)
                    {
                        var content = await res.Content.ReadAsStringAsync(ct);
                        if (string.IsNullOrWhiteSpace(content) || content == "[]")
                        {
                            throw new InvalidOperationException("Empty data");
                        }
                    }
                    return res;
                });

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"After all attempts, api returned: {response.StatusCode}");
                    return null;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"kaput: {ex.Message}");
                return null;
            }
        }
    }
}
