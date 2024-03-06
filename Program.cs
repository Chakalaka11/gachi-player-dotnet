using Microsoft.Extensions.Configuration;

namespace GachiPlayerDotnet
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                   .AddJsonFile("appsettings.json")
                   .Build();

            Console.WriteLine(configuration["DiscordToken"]);
          
            DiscordHandler DH = new DiscordHandler();
            await DH.Connect(configuration["DiscordToken"]);
            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

    }
}