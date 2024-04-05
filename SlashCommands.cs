using DSharpPlus.SlashCommands;

namespace GachiPlayerDotnet;

public class SlashCommands : ApplicationCommandModule
{
    [SlashCommand("ping", "To verify if everything is working.")]
    public async Task PingCommand(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync("Oh my shoulder! v3");
    }
}