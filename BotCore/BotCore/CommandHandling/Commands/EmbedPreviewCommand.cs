using BotCoreNET.Helpers;
using Discord;
using JSON;
using System.Threading.Tasks;

namespace BotCoreNET.CommandHandling.Commands
{
    internal class EmbedPreviewCommand : Command
    {
        public override string Summary => "Previews an embed in the channel the command is issued from";
        public override string Remarks => "Good tool for creating JSON formatted embeds: [MagicBots](https://discord.club/embedg/)";
        public override Argument[] Arguments => new Argument[] {
            new Argument("EmbedJSON", "The embed, formatted as a JSON", multiple: true)
        };
        public override Precondition[] ExecutePreconditions => new Precondition[] { new IsOwnerOrAdminPrecondition() };
        public override Precondition[] ViewPreconditions => new Precondition[] { new IsOwnerOrAdminPrecondition() };

        private class ArgumentContainer
        {
            public string messageContent = string.Empty;
            public EmbedBuilder embed;
        }

        public override HandledContexts ArgumentParserMethod => HandledContexts.DMOnly;

        public override HandledContexts ExecutionMethod => HandledContexts.DMOnly;


        public EmbedPreviewCommand(string identifier, CommandCollection collection) : base()
        {
            Register(identifier, collection);
        }

        protected override Task<ArgumentParseResult> ParseArguments(IDMCommandContext context)
        {
            ArgumentContainer argOut = new ArgumentContainer();

            if (context.Message.Content.Length > Identifier.Length + 1)
            {
                string embedText = context.ArgumentSection.Replace("[3`]", "```");

                if (JSONContainer.TryParse(embedText, out JSONContainer json, out string errormessage))
                {
                    if (EmbedHelper.TryGetMessageFromJSONObject(json, out argOut.embed, out argOut.messageContent, out string error))
                    {
                        return Task.FromResult(new ArgumentParseResult(argOut));
                    }
                    else
                    {
                        return Task.FromResult(new ArgumentParseResult(Arguments[0], error));
                    }
                }
                else
                {
                    return Task.FromResult(new ArgumentParseResult(Arguments[0], $"Unable to parse JSON text to a json data structure! Error: `{errormessage}`"));
                }
            }
            else
            {
                return Task.FromResult(new ArgumentParseResult("Internal Error: " + Macros.GetCodeLocation()));
            }
        }

        protected override Task Execute(IDMCommandContext context, object argObject)
        {
            ArgumentContainer args = argObject as ArgumentContainer;

            if (args.embed == null && args.messageContent != null)
            {
                return context.Channel.SendMessageAsync(args.messageContent);
            }
            else if (args.embed != null && args.messageContent == null)
            {
                return context.Channel.SendEmbedAsync(args.embed);
            }
            else if (args.embed != null && args.messageContent != null)
            {
                return context.Channel.SendMessageAsync(text: args.messageContent, embed: args.embed.Build());
            }
            else
            {
                return context.Channel.SendEmbedAsync("The json you provided had no information or could not be parsed!", true);
            }
        }
    }

}
