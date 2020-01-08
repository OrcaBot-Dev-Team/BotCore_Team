using BotCoreNET.Helpers;
using Discord;
using Discord.WebSocket;
using JSON;
using System.Threading.Tasks;

namespace BotCoreNET.CommandHandling.Commands
{
    internal class EmbedReplaceCommand : Command
    {
        public override string Summary => "Edits a message to follow a new embedjson";
        public override string Remarks => "The message author has to be by the bot used to modify!";
        public override Argument[] Arguments => new Argument[] {
            new Argument("MessageLink", "A discord message link to select the source"),
            new Argument("EmbedJSON", "The embed, formatted as a JSON", multiple: true)
        };

        private class ArgumentContainer
        {
            public IUserMessage message;
            public string messageContent;
            public EmbedBuilder embed;
        }

        
        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;

        public override HandledContexts ExecutionMethod => HandledContexts.DMOnly;
        private static readonly Precondition[] PRECONDITIONS = new Precondition[] { new HasRolePrecondition(BotCore.ADMINROLE_BOTVARID) };
        public override Precondition[] ExecutePreconditions => PRECONDITIONS;
        public override Precondition[] ViewPreconditions => PRECONDITIONS;

        public EmbedReplaceCommand(string identifier, CommandCollection collection) : base()
        {
            Register(identifier, collection);
        }

        protected override async Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            ArgumentContainer argOut = new ArgumentContainer();

            if (!context.Arguments.First.StartsWith("https://discordapp.com/channels/") || context.Arguments.First.Length < 40)
            {
                return new ArgumentParseResult(Arguments[0], "Not a valid message link! Failed Startswith or length test");
            }

            string[] messageIdentifiers = context.Arguments.First.Substring(32).Split('/');

            if (messageIdentifiers.Length != 3)
            {
                return new ArgumentParseResult(Arguments[0], "Not a valid message link! Failed split test");
            }

            if (!ulong.TryParse(messageIdentifiers[0], out ulong guildId) || !ulong.TryParse(messageIdentifiers[1], out ulong channelId) || !ulong.TryParse(messageIdentifiers[2], out ulong messageId))
            {
                return new ArgumentParseResult(Arguments[0], "Not a valid message link! Failed id parse test");
            }

            SocketGuild guild = BotCore.Client.GetGuild(guildId);

            if (guild != null)
            {
                SocketTextChannel channel = guild.GetTextChannel(channelId);

                if (channel != null)
                {
                    argOut.message = await channel.GetMessageAsync(messageId) as IUserMessage;

                    if (argOut.message == null)
                    {
                        return new ArgumentParseResult(Arguments[0], "Found correct guild and correct channel, but not correct message! Has the message been deleted?");
                    }
                    else if (argOut.message.Author.Id != BotCore.Client.CurrentUser.Id)
                    {
                        return new ArgumentParseResult(Arguments[0], "Can not edit a message the bot didn't post itself");
                    }
                }
                else
                {
                    return new ArgumentParseResult(Arguments[0], "Found correct guild, but not the channel!");
                }
            }
            else
            {
                return new ArgumentParseResult(Arguments[0], "Could not find the correct guild!");
            }

            if (context.Message.Content.Length > Identifier.Length + context.Arguments.First.Length + 2)
            {
                context.Arguments.Index++;
                string embedText = context.RemoveArgumentsFront(1).Replace("[3`]", "```");

                if (JSONContainer.TryParse(embedText, out JSONContainer json, out string errormessage))
                {
                    if (EmbedHelper.TryGetMessageFromJSONObject(json, out argOut.embed, out argOut.messageContent, out string error))
                    {
                        return new ArgumentParseResult(argOut);
                    }
                    else
                    {
                        return new ArgumentParseResult(Arguments[1], error);
                    }
                }
                else
                {
                    return new ArgumentParseResult(Arguments[1], $"Unable to parse JSON text to a json data structure! Error: `{errormessage}`");
                }
            }
            else
            {
                return new ArgumentParseResult("Internal Error: " + Macros.GetCodeLocation());
            }
        }

        protected override async Task Execute(IDMCommandContext context, object argObj)
        {
            ArgumentContainer args = argObj as ArgumentContainer;

            await args.message.ModifyAsync(message =>
            {
                message.Content = args.messageContent; message.Embed = args.embed?.Build();
            });
            await context.Channel.SendEmbedAsync("Edit done!");
        }
    }
}