# EAFC Notification Documentation
Programming in C# - MFF UK - ZS 2023/2024

Hai Hung Nguyen
## Overview
This project is a combination of simple API, notification sender and also a simple EAFC player scraper. The goal of
this project is to provide for players of EAFC a way to get notified when a new player is added into the game.
The following sections will describe the API, the notification sender and the scraper in more detail. Also, this
documentation will provide a guide on how to use the API and the notification sender.

## Installation and user guide

### Pre-requisites
To install the project, you need to have the following tools installed on your machine:
- .NET 8.0 SDK
- Database server (PostgreSQL)

### Configuration
First we need to set up configuration in configuration UI.

To run configuration UI, you can run the following command:
```shell
dotnet run --project EAFC.Notification.Configuration
```

For the first time you need to fill the values in the configuration UI.

#### In general
Please configure the fields shown in configuration:
- Platforms: The platforms that you want to scrape the players from.
  - Currently, the project supports these platforms for sending notifications:
    - Discord
  - For adding a new platform it will be described in the `Programmer documentation section.` 
- Fill the `cron` value
  - The cron value is the time that the scraper will run to scrape the players from the platforms.
  - The recommended value is `0 5 19 * * ?` which means the scraper will run at 19:05 every day.
  - You can change the value to your desired time.
#### Discord
Please configure the fields shown in configuration:
- `discordGuildId`: The ID of the Discord guild that you want to send the notifications to.
- `discordChannelId`: The ID of the Discord channel that you want to send the notifications to.



Do not forget to **save** the configuration after you have done the changes.

### Server setup
After you have installed the tools, you can follow the steps below to install the project:
1. Open the project in your favorite IDE (Visual Studio, Rider, etc.)
2. Open the `appsettings.template.json` file in the `EAFC.API` project and change the connection string to your
   database server, and also change the `DiscordToken` to your Discord bot token.
3. Rename the `appsettings.template.json` file to `appsettings.json`
4. Run the following commands in the terminal to create the database and the tables:
   ```shell
   cd EAFC.Notification.API
   dotnet ef database update
   ```
5. Run server by running the following command:
   ```shell
   dotnet run --project EAFC.Notification.API
   ```

## Programmer documentation
In this section, I will describe the structure of the project and the main classes and methods that are used in the
project.

### Structure
The solution is divided into 9 projects:
- `EAFC.API`: The main project that contains the API for the project.
- `EAFC.Configuration`: The project that contains the configuration UI for the project.
- `EAFC.Core`: The project that contains the core logic of the project, mainly definitions of models.
- `EAFC.Data`: The project that contains the data access layer of the project.
- `EAFC.DiscordBot`: The project that contains the Discord bot for the project, which implements the notification sender.
- `EAFC.Notification`: The project that contains the common logic and factory logic for notifications.
- `EAFC.Services`: The project that contains the services of the project, mainly scraper logic and Player service logic
for retrieving and saving players.
- `EAFC.Tests`: The project that contains the tests for the project.

### EAFC.API
This project contains the API for the project. The main items that are in this projects are:
- `Controllers`: The folder that contains the controllers for the API.
- `Program.cs`: The main class that configures the services and the middleware for the API.
- `appsettings.json`: The configuration file for the API.

Except for `Program.cs`, the rest of the main files are self-explanatory.

### Program.cs
This file is designed to handle various functionalities related to the EAFC system, including **data crawling**, 
**player services**, and **notifications** through a factory builder. This documentation describes the initialization and configuration of the application services and middleware.
#### Configuration and initialization
Initialization and configuration is seperated into several steps bellow:
1. **Create a new `WebApplicationBuilder` instance**
   ```csharp
   var builder = WebApplication.CreateBuilder(args);
   ```
   The `WebApplication.CreateBuilder(args)` method creates a new `WebApplicationBuilder` instance that is used to configure the services and the middleware for the application.

2. **Add the configuration file to the builder**
   ```csharp
   var configFilePath = Path.Combine(AppContext.BaseDirectory, "config.json");
   if (configFilePath.Contains("EAFC.Api"))
   {
       configFilePath = configFilePath.Replace("EAFC.Api", "EAFC.Configurator");
   }
   
   builder.Configuration.AddJsonFile(configFilePath, optional: false, reloadOnChange: true);
   ```
    The `AddJsonFile` method adds the configuration file to the builder. The configuration file is used to configure the
