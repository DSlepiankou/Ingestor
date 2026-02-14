using Ingestor.BackgroundServices;
using Ingestor.Components;
using Ingestor.Interfaces;
using Ingestor.Services;
using Ingestor.Utility;
using MassTransit;
using Microsoft.AspNetCore.Server.IIS;
using Quartz;

namespace Ingestor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSingleton<PollingSettings>();
            builder.Services.AddTransient<IIngestorService, IngestorService>();
            builder.Services.AddTransient<IIngestorService, IngestorService>();
            builder.Services.AddHttpClient("WeakApi", x =>
            {
                var baseAddress = builder.Configuration["ApiSettings:BaseUrl"]
                      ?? "http://localhost:8080";
                x.DefaultRequestHeaders.Add("X-Api-Key", "supersecret");
                x.BaseAddress = new Uri(baseAddress);
            });

            builder.Services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host("rabbitmq", "/", h =>
                    {
                        h.Username("guest");
                        h.Password("guest");
                    });

                    cfg.ConfigureEndpoints(context);
                });
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
