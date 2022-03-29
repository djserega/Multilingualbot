using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultilingualBot
{
    public static class Additions
    {
        public static DiscordRole GetDiscordRole(this string source, DiscordGuild guild)
        {
            return guild.GetRole(source.GetID());
        }

        public static ulong GetID(this string source)
        {
            return ulong.Parse(new string(source.Where(char.IsDigit).ToArray()));
        }
    }
}
