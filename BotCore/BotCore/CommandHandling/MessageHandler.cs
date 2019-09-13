using BotCoreNET.BotVars;
using BotCoreNET.Helpers;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCoreNET.CommandHandling
{
    internal static class MessageHandler
    {
        public static string Prefix { get; private set; }

        internal static Task Client_MessageReceived(SocketMessage arg)
        {
            SocketUserMessage userMessage = arg as SocketUserMessage;

            if (userMessage != null)
            {
                SocketTextChannel guildChannel = userMessage.Channel as SocketTextChannel;

                if (userMessage.Content.StartsWith(Prefix) && userMessage.Content.Length > Prefix.Length)
                {
                    bool isGuildContext = guildChannel != null;

                    IMessageContext messageContext;
                    IDMCommandContext commandContext;
                    IGuildCommandContext guildCommandContext;
                    if (isGuildContext)
                    {
                        messageContext = new GuildMessageContext(userMessage, guildChannel.Guild);
                    }
                    else
                    {
                        messageContext = new MessageContext(userMessage);
                    }

                    if (messageContext.IsDefined)
                    {
                        if (isGuildContext)
                        {
                            guildCommandContext = new GuildCommandContext(messageContext as GuildMessageContext);
                            commandContext = guildCommandContext;
                        }
                        else
                        {
                            commandContext = new DMCommandContext(messageContext);
                            guildCommandContext = null;
                        }

                        if (isGuildContext)
                        {
                            if (!guildCommandContext.ChannelMeta.CheckChannelMeta(guildCommandContext.UserInfo, guildCommandContext.InterpretedCommand, out string error))
                            {
                                return messageContext.Channel.SendEmbedAsync(error, true);
                            }
                        }

                        switch (commandContext.CommandSearch)
                        {
                            case CommandSearchResult.NoMatch:
                                return messageContext.Message.AddReactionAsync(UnicodeEmoteService.Question);
                            case CommandSearchResult.PerfectMatch:
                                return commandContext.InterpretedCommand.HandleCommandAsync(commandContext, guildCommandContext);
                            case CommandSearchResult.TooFewArguments:
                                return messageContext.Channel.SendEmbedAsync($"The command `{commandContext.InterpretedCommand}` requires a minimum of {commandContext.InterpretedCommand.MinimumArgumentCount} arguments!", true);
                            case CommandSearchResult.TooManyArguments:
                                return commandContext.InterpretedCommand.HandleCommandAsync(commandContext, guildCommandContext);
                        }
                    }
                }
            }
            return Task.CompletedTask;
        }


        static MessageHandler()
        {
            BotVar defaultPrefix = new BotVar("prefix", "/");
            BotVarManager.SetDefault(defaultPrefix);
            OnBotVarUpdated(defaultPrefix);
        }

        internal static void SetupBotVarSubscription()
        {
            BotVarManager.SubscribeToBotVarUpdateEvent(OnBotVarUpdated, "prefix");
        }

        private static void OnBotVarUpdated(BotVar var)
        {
            if (var.Identifier == "prefix" && var.IsString)
            {
                Prefix = var.String;
            }
        }
    }
}
