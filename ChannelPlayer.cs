using System.Buffers;
using System.Diagnostics;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using Microsoft.Extensions.Logging;

public class ChannelPlayer
{
    public ulong ChannelId { get; private set; }
    public ulong GuildId { get; private set; }
    public bool IsConnected { get; private set; }


    #region Private fields
    private YtdlLoader _ytdlLoader = new YtdlLoader();
    private VoiceNextConnection? _voiceConnection;
    private Thread _thread;
    private Stack<string> _playlist = new Stack<string>();

    // Default waiting 15 sec
    private int _waitingTresholdInMilliseconds = 15 * 1000;
    private int _waitingIncrement = 100;

    private bool _isSongRepeating = false;

    private bool isSongSkipped = false;

    private bool isDisconnectedManually = false;
    
    private ILogger logger;
    
    #endregion

    public ChannelPlayer(ulong channelId, ulong guildId)
    {
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
        logger = factory.CreateLogger(nameof(ChannelPlayer));
        ChannelId = channelId;
        GuildId = guildId;
        IsConnected = false;
        _thread = new Thread(PlaySongs);
        logger.LogInformation($"Channel player initialized for {channelId}");
    }

    public async Task ConnectAsync(DiscordChannel channel)
    {
        logger.LogInformation($"Connecting player...");
        if (!IsConnected)
        {
            _voiceConnection = await channel.ConnectAsync();
            IsConnected = true;
            logger.LogInformation("Connected to voice channel.");
        }
        else
        {
            logger.LogInformation("Voice already connected.");
        }
    }

    public void Disconnect()
    {
        logger.LogInformation($"Disconnecting player...");

        if(!IsConnected)
        {
            logger.LogWarning("Player wasn't connected to channel, returning.");
            return;
        }

        isSongSkipped = true;
        isDisconnectedManually = true;
        _playlist.Clear();
        
        if (_voiceConnection != null)
        {
            _voiceConnection.Disconnect();
            _voiceConnection = null;
        }
        IsConnected = false;
    }

    public void AddSongToPlaylist(string songUrl)
    {
        logger.LogInformation("Adding song...");
        _playlist.Push(songUrl);
        StartPlayback();
    }

    public void StartPlayback()
    {
        if (!_thread.IsAlive)
        {
            // Recreate thread if it was closed previously
            Console.WriteLine($"Thread status {_thread.ThreadState}");
            if(_thread.ThreadState == System.Threading.ThreadState.Stopped)
            {
                _thread = new Thread(PlaySongs);
            }

            try
            {
                _thread.Start();
            }
            catch (System.Exception ex)
            {
                logger.LogError($"Exception caught with starting new thread, details - {ex.Message}");
            }
        }
    }

    public bool ToggleRepeat()
    {
        return this._isSongRepeating = !_isSongRepeating;
    }

    public void SkipSong()
    {
        isSongSkipped = true;
    }

    private void PlaySongs()
    {
        isDisconnectedManually = false;

        // Rework trhis part
        while (_playlist.Any())
        {
            isSongSkipped = false;

            var nextTrackUrl = _playlist.Pop();
            var nextTrack = _ytdlLoader.LoadFromUrl(nextTrackUrl);

            if (!string.IsNullOrEmpty(nextTrack))
            {

                var processParams = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $@"-i ""{nextTrack}"" -ac 2 -f s16le -ar 48000 -hide_banner -loglevel error pipe:1",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };

                using (var ffmpeg = Process.Start(processParams))
                using (Stream pcm = ffmpeg!.StandardOutput.BaseStream)
                {
                    do
                    {
                        var transmit = _voiceConnection!.GetTransmitSink();

                        int bufferLength = transmit.SampleLength;
                        byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferLength);
                        try
                        {
                            int length;
                            while ((length = pcm.Read(buffer, 0, bufferLength)) != 0)
                            {
                                if (isSongSkipped)
                                    break;
                                transmit.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, length)).Wait();
                            }
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(buffer);
                        }
                    } while (_isSongRepeating);
                }
            }


            if (isDisconnectedManually)
            {
                //If forced disconnect - exit from playlist
                break;
            }

            var timeWaited = 0;
            while (!_playlist.Any())
            {
                Thread.Sleep(_waitingIncrement);
                timeWaited += _waitingIncrement;

                if (timeWaited == _waitingTresholdInMilliseconds)
                {
                    Disconnect();
                    return;
                }
            }
        }
    }
}