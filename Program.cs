﻿using DSharpPlus;
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
        var slash = discord.UseSlashCommands();
        slash.RegisterCommands<SlashCommands>();

        await discord.ConnectAsync();
        await Task.Delay(-1);
    }
}
