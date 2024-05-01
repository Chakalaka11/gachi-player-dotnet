using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;

namespace GachiPlayerDotnet;
[SlashModuleLifespan(SlashModuleLifespan.Singleton)]
public class PlayerCommands : ApplicationCommandModule
{
    private PlayerService playerService;
    private ILogger logger;
    public PlayerCommands()
    {
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
        logger = factory.CreateLogger(nameof(PlayerCommands));

        logger.LogInformation("Player commands initialized!");
        playerService = new PlayerService();
    }

    [SlashCommand("play", "Play some jams!")]
    public async Task PlayCommand(InteractionContext ctx, [Option("url", "Link to Youtube video")] string url)
    {
        if (ctx.Channel.Type != ChannelType.Voice)
        {
            await ctx.CreateResponseAsync("Not a valid voice channel.");
            return;
        }

        await ctx.CreateResponseAsync(
            InteractionResponseType.DeferredChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .WithContent("Url received, trying to play..."));

        if(await playerService.AddSongToPlaylist(ctx.Channel, url))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Added song {url}"));
        }
        else
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Error occured when trying to add song, please check if its valid URL - {url}"));
        }
    }

    [SlashCommand("skip", "Skips current song.")]
    public async Task SkipCommand(InteractionContext ctx)
    {
        playerService.SkipSong(ctx.Channel);
        await ctx.CreateResponseAsync("Skipped song!");
    }


    [SlashCommand("repeat", "Repeat current song.")]
    public async Task RepeatCommand(InteractionContext ctx)
    {
        if (playerService.RepeatSong(ctx.Channel))
        {
            await ctx.CreateResponseAsync("Song is repeating.");
        }
        else
        {
            await ctx.CreateResponseAsync("Song is not repeating.");
        }
    }
}