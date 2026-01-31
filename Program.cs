using Ingestor.BackgroundServices;
using Ingestor.Components;
using Ingestor.Interfaces;
using Ingestor.Services;
using Ingestor.Utility;
using Microsoft.AspNetCore.Server.IIS;
using Quartz;

namespace Ingestor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //DI
            builder.Services.AddSingleton<PollingSettings>();
            builder.Services.AddTransient<IIngestorService, IngestorService>();
            builder.Services.AddTransient<IIngestorService, IngestorService>();
            builder.Services.AddHttpClient("WeakApi", x =>
            {
                x.DefaultRequestHeaders.Add("X-Api-Key", "supersecret");
                x.BaseAddress = new Uri("http://localhost:5000/");
            });

            builder.Services.AddQuartz(q =>
            {
                var jobKey = new JobKey("ApiPollingJob");
                q.AddJob<ApiPollingJob>(opts => opts.WithIdentity(jobKey));
                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity("ApiPollingTrigger")
                    .WithSimpleSchedule(x => x
                        .WithIntervalInSeconds(10)
                        .RepeatForever()));
            });

            builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

            builder.Services.AddRazorComponents().AddInteractiveServerComponents();

            var app = builder.Build();
            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseAntiforgery();
            app.MapStaticAssets();
            app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
            app.Run();
        }
    }
}
