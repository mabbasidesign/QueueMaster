using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationFunction.Notifications;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services
    .AddOptions<NotificationEmailOptions>()
    .Bind(builder.Configuration.GetSection(NotificationEmailOptions.SectionName));

builder.Services.AddSingleton<INotificationSender, AcsEmailNotificationSender>();

builder.Build().Run();
