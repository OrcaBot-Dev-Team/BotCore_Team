using BotCoreNET.BotVars;
using BotCoreNET.Helpers;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotCoreNET
{
    public static class ExceptionHandler
    {
        private static ulong GuildId;
        private static ulong RoleId;
        private static ulong ChannelId;

        internal static void SetupBotVar()
        {
            BotVarManager.SubscribeToBotVarUpdateEvent(OnBotVarsUpdated, "exceptionhandler.guildid", "exceptionhandler.roleid", "exceptionhandler.channelid");
        }

        private static void OnBotVarsUpdated(BotVar var)
        {
            if (var.IsUnsignedInt64)
            {
                switch (var.Identifier)
                {
                    case "exceptionhandler.guildid":
                        GuildId = var.UInt64;
                        break;
                    case "exceptionhandler.roleid":
                        RoleId = var.UInt64;
                        break;
                    case "exceptionhandler.channelid":
                        ChannelId = var.UInt64;
                        break;
                }
            }
        }

        private static bool TryGetChannel(out SocketTextChannel channel)
        {
            var guild = BotCore.Client.GetGuild(GuildId);
            if (guild != null)
            {
                channel = guild.GetTextChannel(ChannelId);
                return channel != null;
            }
            channel = null;
            return false;
        }

        private static bool TryGetRole(out SocketRole role)
        {
            var guild = BotCore.Client.GetGuild(GuildId);
            if (guild != null)
            {
                role = guild.GetRole(RoleId);
                return role != null;
            }
            role = null;
            return false;
        }

        public static async Task ReportException(Exception e, string source, string context = null)
        {
            await BotCore.Log(new LogMessage(LogSeverity.Error, source, context, e));
            if (TryGetChannel(out SocketTextChannel exceptionChannel))
            {
                EmbedBuilder exceptionEmbed = new EmbedBuilder()
                {
                    Description = Markdown.MultiLineCodeBlock(e.StackTrace.MaxLength(EmbedHelper.EMBEDDESCRIPTION_MAX - 6)),
                    Color = BotCore.ErrorColor
                };
                if (TryGetRole(out SocketRole exceptionRole))
                {
                    exceptionEmbed.Title = $"{exceptionRole.Mention} Exception reported from `{source}`{(string.IsNullOrEmpty(context) ? "" : $" with context `{context}`")}".MaxLength(EmbedHelper.EMBEDTITLE_MAX);
                }
                else
                {
                    exceptionEmbed.Title = $"Exception reported from `{source}`{(string.IsNullOrEmpty(context) ? "" : $" with context `{context}`")}".MaxLength(EmbedHelper.EMBEDTITLE_MAX);
                }
                try
                {
                    await exceptionChannel.SendEmbedAsync(exceptionEmbed);
                }
                catch (Exception sendException)
                {
                    await BotCore.Log(new LogMessage(LogSeverity.Error, "EXCEPTHNDL", "Failed to send exception message!", sendException));
                }
            }
        }
    }
}
