using System.Diagnostics;
using DSharpPlus;
using DSharpPlus.AsyncEvents;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;

namespace GachiPlayerDotnet;
[SlashModuleLifespan(SlashModuleLifespan.Singleton)]
public class PlayerCommands : ApplicationCommandModule
{
    private PlayerService playerService;
    public PlayerCommands()
    {
        Console.WriteLine("Player commands initialized!");
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

        await playerService.AddSongToPlaylist(ctx.Channel, url);
        
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Now playing {url}"));
    }

    [SlashCommand("skip", "Skips current song.")]
    public async Task SkipCommand(InteractionContext ctx)
    {
        playerService.SkipSong(ctx.Channel);
        await ctx.CreateResponseAsync("Skipped song!");
    }
}