using BotCoreNET.Helpers;
using Discord;
using Discord.WebSocket;
using JSON;
using System.Threading.Tasks;

namespace BotCoreNET.CommandHandling.Commands
{
    class EmbedSendCommand : Command
    {
        public override string Summary => "Sends a fully featured embed to a guild text channel";
        public override string Remarks => "Good tool for creating JSON formatted embeds: [MagicBots](https://discord.club/embedg/)";
        public override Argument[] Arguments => new Argument[] {
            new Argument("Channel", ArgumentParsing.GENERIC_PARSED_CHANNEL),
            new Argument("EmbedJSON", "The embed, formatted as a JSON", multiple: true)
        };
        public override Precondition[] ExecutePreconditions => new Precondition[] { new IsOwnerOrAdminPrecondition() };
        public override Precondition[] ViewPreconditions => new Precondition[] { new IsOwnerOrAdminPrecondition() };


        private SocketTextChannel channel;
        string messageContent = string.Empty;
        private EmbedBuilder embed;

        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;

        public override HandledContexts ExecutionMethod => HandledContexts.DMOnly;


        public EmbedSendCommand(string identifier, CommandCollection collection) : base()
        {
            Register(identifier, collection);
        }

        protected override Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            if (!ArgumentParsing.TryParseGuildTextChannel(context, context.Arguments.First, out channel))
            {
                return Task.FromResult(new ArgumentParseResult(Arguments[0], "Failed to parse to a guild text channel!"));
            }

            if (context.Message.Content.Length > Identifier.Length + context.Arguments.First.Length + 2)
            {
                context.Arguments.Index++;
                string embedText = context.RemoveArgumentsFront(1).Replace("[3`]", "```");

                if (JSONContainer.TryParse(embedText, out JSONContainer json, out string errormessage))
                {
                    if (EmbedHelper.TryGetMessageFromJSONObject(json, out embed, out messageContent, out string error))
                    {
                        return Task.FromResult(ArgumentParseResult.SuccessfullParse);
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
                embed = null;
                return Task.FromResult(new ArgumentParseResult("Internal Error: " + Macros.GetCodeLocation()));
            }
        }


        protected override async Task Execute(IDMCommandContext context)
        {
            if (embed == null && messageContent != null)
            {
                await channel.SendMessageAsync(messageContent);
            }
            else if (embed != null && messageContent == null)
            {
                await channel.SendEmbedAsync(embed);
            }
            else if (embed != null && messageContent != null)
            {
                await channel.SendMessageAsync(text: messageContent, embed: embed.Build());
            }
            else
            {
                await context.Channel.SendEmbedAsync("The json you provided had no information or could not be parsed!", true);
                return;
            }
            await context.Channel.SendEmbedAsync("Done. Check it out here: " + channel.Mention);
        }
    }
}