services and the middleware for the application.
3. **Dependency injection**
   ```csharp
   builder.Services.AddScoped<IPlayerService, PlayerService>();
   builder.Services.AddScoped<PlayerDataCrawler>();
   ```
   Registers PlayerService and PlayerDataCrawler as scoped services, meaning a new instance is created for each request.

4. **Quartz Job Scheduling**
   ```csharp
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
   ```
   Configures Quartz for job scheduling. It sets up a job (CrawlingJob) with a trigger that uses a cron expression from
the configuration file. It also ensures that the hosted service waits for jobs to complete before shutting down.
5. **Notification Service Factory and Initialization**
    ```csharp
    builder.Services.AddSingleton<INotificationServiceFactory, NotificationServiceFactory>();
   builder.Services.AddSingleton<INotificationService>(provider =>
   {
   var factory = provider.GetRequiredService<INotificationServiceFactory>();
   var configuration = provider.GetRequiredService<IConfiguration>();
   return factory.CreateNotificationService(provider, configuration) ?? throw new InvalidOperationException();
   });
    ```
   Registers the NotificationServiceFactory as a singleton and uses it to create an instance of INotificationService. 
This allows for more flexible creation and configuration of notification services.
6. Database
    ```csharp
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
    ```
    Configures the database context to use a PostgreSQL connection string from the configuration file.

### EAFC.Configuration
This project contains the configuration UI for the project. The main parts of the projects are:
- `Models`
- `ViewModels`
- `Views`

Since the goal of this project is also to have modular way to have sent notifications to different platforms, we would
also want to have the ability to easily incorporate a new platform setting into the project if needed.

But firstly let's briefly discuss the current implementation of the configuration UI.

The main classes are:
- `DiscordSetting`
- `PlatformSettingsFactory`
- `MainWindowsViewModel`

#### DiscordSetting
[This class](../EAFC.Configurator/Models/Settings/DiscordSettings.cs) is used to store the Discord settings for the project. It implements the [`IPlatformSettings`](../EAFC.Configurator/Models/Settings/IPlatformSettings.cs) interface.
```csharp
public interface IPlatformSettings
{
    string PlatformName { get; }
    void SaveSettings(Utf8JsonWriter jsonWriter);
    void LoadSettings(JsonElement settingsElement);
}
```

#### PlatformSettingsFactory
The PlatformSettingsFactory class is responsible for managing and providing instances of platform-specific settings. 
It acts as a registry and factory for IPlatformSettings implementations, allowing for the dynamic retrieval and 
management of various platform settings.

It maintains a dictionary of registered platform settings, indexed by their platform names.

##### Constructor

- **PlatformSettingsFactory()**
    - Initializes a new instance of the `PlatformSettingsFactory` class.
    - Registers the initial set of platform settings (e.g., `DiscordSettings`).

##### Methods

- `void RegisterPlatformSettings(IPlatformSettings platformSettings)`
    - Registers a new platform settings instance.
    - **Parameters:**
        - `platformSettings`: An instance of a class implementing the `IPlatformSettings` interface.
    - **Example:**
      ```csharp
      var factory = new PlatformSettingsFactory();
      factory.RegisterPlatformSettings(new DiscordSettings());
      ```

- `IPlatformSettings? GetPlatformSettings(string platformName)`
    - Retrieves a platform settings instance by its platform name.
    - **Parameters:**
        - `platformName`: The name of the platform whose settings are to be retrieved.
    - **Returns:**
        - An instance of a class implementing `IPlatformSettings`, or `null` if no settings are registered for the specified platform name.
    - **Example:**
      ```csharp
      var discordSettings = factory.GetPlatformSettings("Discord");
      ```

- `IEnumerable<IPlatformSettings> GetAllPlatformSettings()`
    - Retrieves all registered platform settings instances.
    - **Returns:**
        - An enumerable collection of instances implementing the `IPlatformSettings` interface.
    - **Example:**
      ```csharp
      var allSettings = factory.GetAllPlatformSettings();
      ```

