using BotCoreNET.Helpers;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace BotCoreNET.CommandHandling
{
    internal static class MessageHandler
    {
        internal static ICommandParser CommandParser;

        internal static Task Client_MessageReceived(SocketMessage arg)
        {
            SocketUserMessage userMessage = arg as SocketUserMessage;

            if (userMessage != null)
            {
                SocketTextChannel guildChannel = userMessage.Channel as SocketTextChannel;
                bool isGuildContext = guildChannel != null;

                bool ispotentialCommand;
                if (isGuildContext)
                {
                    ispotentialCommand = CommandParser.IsPotentialCommand(userMessage.Content, guildChannel.Guild.Id);
                }
                else
                {
                    ispotentialCommand = CommandParser.IsPotentialCommand(userMessage.Content);
                }

                if (ispotentialCommand)
                {
                    IMessageContext messageContext;
                    IGuildMessageContext guildMessageContext;
                    IDMCommandContext commandContext;
                    IGuildCommandContext guildCommandContext;
                    if (isGuildContext)
                    {
                        guildMessageContext = new GuildMessageContext(userMessage, guildChannel.Guild);
                        messageContext = guildMessageContext;
                    }
                    else
                    {
                        messageContext = new MessageContext(userMessage);
                        guildMessageContext = null;
                    }

                    if (messageContext.IsDefined)
                    {
                        if (isGuildContext)
                        {
                            guildCommandContext = new GuildCommandContext(guildMessageContext, CommandParser.ParseCommand(guildMessageContext));
                            commandContext = guildCommandContext;
                        }
                        else
                        {
                            commandContext = new DMCommandContext(messageContext, CommandParser.ParseCommand(messageContext));
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

        internal static void SetupBotVarSubscription()
        {
            CommandParser.OnBotVarSetup();
        }

    }
}
