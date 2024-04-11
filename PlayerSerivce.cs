using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;

public class PlayerService
{
    private YtdlLoader _ytdlLoader = new YtdlLoader();
    private ulong connectedGuildId = 0;
    private ulong connectedChannelId = 0;
    private Dictionary<ulong, ChannelPlayer> _channelPlayers = new Dictionary<ulong, ChannelPlayer>();

    public PlayerService() { }

    public void DisconnectAsync(ulong channelId) 
    {
        VerifyAndCreatePlayers(channelId);
        _channelPlayers[channelId].Disconnect();
    }

    public async Task AddSongToPlaylist(DiscordChannel channel, string songUrl)
    {
        VerifyAndCreatePlayers(channel.Id);
        await _channelPlayers[channel.Id].ConnectAsync(channel);
        var path = _ytdlLoader.LoadFromUrl(songUrl);
        _channelPlayers[channel.Id].AddSongToPlaylist(path);
    }
    
    public void SkipSong(DiscordChannel channel)
    {
        VerifyAndCreatePlayers(channel.Id);
        _channelPlayers[channel.Id].SkipSong();
    }

    private void VerifyAndCreatePlayers(ulong channelId)
    {
        if (!_channelPlayers.ContainsKey(channelId))
        {
            _channelPlayers.Add(channelId, new ChannelPlayer(channelId));
        }
    }
}