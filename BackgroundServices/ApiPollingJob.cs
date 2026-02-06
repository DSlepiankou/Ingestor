using Ingestor.Interfaces;
using Ingestor.Utility;
using Quartz;

namespace Ingestor.BackgroundServices
{
    [DisallowConcurrentExecution]
    public class ApiPollingJob : IJob
    {
        private readonly ILogger<ApiPollingJob> _logger;
        private readonly IIngestorService _service;

        public ApiPollingJob(ILogger<ApiPollingJob> logger, IIngestorService service)
        {
            _logger = logger;
            _service = service;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation($"[Quartz] Опрос API запущен в {DateTime.Now:HH:mm:ss}");
            await _service.RetrieveDataAsync();
        }
    }
}
