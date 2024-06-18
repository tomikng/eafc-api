using EAFC.Data;
using EAFC.DiscordBot;
using EAFC.Jobs;
using EAFC.Notifications;
using EAFC.Services;
using EAFC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Quartz;
using System;
using System.IO;
using EAFC.Notifications.interfaces;

var builder = WebApplication.CreateBuilder(args);

var configFilePath = Path.Combine(AppContext.BaseDirectory, "config.json");
if (configFilePath.Contains("EAFC.Api"))
{
    configFilePath = configFilePath.Replace("EAFC.Api", "EAFC.Configurator");
}

builder.Configuration.AddJsonFile(configFilePath, optional: false, reloadOnChange: true);

builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<PlayerDataCrawler>();

builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("CrawlingJob");
    q.AddJob<CrawlingJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("CrawlingJobTrigger")
        .WithCronSchedule(builder.Configuration["CronExpression"] ?? throw new InvalidDataException()));
});

builder.Services.AddQuartzHostedService(q =>
{
    q.WaitForJobsToComplete = true;
});

builder.Services.AddSingleton<INotificationServiceFactory, NotificationServiceFactory>();
builder.Services.AddSingleton<INotificationService>(provider =>
{
    var factory = provider.GetRequiredService<INotificationServiceFactory>();
    var configuration = provider.GetRequiredService<IConfiguration>();
    return factory.CreateNotificationService(provider, configuration) ?? throw new InvalidOperationException();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

var notificationService = app.Services.GetRequiredService<INotificationService>();
(notificationService as IInitializable)?.InitializeAsync().GetAwaiter().GetResult();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
