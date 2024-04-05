using DSharpPlus;
using DSharpPlus.AsyncEvents;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.SlashCommands;
using Microsoft.VisualBasic;

namespace GachiPlayerDotnet;
[SlashModuleLifespan(SlashModuleLifespan.Singleton)]
public class PlayerCommands : ApplicationCommandModule
{
    private Dictionary<ulong, Stack<LavalinkTrack>> _playlists = new Dictionary<ulong, Stack<LavalinkTrack>>();
    private ulong connectedGuildId = 0;
    private ulong connectedChannelId = 0;
    private Dictionary<ulong, LavalinkGuildConnection?> _voiceConnections = new Dictionary<ulong, LavalinkGuildConnection?>();

    public PlayerCommands()
    {
        Console.WriteLine("Player commands initialized!");
    }

    [SlashCommand("play", "Play some jams!")]
    public async Task PlayCommand(InteractionContext ctx, [Option("url", "Link to Youtube video")] string url)
    {
        var lava = ctx.Client.GetLavalink();
        if (!lava.ConnectedNodes.Any())
        {
            await ctx.CreateResponseAsync("The Lavalink connection is not established");
            return;
        }
        if (ctx.Channel.Type != ChannelType.Voice)
        {
            await ctx.CreateResponseAsync("Not a valid voice channel.");
            return;
        }

        await ctx.CreateResponseAsync(
            InteractionResponseType.DeferredChannelMessageWithSource, 
            new DiscordInteractionResponseBuilder()
                .WithContent("Url received, trying to play..."));
        
        var channelId = ctx.Channel.Id;
        var guildId = ctx.Guild.Id;
        var node = lava.ConnectedNodes.Values.First();
        
        if(!_playlists.ContainsKey(channelId))
        {
            _voiceConnections.Add(channelId, null);
        }

        if (connectedChannelId == 0)
        {
            _voiceConnections[channelId] = await node.ConnectAsync(ctx.Channel);
            _voiceConnections[channelId]!.PlaybackFinished += HandlePlaybackFinish;
            connectedChannelId = channelId;
        }
        else    
        {
            if(connectedChannelId != channelId)
            {
                // Disconnect from current channel and connect to one that we received interaction from
                await _voiceConnections[connectedChannelId]!.DisconnectAsync();
                _voiceConnections[connectedChannelId] = null;

                _voiceConnections[channelId] = await node.ConnectAsync(ctx.Channel);
                _voiceConnections[channelId]!.PlaybackFinished += HandlePlaybackFinish;

                connectedChannelId = channelId;
            }
        }

        if (_voiceConnections[connectedChannelId] == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Lavalink is not connected."));
            return;
        }

        var loadResult = await node.Rest.GetTracksAsync(url);
        if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed
            || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Track search failed for {url}."));
            return;
        }

        var track = loadResult.Tracks.First();

        AddSongToPlaylist(connectedChannelId, track);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Added song {url}"));

        // If nothing played by voice connection - get current track 
        if (_voiceConnections[connectedChannelId]!.CurrentState.CurrentTrack == null)
        {
            var currentTrack = GetNextTrack(connectedChannelId);
            await _voiceConnections[connectedChannelId]!.PlayAsync(currentTrack);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Now playing {url}"));
        }

    }

    [SlashCommand("skip", "Skips current song.")]
    public async Task SkipCommand(InteractionContext ctx)
    {
        var channelId = ctx.Channel.Id;
        var guildId = ctx.Guild.Id;
        if (ctx.Channel.Type != ChannelType.Voice)
        {
            await ctx.CreateResponseAsync("Not a valid voice channel.");
            return;
        }
        if (!_voiceConnections.ContainsKey(channelId))
        {
            await ctx.CreateResponseAsync("There are no players created.");
            return;
        }
        if (_voiceConnections[channelId] == null)
        {
            await ctx.CreateResponseAsync("No player exists in this channel.");
            return;
        }

        var lava = ctx.Client.GetLavalink();
        if (!lava.ConnectedNodes.Any())
        {
            await ctx.CreateResponseAsync("The Lavalink connection is not established");
            return;
        }

        await _voiceConnections[channelId]!.StopAsync();
        await ctx.CreateResponseAsync("Skipped song!");
    }

    private void AddSongToPlaylist(ulong channelId, LavalinkTrack track)
    {
        if (!_playlists.ContainsKey(channelId))
        {
            _playlists.Add(channelId, new Stack<LavalinkTrack>());
        }
        _playlists[channelId].Push(track);
    }

    private LavalinkTrack? GetNextTrack(ulong channelId)
    {
        if (!_playlists.ContainsKey(channelId))
        {
            Console.WriteLine("There are no playlist created.");
            return null;
        }
        if (_playlists[channelId].Count == 0)
        {
            Console.WriteLine("There are no songs in playlist.");
            return null;
        }

        return _playlists[channelId].Pop();
    }
    private async Task HandlePlaybackFinish(LavalinkGuildConnection sender, TrackFinishEventArgs args)
    {
        var nextTrack = GetNextTrack(sender.Channel.Id);
        if(nextTrack == null)
        {
            await Task.Delay(60 * 1000);
            nextTrack = GetNextTrack(sender.Channel.Id);
        }

        if(nextTrack != null)
        {
            await _voiceConnections[sender.Channel.Id]!.PlayAsync(nextTrack);
        }
        else
        {
            await _voiceConnections[sender.Channel.Id]!.DisconnectAsync();
            _voiceConnections[connectedChannelId] = null;
            connectedChannelId = 0;
        }
    }
}