##### Usage Example

Here’s an example of how to use the `PlatformSettingsFactory` class:

```csharp
var factory = new PlatformSettingsFactory();
factory.RegisterPlatformSettings(new DiscordSettings());
// Register other platform settings as needed

// Retrieve a specific platform's settings
var discordSettings = factory.GetPlatformSettings("Discord");
if (discordSettings != null)
{
    // Use the discordSettings instance
}

// Retrieve all platform settings
foreach (var settings in factory.GetAllPlatformSettings())
{
    // Use each settings instance
}
```


#### MainWindowsViewModel
The `MainWindowViewModel` class utilizes the `ReactiveObject` base class from the ReactiveUI framework to provide reactive properties and commands. It manages platform-specific settings using a factory pattern and provides commands for saving and loading the configuration.

##### Properties

- **`ObservableCollection<IPlatformSettings> PlatformSettings`**
    - A collection of platform-specific settings.
    - **Example:**
      ```csharp
      var platformSettings = viewModel.PlatformSettings;
      ```

- **`bool EnableNotifications`**
    - Indicates whether notifications are enabled.
    - **Example:**
      ```csharp
      bool notificationsEnabled = viewModel.EnableNotifications;
      viewModel.EnableNotifications = true;
      ```

- **`string AllowedPlatforms`**
    - A comma-separated list of allowed platforms.
    - **Example:**
      ```csharp
      string platforms = viewModel.AllowedPlatforms;
      viewModel.AllowedPlatforms = "Discord,Slack";
      ```

- **`string? CronExpression`**
    - A CRON expression representing the schedule for notifications.
    - **Example:**
      ```csharp
      string cron = viewModel.CronExpression;
      viewModel.CronExpression = "0 0 * * *";
      ```

- **`ICommand SaveCommand`**
    - A command that saves the current configuration to a JSON file.
    - **Example:**
      ```csharp
      viewModel.SaveCommand.Execute(null);
      ```

#### Constructor

- **`MainWindowViewModel()`**
    - Initializes a new instance of the `MainWindowViewModel` class.
    - Sets up platform settings and binds commands.

#### Methods

- **`void SaveConfiguration()`**
    - Saves the current configuration to a JSON file located at `_configFilePath`.
    - Uses the `Utf8JsonWriter` to serialize each platform's settings.
    - **Example:**
      ```csharp
      viewModel.SaveConfiguration();
      ```

- **`void LoadConfiguration()`**
    - Loads the configuration from a JSON file located at `_configFilePath`.
    - Reads and deserializes the JSON content to initialize the settings.
    - **Example:**
      ```csharp
      viewModel.LoadConfiguration();
      ```


#### How to add a new platform setting
To add a new platform setting, you need to follow these steps:
1. Create a new class that implements the `IPlatformSettings` interface.
2. Add the new class to the `PlatformSettingsFactory` using the `RegisterPlatformSettings` method.
3. Update the `MainWindowViewModel` class to handle the new platform setting.
4. Update the configuration UI in `MainWindow.axaml` to display and manage the new platform setting.

### EAFC.Core
This project defines the core models and interfaces used throughout the project. The main classes and interfaces are:
- `Player`
- `Pagination`: The Pagination<T> class is a generic utility for managing paginated data. It provides properties to 
access the current page, page size, total count, and total pages. The class also includes a method to check if any 
items on the current page satisfy a specified condition. This utility class simplifies the handling of paginated data
in applications.

#### Player
The `Player` class represents a player in the EAFC system. It includes basic details about the player such as their ID, 
name, rating, position, profile URL, and the date they were added.

#### Attributes:
- **Key Attribute:** The `[Key]` attribute is used to denote the primary key of the entity.

#### Properties

- **int Id**
    - The unique identifier for the player.
    - **Data Annotation:** `[Key]` marks this property as the primary key.
    - **Example:**
      ```csharp
      player.Id = 1;
      ```

- **string Name**
    - The name of the player.
    - **Example:**
      ```csharp
      player.Name = "Lionel Messi";
      ```

