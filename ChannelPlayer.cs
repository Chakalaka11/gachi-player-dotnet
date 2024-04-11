using System.Buffers;
using System.Diagnostics;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;

public class ChannelPlayer
{
    public ulong ChannelId { get; private set; }


    #region Private fields
    private VoiceNextConnection? _voiceConnection;
    private Thread _thread;
    private Stack<string> _playlist = new Stack<string>();

    // Default waiting 2 min
    private int _waitingTresholdInMilliseconds = 15 * 1000;
    private int _waitingIncrement = 100;

    private CancellationTokenSource playSongCancellationTokenSource;

    private CancellationTokenSource disconnectCancellationTokenSource;
    #endregion

    public ChannelPlayer(ulong channelId)
    {
        ChannelId = channelId;
        _thread = new Thread(PlaySongs);
    }

    public async Task ConnectAsync(DiscordChannel channel)
    {
        if (_voiceConnection == null)
        {
            _voiceConnection = await channel.ConnectAsync();
            Console.WriteLine("Connect to voice channel.");
        }
        else
        {
            Console.WriteLine("Voice already connected.");
        }
    }

    public void Disconnect()
    {
        playSongCancellationTokenSource.Cancel();
        disconnectCancellationTokenSource.Cancel();
        _playlist.Clear();
        
        if (_voiceConnection != null)
        {
            _voiceConnection.Disconnect();
            _voiceConnection = null;
        }

        playSongCancellationTokenSource.Dispose();
        disconnectCancellationTokenSource.Dispose();
    }

    public void AddSongToPlaylist(string songPath)
    {
        Console.WriteLine("Adding song...");
        _playlist.Push(songPath);
        StartPlayback();
    }

    public void StartPlayback()
    {
        if (!_thread.IsAlive)
        {
            // Recreate thread if it was closed previously
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
                Console.WriteLine($"{ex.Message}");
            }
        }
    }

    public void SkipSong()
    {
        playSongCancellationTokenSource.Cancel();
    }

    private void PlaySongs()
    {
        disconnectCancellationTokenSource = new CancellationTokenSource();

        while (_playlist.Any())
        {
            // Re-initialized tokens
            playSongCancellationTokenSource = new CancellationTokenSource();

            var nextTrack = _playlist.Pop();
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
                try
                {                        
                    var transmit = _voiceConnection!.GetTransmitSink();

                    int bufferLength = transmit.SampleLength;
                    byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferLength);
                    try
                    {
                        int length;
                        while ((length = pcm.Read(buffer, 0, bufferLength)) != 0)
                        {
                            transmit.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, length), playSongCancellationTokenSource.Token).Wait();
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Song Canceled");
                    Console.WriteLine("Exception details: " + ex.ToString());
                }
            }
            

            if (disconnectCancellationTokenSource.Token.IsCancellationRequested)
            {
                // Exit from playlist
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
            
            // Clear tokens
            playSongCancellationTokenSource.Dispose();
        }
    }
}