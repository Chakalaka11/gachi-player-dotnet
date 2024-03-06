
using System.Diagnostics;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;

namespace GachiPlayerDotnet
{
    public class DiscordHandler
    {
        private const string PingCommandName = "ping";
        private const string PlayCommandName = "play";

        private readonly DiscordSocketClient _client = new DiscordSocketClient();
        public DiscordHandler()
        {
            _client.Log += Log;
            _client.SlashCommandExecuted += SlashCommandHandler;

        }
        public async Task Connect(string token)
        {
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
        }

        [Command(RunMode = RunMode.Async)]
        public async Task SlashCommandHandler(SocketSlashCommand command)
        {
            Console.WriteLine($"{command.CommandName} {command.Data.Id} {command.Data.Name}");

            switch (command.CommandName)
            {
                case PingCommandName:
                    await PingCommand(command);
                    break;
                case PlayCommandName:
                    await PlayCommand(command);
                    break;
                default:
                    Console.WriteLine($"Can't handle command name {command.CommandName}");
                    break;
            }
        }

        private async Task PingCommand(SocketSlashCommand command)
        {
            await command.RespondAsync("Oh my shoulder! v2");
        }

        [Command(RunMode = RunMode.Async)]
        private async Task PlayCommand(SocketSlashCommand command)
        {
            const string UrlOptionName = "url";
            var youtubeUrlPathOption = command.Data.Options.First(x => x.Name == UrlOptionName);

            var currentChannel = _client.GetChannel((ulong)command.ChannelId!) as IAudioChannel;
            Console.WriteLine($"Channel id {command.ChannelId}");
            Console.WriteLine($"Channel type {currentChannel.GetType()}");

            var audioClient = await currentChannel.ConnectAsync();
            await SendAsync(audioClient,"./AudioFiles/audioTrack-1709213809974.mp4");
            await command.RespondAsync($"Got command with path: {youtubeUrlPathOption.Value}");
        }
        
        private async Task SendAsync(IAudioClient client, string path)
        {
            // Create FFmpeg using the previous example
            using (var ffmpeg = CreateStream(path))
            using (var output = ffmpeg.StandardOutput.BaseStream)
            using (var discord = client.CreatePCMStream(AudioApplication.Mixed))
            {
                try { await output.CopyToAsync(discord); }
                finally { await discord.FlushAsync(); }
            }
        }

        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }
        
        private static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}