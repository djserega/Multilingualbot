using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultilingualBot.Commands
{
    public class SetCommand : BaseCommandModule, ICommands
    {
        //internal static event Func<string, string, bool> SetEmojiRoleEvent;

        //[Command("check-emoji-role")]
        //public async Task CheckEmojiRole(CommandContext ctx, ulong channelId = default, ulong messageId = default)
        //{

        //    if (messageId == default || messageId == default)
        //    {
        //        await ctx.RespondAsync("Что-то пошло не так...");
        //        return;
        //    }

        //    DiscordChannel channel = await ctx.Client.GetChannelAsync(channelId);
        //    DiscordMessage message = await channel.GetMessageAsync(messageId);

        //    //message.Reactions[0].
        //}
    }
}