- **DateTime AddedOn**
    - The date and time when the player was added to the system.
    - **Example:**
      ```csharp
      player.AddedOn = DateTime.UtcNow;
      ```

- **int Rating**
    - The rating of the player.
    - **Example:**
      ```csharp
      player.Rating = 90;
      ```

- **string? Position**
    - The position of the player on the field.
    - **Nullable:** This property is nullable.
    - **Example:**
      ```csharp
      player.Position = "Forward";
      ```

- **string ProfileUrl**
    - The URL to the player's profile.
    - **Example:**
      ```csharp
      player.ProfileUrl = "http://example.com/players/messi";
      ```

#### Example Usage

Here’s an example of how to create and use a `Player` object:

```csharp
using EAFC.Core.Models;
using System;

public class Example
{
    public void CreatePlayer()
    {
        Player player = new Player
        {
            Id = 1,
            Name = "Lionel Messi",
            AddedOn = DateTime.UtcNow,
            Rating = 90,
            Position = "Forward",
            ProfileUrl = "http://example.com/players/messi"
        };

        // Access player properties
        Console.WriteLine($"Player Name: {player.Name}");
        Console.WriteLine($"Position: {player.Position}");
        Console.WriteLine($"Rating: {player.Rating}");
        Console.WriteLine($"Profile URL: {player.ProfileUrl}");
        Console.WriteLine($"Added On: {player.AddedOn}");
    }
}
```

### EAFC.Data
The `ApplicationDbContext` class is the primary class that coordinates Entity Framework functionality for a given data model. It is derived from the `DbContext` class and provides properties to query and save instances of the `Player` entity.


#### Inheritance:
Inherits from `Microsoft.EntityFrameworkCore.DbContext`.

#### Properties

- **DbSet<Player> Players**
    - A `DbSet` representing the collection of `Player` entities in the context. This property allows querying and saving instances of the `Player` class.
    - **Example:**
      ```csharp
      var players = context.Players.ToList();
      ```

#### Constructor

- **ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)**
    - Initializes a new instance of the `ApplicationDbContext` class.
    - **Parameters:**
        - `options`: The options to be used by the `DbContext`. These options are typically configured in the `Startup` class using dependency injection.
    - **Example:**
      ```csharp
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                      .UseSqlServer(connectionString)
                      .Options;
      var context = new ApplicationDbContext(options);
      ```

#### Example Usage

Here’s an example of how to configure and use the `ApplicationDbContext` class:

##### Configuration in `Startup.cs` (or `Program.cs` in newer .NET versions):

```csharp
using EAFC.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Configure Entity Framework with SQL Server
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

        // Other service configurations
    }
}
```

##### Using the `ApplicationDbContext` to Query Players:

```csharp
using EAFC.Core.Models;
using EAFC.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

public class Example
{
    private readonly ApplicationDbContext _context;

    public Example(ApplicationDbContext context)
    {
        _context = context;
    }

    public void QueryPlayers()
    {
        // Query all players
        var players = _context.Players.ToList();
        
        // Display player details
        foreach (var player in players)
        {
            Console.WriteLine($"Player ID: {player.Id}, Name: {player.Name}, Rating: {player.Rating}");
        }
    }
}
```

##### Example of Dependency Injection in a Console Application:

```csharp
using EAFC.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

public class Program
{
    public static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        // Use the context
        using (var scope = host.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var example = new Example(context);
            example.QueryPlayers();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer("YourConnectionStringHere"));
                
                // Register other services
            });
}
```

### EAFC.DiscordBot
In this project we have `DiscordNotificationService`. The `DiscordNotificationService` class provides functionality to send notifications to a Discord channel. It integrates with the Discord API using the Discord.NET library, handles slash commands, and formats player data for notifications.


#### Interfaces Implemented:
- `INotificationService`
- `IInitializable`

#### Dependencies:
- `DiscordSocketClient`: A Discord client from the Discord.NET library.
- `IServiceProvider`: A service provider for dependency injection.
- `IConfiguration`: An interface for accessing configuration settings.

#### Properties

- **DiscordSocketClient _client**
    - The Discord client used to interact with the Discord API.

