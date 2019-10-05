using BotCoreNET.Helpers;
using Discord;
using Discord.WebSocket;
using JSON;
using System.Threading.Tasks;

namespace BotCoreNET.CommandHandling.Commands
{
    internal class EmbedSendCommand : Command
    {
        public override string Summary => "Sends a fully featured embed to a guild text channel";
        public override string Remarks => "Good tool for creating JSON formatted embeds: [MagicBots](https://discord.club/embedg/)";
        public override Argument[] Arguments => new Argument[] {
            new Argument("Channel", ArgumentParsing.GENERIC_PARSED_CHANNEL),
            new Argument("EmbedJSON", "The embed, formatted as a JSON", multiple: true)
        };
        public override Precondition[] ExecutePreconditions => new Precondition[] { new IsOwnerOrAdminPrecondition() };
        public override Precondition[] ViewPreconditions => new Precondition[] { new IsOwnerOrAdminPrecondition() };

        private class ArgumentContainer
        {
            public SocketTextChannel channel;
            public string messageContent = string.Empty;
            public EmbedBuilder embed;
        }

        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;

        public override HandledContexts ExecutionMethod => HandledContexts.DMOnly;


        public EmbedSendCommand(string identifier, CommandCollection collection) : base()
        {
            Register(identifier, collection);
        }

        protected override Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            ArgumentContainer argOut = new ArgumentContainer();

            if (!ArgumentParsing.TryParseGuildTextChannel(context, context.Arguments.First, out argOut.channel))
            {
                return Task.FromResult(new ArgumentParseResult(Arguments[0], "Failed to parse to a guild text channel!"));
            }

            if (context.Message.Content.Length > Identifier.Length + context.Arguments.First.Length + 2)
            {
                context.Arguments.Index++;
                string embedText = context.RemoveArgumentsFront(1).Replace("[3`]", "```");

                if (JSONContainer.TryParse(embedText, out JSONContainer json, out string errormessage))
                {
                    if (EmbedHelper.TryGetMessageFromJSONObject(json, out argOut.embed, out argOut.messageContent, out string error))
                    {
                        return Task.FromResult(new ArgumentParseResult(argOut));
                    }
                    else
                    {
                        return Task.FromResult(new ArgumentParseResult(Arguments[1], error));
                    }
                }
                else
                {
                    return Task.FromResult(new ArgumentParseResult(Arguments[1], $"Unable to parse JSON text to a json data structure! Error: `{errormessage}`"));
                }
            }
            else
            {
                return Task.FromResult(new ArgumentParseResult("Internal Error: " + Macros.GetCodeLocation()));
            }
        }


        protected override async Task Execute(IDMCommandContext context, object argObj)
        {
            ArgumentContainer args = argObj as ArgumentContainer;

            if (args.embed == null && args.messageContent != null)
            {
                await args.channel.SendMessageAsync(args.messageContent);
            }
            else if (args.embed != null && args.messageContent == null)
            {
                await args.channel.SendEmbedAsync(args.embed);
            }
            else if (args.embed != null && args.messageContent != null)
            {
                await args.channel.SendMessageAsync(text: args.messageContent, embed: args.embed.Build());
            }
            else
            {
                await context.Channel.SendEmbedAsync("The json you provided had no information or could not be parsed!", true);
                return;
            }
            await context.Channel.SendEmbedAsync("Done. Check it out here: " + args.channel.Mention);
        }
    }
}
