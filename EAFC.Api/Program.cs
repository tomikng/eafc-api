using EAFC.Data;
using EAFC.DiscordBot;
using EAFC.Jobs;
using EAFC.Notifications;
using EAFC.Services;
using EAFC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Quartz.Spi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IPlayerService, PlayerService>(); 
builder.Services.AddScoped<PlayerDataCrawler>();

builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("CrawlingJob");
    q.AddJob<CrawlingJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey) 
        .WithIdentity("CrawlingJobTrigger")
        .WithCronSchedule("0 42 17 * * ?"));
});

builder.Services.AddQuartzHostedService(q =>
{
    q.WaitForJobsToComplete = true;
});

builder.Services.AddSingleton<INotificationService, DiscordNotificationService>(provider => new DiscordNotificationService(
    builder.Configuration["DiscordBotToken"] ?? throw new InvalidOperationException(),
    provider));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

var discordService = app.Services.GetRequiredService<INotificationService>() as DiscordNotificationService;
discordService?.InitializeAsync().GetAwaiter().GetResult();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();