- **string _token**
    - The token for authenticating the Discord bot.

- **IServiceProvider _serviceProvider**
    - The service provider for resolving dependencies.

- **ulong _guildId**
    - The ID of the Discord guild (server) where the bot operates.

- **ulong _channelId**
    - The ID of the Discord channel where notifications are sent.

#### Constructor

- **DiscordNotificationService(IServiceProvider serviceProvider, IConfiguration configuration)**
    - Initializes a new instance of the `DiscordNotificationService` class.
    - **Parameters:**
        - `serviceProvider`: The service provider for dependency injection.
        - `configuration`: The configuration settings for the application.
    - **Example:**
      ```csharp
      var serviceProvider = new ServiceCollection().BuildServiceProvider();
      var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
      var discordService = new DiscordNotificationService(serviceProvider, configuration);
      ```

#### Methods

- **Task InitializeAsync()**
    - Initializes the Discord client, logs in, and starts the client.
    - Registers slash commands once the client is ready.
    - **Example:**
      ```csharp
      await discordService.InitializeAsync();
      ```

- **Task RegisterCommandsAsync()**
    - Registers slash commands with the Discord guild.
    - **Example:**
      ```csharp
      await discordService.RegisterCommandsAsync();
      ```

- **Task LogAsync(LogMessage log)**
    - Logs messages from the Discord client.
    - **Example:**
      ```csharp
      await discordService.LogAsync(new LogMessage(LogSeverity.Info, "Source", "Message"));
      ```

- **Task SlashCommandHandler(SocketSlashCommand command)**
    - Handles slash commands executed by users.
    - **Example:**
      ```csharp
      await discordService.SlashCommandHandler(command);
      ```

- **void AppendPlayerDetails(StringBuilder builder, Player player)**
    - Appends player details to a `StringBuilder`.
    - **Example:**
      ```csharp
      var builder = new StringBuilder();
      discordService.AppendPlayerDetails(builder, player);
      ```

- **string? FormatPlayers(List<Player> players)**
    - Formats a list of players into a string.
    - **Example:**
      ```csharp
      string formattedPlayers = discordService.FormatPlayers(players);
      ```

- **string? FormatPlayer(Player player)**
    - Formats a single player's details into a string.
    - **Example:**
      ```csharp
      string formattedPlayer = discordService.FormatPlayer(player);
      ```

- **Task SendNotificationAsync(List<Player> players)**
    - Sends a notification with a list of players.
    - **Example:**
      ```csharp
      await discordService.SendNotificationAsync(players);
      ```

- **Task SendNotificationAsync(Player player)**
    - Sends a notification with a single player.
    - **Example:**
      ```csharp
      await discordService.SendNotificationAsync(player);
      ```

- **Task SendErrorNotificationAsync(string errorMessage)**
    - Sends an error notification.
    - **Example:**
      ```csharp
      await discordService.SendErrorNotificationAsync("An error occurred.");
      ```

- **Task SendWarningNotificationAsync(string warningMessage)**
    - Sends a warning notification.
    - **Example:**
      ```csharp
      await discordService.SendWarningNotificationAsync("This is a warning.");
      ```

- **Task SendInfoNotificationAsync(string infoMessage)**
    - Sends an informational notification.
    - **Example:**
      ```csharp
      await discordService.SendInfoNotificationAsync("This is an informational message.");
      ```

- **Task SendCustomNotificationAsync(string customMessage)**
    - Sends a custom notification.
    - **Example:**
      ```csharp
      await discordService.SendCustomNotificationAsync("Custom message.");
      ```

- **Task SendNotificationAsync(string message)**
    - Sends a notification with a custom message.
    - **Example:**
      ```csharp
      await discordService.SendNotificationAsync("Custom message.");
      ```

- **Task SendMessageAsync(string? message)**
    - Sends a message to the configured Discord channel.
    - **Example:**
      ```csharp
      await discordService.SendMessageAsync("Hello, Discord!");
      ```

#### Example Usage

Here’s an example of how to create and use the `DiscordNotificationService` class:

