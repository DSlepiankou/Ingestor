using Ingestor.Interfaces;
using Ingestor.Models;
using MassTransit;
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
        private readonly IPublishEndpoint _publishEndpoint;

        public IngestorService(IHttpClientFactory httpClientFactory,
            ILogger<IngestorService> logger,
            IPublishEndpoint publishEndpoint)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _publishEndpoint = publishEndpoint;
        }

        public async Task RetrieveDataAsync()
        {
            var response = await FetchDataWithRetryAsync();
            try
            {
                if (!string.IsNullOrEmpty(response))
                {
                    await _publishEndpoint.Publish(new MeterDataMessage(
                        JsonPayload: response,
                        CapturedAt: DateTime.UtcNow
                    ));

                    Console.WriteLine("Данные успешно отправлены в RabbitMQ");
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
                    throw new InvalidOperationException("Not success");

                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"kaput: {ex.Message}");
                throw new InvalidOperationException("Api pizdec");
            }
        }
    }
}
