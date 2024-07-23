using Microsoft.Extensions.Configuration;
using SoraBatteryStatus.DTO;

namespace SoraBatteryStatus;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        
        // add our appsettings.json file
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        IConfigurationRoot configuration = builder.Build();

        var mouseConfig = new MouseConfiguration();
        configuration.GetSection("MouseConfiguration").Bind(mouseConfig);
        
        // start the app and inject our settings file
        Application.Run(new BatteryStatusTray(mouseConfig));
    }
}