```csharp
using EAFC.DiscordBot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

public class Example
{
    public async Task Run()
    {
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var discordService = new DiscordNotificationService(serviceProvider, configuration);

        await discordService.InitializeAsync();

        // Send a notification
        var players = new List<Player>
        {
            new Player { Id = 1, Name = "Lionel Messi", Rating = 93 },
            new Player { Id = 2, Name = "Cristiano Ronaldo", Rating = 92 }
        };
        await discordService.SendNotificationAsync(players);
    }
}
```

### EAFC.Jobs

This project is for the scheduled job that fetches newly added players and sends notifications about the fetched players. The main class in this project is the `CrawlingJob` class.
#### CrawlingJob Class

The `CrawlingJob` class is a scheduled job that uses Quartz.NET for task scheduling. It fetches newly added players using a data crawler and sends notifications about the fetched players using a notification service.

#### Inheritance:
- Implements `Quartz.IJob` interface.

#### Dependencies:
- `IPlayerDataCrawler`: Interface for fetching newly added players.
- `INotificationService`: Interface for sending notifications.

#### Constructor

- **CrawlingJob(IPlayerDataCrawler crawler, INotificationService notificationService)**
    - Initializes a new instance of the `CrawlingJob` class.
    - **Parameters:**
        - `crawler`: The data crawler used to fetch newly added players.
        - `notificationService`: The notification service used to send notifications.
    - **Example:**
      ```csharp
      var crawler = new PlayerDataCrawler();
      var notificationService = new DiscordNotificationService();
      var job = new CrawlingJob(crawler, notificationService);
      ```

#### Methods

- **Task Execute(IJobExecutionContext context)**
    - Executes the crawling job.
    - Fetches newly added players and sends notifications based on the results.
    - **Parameters:**
        - `context`: The job execution context provided by Quartz.NET.
    - **Example:**
      ```csharp
      await job.Execute(context);
      ```

#### Example Usage

Here’s an example of how to create and use the `CrawlingJob` class:

```csharp
using EAFC.Jobs;
using EAFC.Notifications;
using EAFC.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using System.Threading.Tasks;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Set up DI
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IPlayerDataCrawler, PlayerDataCrawler>()
            .AddSingleton<INotificationService, DiscordNotificationService>()
            .BuildServiceProvider();

        var crawler = serviceProvider.GetRequiredService<IPlayerDataCrawler>();
        var notificationService = serviceProvider.GetRequiredService<INotificationService>();
        var job = new CrawlingJob(crawler, notificationService);

        // Set up Quartz job scheduler
        var schedulerFactory = new StdSchedulerFactory();
        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.Start();

        var jobDetail = JobBuilder.Create<CrawlingJob>()
            .WithIdentity("crawlingJob", "group1")
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("trigger1", "group1")
            .StartNow()
            .WithSimpleSchedule(x => x
                .WithIntervalInSeconds(60)
                .RepeatForever())
            .Build();

        await scheduler.ScheduleJob(jobDetail, trigger);
    }
}
```

### EAFC.Notifications
This project contains the common logic and factory logic for notifications. The main classes and interfaces are:
- `INotificationService`
- `INotificationServiceFactory`
- `IInitializable`
- `NotificationFactory`

Since the interfaces are self-explanatory, I will focus on the `NotificationFactory` class.

### NotificationServiceFactory Class

The `NotificationServiceFactory` class is responsible for creating instances of notification services. It reads the configuration settings to determine which notification services are allowed and dynamically loads the appropriate service based on the allowed platforms.

#### Interfaces Implemented:
- `INotificationServiceFactory`

#### Methods

- **INotificationService CreateNotificationService(IServiceProvider serviceProvider, IConfiguration configuration)**
    - Creates an instance of an `INotificationService` based on the configuration settings.
    - **Parameters:**
        - `serviceProvider`: The service provider for dependency injection.
        - `configuration`: The configuration settings for the application.
    - **Returns:** An instance of `INotificationService`.
    - **Throws:** `InvalidOperationException` if notifications are disabled, no allowed platforms are specified, or no valid notification service is found.
    - **Example:**
      ```csharp
      var factory = new NotificationServiceFactory();
      var notificationService = factory.CreateNotificationService(serviceProvider, configuration);
      ```
    - Note that in order to the factory to find the correct notification service, it has to be in this format 
  `EAFC.{platform}Bot.{platform}NotificationService` 
