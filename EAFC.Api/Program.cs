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

// Add services to the container.
builder.Services.AddScoped<IPlayerService, PlayerService>(); 
builder.Services.AddScoped<PlayerDataCrawler>();

// Quartz configuration
builder.Services.AddQuartz(q =>
{
    // Register your job
    var jobKey = new JobKey("CrawlingJob");
    q.AddJob<CrawlingJob>(opts => opts.WithIdentity(jobKey));

    // Create a trigger for the job
    q.AddTrigger(opts => opts
        .ForJob(jobKey) // Links this trigger to the CrawlingJob
        .WithIdentity("CrawlingJobTrigger") // Give the trigger a unique name
        // .WithCronSchedule("0 5 7 * * ?"));
        .StartNow() // start immediately
        .WithSimpleSchedule(x => x
            .WithIntervalInSeconds(250) // set the interval to 10 seconds
            .RepeatForever())); 
});

// Register Quartz as a hosted service
builder.Services.AddQuartzHostedService(q =>
{
    // When shutting down, wait for jobs to complete
    q.WaitForJobsToComplete = true;
});

// Configure the rest of your services
builder.Services.AddSingleton<INotificationService, DiscordNotificationService>(provider => new DiscordNotificationService(
    builder.Configuration["DiscordBotToken"] ?? throw new InvalidOperationException(),
    provider));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Initialize Discord bot
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