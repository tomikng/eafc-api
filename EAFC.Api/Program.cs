using EAFC.Data;
using EAFC.DiscordBot;
using EAFC.Notifications;
using EAFC.Services;
using EAFC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IPlayerService, PlayerService>(); 

builder.Services.AddScoped<PlayerDataCrawler>();
builder.Services.AddSingleton<INotificationService, DiscordNotificationService>(provider =>
{
    using var scope = provider.CreateScope();
    return new DiscordNotificationService(
        builder.Configuration["DiscordBotToken"] ?? throw new InvalidOperationException(),
        provider);
});

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