#### Example Usage

Here’s an example of how to use the `NotificationServiceFactory` class:

##### Example Configuration (`config.json`, which can be set in configuration UI):

```json
{
  "EnableNotifications": "true",
  "AllowedPlatforms": [
    "Discord",
    "Slack"
  ]
}
```

##### Using the Factory:

```csharp
using EAFC.Notifications;
using EAFC.Notifications.interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

public class Program
{
    public static void Main(string[] args)
    {
        // Set up configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        // Set up DI
        var serviceProvider = new ServiceCollection()
            .BuildServiceProvider();

        // Create the notification service factory
        var factory = new NotificationServiceFactory();
        INotificationService notificationService = null;

        try
        {
            notificationService = factory.CreateNotificationService(serviceProvider, configuration);
            Console.WriteLine("Notification service created successfully.");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        // Use the notification service
        if (notificationService != null)
        {
            // Example usage of the notification service
        }
    }
}
```

### EAFC.Services

This project contains the services of the project, mainly the scraper logic and player service logic for retrieving and saving players.

#### PlayerDataCrawler Class

The `PlayerDataCrawler` class uses HtmlAgilityPack to fetch and parse player data from a specified URL. It recursively fetches data from multiple pages and extracts player details to identify newly added players.

#### Interfaces Implemented:
- `IPlayerDataCrawler`

#### Dependencies:
- `IConfiguration`: Interface for accessing configuration settings.
- `IPlayerService`: Interface for player-related services.

#### Properties

- **HtmlWeb _web**
    - An instance of `HtmlWeb` for fetching web pages.

- **string _dataUrl**
    - The URL for fetching player data, configured through `IConfiguration`.

- **IPlayerService playerService**
    - The player service for interacting with player data in the database.

#### Constructor

- **PlayerDataCrawler(IConfiguration configuration, IPlayerService playerService)**
    - Initializes a new instance of the `PlayerDataCrawler` class.
    - **Parameters:**
        - `configuration`: The configuration settings for the application.
        - `playerService`: The service for managing player data.
    - **Example:**
      ```csharp
      var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
      var playerService = new PlayerService();
      var crawler = new PlayerDataCrawler(configuration, playerService);
      ```

#### Methods

- **Task FetchNewlyAddedPlayersAsync()**
    - Fetches newly added players from the data source.
    - **Returns:** A `Task` that represents the asynchronous operation, containing a list of newly added `Player` objects.
    - **Throws:** `InvalidDataException` if the data URL is not configured.
    - **Example:**
      ```csharp
      var newPlayers = await crawler.FetchNewlyAddedPlayersAsync();
      ```

- **Task FetchPlayersRecursively(string url, List<Player> allPlayers)**
    - Recursively fetches player data from multiple pages.
    - **Parameters:**
        - `url`: The URL of the current page to fetch.
        - `allPlayers`: A list to accumulate all fetched players.
    - **Example:**
      ```csharp
      await crawler.FetchPlayersRecursively("http://example.com/players", allPlayers);
      ```

- **List<Player> ExtractPlayersFromPage(HtmlDocument doc)**
    - Extracts player details from an HTML document.
    - **Parameters:**
        - `doc`: The HTML document to parse.
    - **Returns:** A list of `Player` objects extracted from the page.
    - **Example:**
      ```csharp
      var players = crawler.ExtractPlayersFromPage(htmlDocument);
      ```

#### Example Usage

Here’s an example of how to configure and use the `PlayerDataCrawler` class:

##### Example Configuration (`appsettings.json`):

```json
{
  "CrawlerSettings": {
    "PlayerDataURL": "http://example.com/players"
  },
  "BaseUrl": "http://example.com"
}
```

##### Using the Crawler:

