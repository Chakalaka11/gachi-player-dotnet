using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using Microsoft.Extensions.Logging;

public class PlayerService
{
    private Dictionary<ulong, ChannelPlayer> _channelPlayers = new Dictionary<ulong, ChannelPlayer>();
    private ILogger logger;

    public PlayerService() 
    {
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
        logger = factory.CreateLogger(nameof(PlayerService));
    }

    public void DisconnectAsync(DiscordChannel channel) 
    {
        VerifyAndCreatePlayer(channel);
        _channelPlayers[channel.Id].Disconnect();
    }

    public async Task<bool> AddSongToPlaylist(DiscordChannel channel, string songUrl)
    {
        VerifyAndCreatePlayer(channel);

        var connectedPlayer = _channelPlayers.Values.FirstOrDefault(x => x.IsConnected);
        if (connectedPlayer != null && connectedPlayer.ChannelId != channel.Id)
        {
            // If called from different channel - disconnect and connect to new one
            connectedPlayer.Disconnect();
        }

        await _channelPlayers[channel.Id].ConnectAsync(channel);

        _channelPlayers[channel.Id].AddSongToPlaylist(songUrl);
        return true;
    }

    public void SkipSong(DiscordChannel channel)
    {
        VerifyAndCreatePlayer(channel);
        _channelPlayers[channel.Id].SkipSong();
    }
    
    public bool RepeatSong(DiscordChannel channel)
    {
        VerifyAndCreatePlayer(channel);
        return _channelPlayers[channel.Id].ToggleRepeat();
    }

    private void VerifyAndCreatePlayer(DiscordChannel channel)
    {
        if (!_channelPlayers.ContainsKey(channel.Id))
        {
            _channelPlayers.Add(channel.Id, new ChannelPlayer(channel.Id, channel.Guild.Id));
        }
    }
}