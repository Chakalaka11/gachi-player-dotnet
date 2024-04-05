using DSharpPlus;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Configuration;

namespace GachiPlayerDotnet;

class Program
{
    static async Task Main(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json")
               .AddJsonFile("appsettings.local.json")
               .Build();

        Console.WriteLine(configuration["DiscordToken"]);
        var discord = new DiscordClient(new DiscordConfiguration()
        {
            Token = configuration["DiscordToken"],
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.Guilds | DiscordIntents.GuildVoiceStates
        });
        var endpoint = new ConnectionEndpoint
        {
            Hostname = "127.0.0.1", // From your server configuration.
            Port = 2333 // From your server configuration
        };

        var lavalinkConfig = new LavalinkConfiguration
        {
            Password = configuration["LavalinkPass"], // From your server configuration.
            RestEndpoint = endpoint,
            SocketEndpoint = endpoint
        };

        var lavalink = discord.UseLavalink();
        var slash = discord.UseSlashCommands();
        slash.RegisterCommands<SlashCommands>();
        slash.RegisterCommands<PlayerCommands>();

        await discord.ConnectAsync();
        await lavalink.ConnectAsync(lavalinkConfig); 
        await Task.Delay(-1);
    }
}