```csharp
using EAFC.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

public class Program
{
    public static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<IPlayerService, PlayerService>()
            .AddSingleton<IPlayerDataCrawler, PlayerDataCrawler>()
            .BuildServiceProvider();

        var crawler = serviceProvider.GetRequiredService<IPlayerDataCrawler>();

        var newPlayers = await crawler.FetchNewlyAddedPlayersAsync();
        foreach (var player in newPlayers)
        {
            Console.WriteLine($"New Player: {player.Name}, Added On: {player.AddedOn}");
        }
    }
}
```

#### PlayerService Class

The `PlayerService` class is responsible for managing player data in the EAFC application. It interacts with the database context to perform CRUD operations on player entities.


#### Interfaces Implemented:
- `IPlayerService`

#### Dependencies:
- `ApplicationDbContext`: The Entity Framework Core database context for the application.

#### Constructor

- **PlayerService(ApplicationDbContext context)**
    - Initializes a new instance of the `PlayerService` class.
    - **Parameters:**
        - `context`: The `ApplicationDbContext` instance for interacting with the database.
    - **Example:**
      ```csharp
      var context = new ApplicationDbContext(options);
      var playerService = new PlayerService(context);
      ```

#### Methods

- **Task<DateTime?> GetLatestAddedOnDateAsync()**
    - Retrieves the most recent `AddedOn` date of all players in the database.
    - **Returns:** A `Task` representing the asynchronous operation, containing the most recent `AddedOn` date, or `null` if no players are found.
    - **Example:**
      ```csharp
      var latestDate = await playerService.GetLatestAddedOnDateAsync();
      ```

- **Task<Pagination<Player>> GetLatestPlayersAsync(int page = 1, int pageSize = 100)**
    - Retrieves a paginated list of the latest players, ordered by `AddedOn` date.
    - **Parameters:**
        - `page`: The page number to retrieve (default is 1).
        - `pageSize`: The number of items per page (default is 100).
    - **Returns:** A `Task` representing the asynchronous operation, containing a `Pagination<Player>` object with the latest players.
    - **Example:**
      ```csharp
      var latestPlayers = await playerService.GetLatestPlayersAsync();
      ```

- **Task<Pagination<Player>> GetLatestPlayersByLatestAddOnAsync(int page = 1, int pageSize = 100)**
    - Retrieves a paginated list of players who have the most recent `AddedOn` date.
    - **Parameters:**
        - `page`: The page number to retrieve (default is 1).
        - `pageSize`: The number of items per page (default is 100).
    - **Returns:** A `Task` representing the asynchronous operation, containing a `Pagination<Player>` object with the latest players by the most recent `AddedOn` date.
    - **Example:**
      ```csharp
      var latestPlayersByDate = await playerService.GetLatestPlayersByLatestAddOnAsync();
      ```

- **Task AddPlayersAsync(IEnumerable<Player> players)**
    - Adds a collection of new players to the database.
    - **Parameters:**
        - `players`: The collection of `Player` objects to add.
    - **Example:**
      ```csharp
      var newPlayers = new List<Player>
      {
          new Player { Name = "Lionel Messi", AddedOn = DateTime.UtcNow, Rating = 93, Position = "Forward", ProfileUrl = "http://example.com/messi" }
      };
      await playerService.AddPlayersAsync(newPlayers);
      ```

#### Example Usage

Here’s an example of how to configure and use the `PlayerService` class:

##### Example Configuration (`appsettings.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;"
  }
}
```

##### Using the Service:

```csharp
using EAFC.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Set up configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        // Set up DI
        var serviceProvider = new ServiceCollection()
            .AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")))
            .AddScoped<IPlayerService, PlayerService>()
            .BuildServiceProvider();

        var playerService = serviceProvider.GetRequiredService<IPlayerService>();

        // Fetch the latest added players
        var latestPlayers = await playerService.GetLatestPlayersAsync();
        foreach (var player in latestPlayers.Items)
        {
            Console.WriteLine($"Player: {player.Name}, Added On: {player.AddedOn}");
        }

        // Add new players
        var newPlayers = new List<Player>
        {
            new Player { Name = "Cristiano Ronaldo", AddedOn = DateTime.UtcNow, Rating = 92, Position = "Forward", ProfileUrl = "http://example.com/ronaldo" }
        };
        await playerService.AddPlayersAsync(newPlayers);
    }
}
```
