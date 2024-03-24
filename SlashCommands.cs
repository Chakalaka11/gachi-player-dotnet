using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace GachiPlayerDotnet;
public class SlashCommands : ApplicationCommandModule
{
    [SlashCommand("ping", "To verify if everything is working.")]
    public async Task DelayTestCommand(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Oh my shoulder! v3"));
    }
}