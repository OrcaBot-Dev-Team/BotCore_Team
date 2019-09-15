using BotCoreNET.Helpers;
using Discord;
using Discord.WebSocket;
using JSON;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotCoreNET.CommandHandling.Commands
{
    #region send

    class SendEmbedCommand : Command
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


        public SendEmbedCommand(string identifier, CommandCollection collection) : base()
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
                    return Task.FromResult(EmbedHelper.TryParseEmbedFromJSONObject(json, out embed, out messageContent));
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

    #endregion
    #region preview

    class PreviewEmbedCommand : Command
    {
        public override string Summary => "Previews an embed in the channel the command is issued from";
        public override string Remarks => "Good tool for creating JSON formatted embeds: [MagicBots](https://discord.club/embedg/)";
        public override Argument[] Arguments => new Argument[] {
            new Argument("EmbedJSON", "The embed, formatted as a JSON", multiple: true)
        };
        public override Precondition[] ExecutePreconditions => new Precondition[] { new IsOwnerOrAdminPrecondition() };
        public override Precondition[] ViewPreconditions => new Precondition[] { new IsOwnerOrAdminPrecondition() };


        string messageContent = string.Empty;
        private EmbedBuilder embed;

        public override HandledContexts ArgumentParserMethod => HandledContexts.DMOnly;

        public override HandledContexts ExecutionMethod => HandledContexts.DMOnly;


        public PreviewEmbedCommand(string identifier, CommandCollection collection) : base()
        {
            Register(identifier, collection);
        }

        protected override Task<ArgumentParseResult> ParseArguments(IDMCommandContext context)
        {
            if (context.Message.Content.Length > Identifier.Length + 1)
            {
                string embedText = context.ArgumentSection.Replace("[3`]", "```");

                if (JSONContainer.TryParse(embedText, out JSONContainer json, out string errormessage))
                {
                    return Task.FromResult(EmbedHelper.TryParseEmbedFromJSONObject(json, out embed, out messageContent));
                }
                else
                {
                    return Task.FromResult(new ArgumentParseResult(Arguments[0], $"Unable to parse JSON text to a json data structure! Error: `{errormessage}`"));
                }
            }
            else
            {
                embed = null;
                return Task.FromResult(new ArgumentParseResult("Internal Error: " + Macros.GetCodeLocation()));
            }
        }

        protected override Task Execute(IDMCommandContext context)
        {
            if (embed == null && messageContent != null)
            {
                return context.Channel.SendMessageAsync(messageContent);
            }
            else if (embed != null && messageContent == null)
            {
                return context.Channel.SendEmbedAsync(embed);
            }
            else if (embed != null && messageContent != null)
            {
                return context.Channel.SendMessageAsync(text: messageContent, embed: embed.Build());
            }
            else
            {
                return context.Channel.SendEmbedAsync("The json you provided had no information or could not be parsed!", true);
            }
        }
    }

    #endregion
    #region get

    class GetEmbedCommand : Command
    {
        public override string Summary => "Formats a JSON from a given message, including embeds";
        public override string Remarks => "Good tool for creating JSON formatted embeds: [MagicBots](https://discord.club/embedg/)";
        public override Argument[] Arguments => new Argument[] {
            new Argument("MessageLink", "A discord message link to select the source"),
            new Argument("Options", $"Command execution options. Available are:\n`{ExecutionOptions.pretty}` = Include some nice formatting in the embed JSON\n" +
                $"`{ExecutionOptions.remove}` = Remove the source message after retrieving the embed", true, true)
        };
        public override Precondition[] ExecutePreconditions => new Precondition[] { new IsOwnerOrAdminPrecondition() };
        public override Precondition[] ViewPreconditions => new Precondition[] { new IsOwnerOrAdminPrecondition() };


        public GetEmbedCommand(string identifier, CommandCollection collection) : base()
        {
            Register(identifier, collection);
        }

        private SocketGuild guild;
        private SocketTextChannel channel;
        private IMessage message;
        private List<ExecutionOptions> options = new List<ExecutionOptions>();

        public override HandledContexts ArgumentParserMethod => HandledContexts.DMOnly;

        public override HandledContexts ExecutionMethod => HandledContexts.DMOnly;


        protected override async Task<ArgumentParseResult> ParseArguments(IDMCommandContext context)
        {
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

            guild = BotCore.Client.GetGuild(guildId);

            if (guild != null)
            {
                channel = guild.GetTextChannel(channelId);

                if (channel != null)
                {
                    message = await channel.GetMessageAsync(messageId);

                    if (message == null)
                    {
                        return new ArgumentParseResult(Arguments[0], "Found correct guild and correct channel, but not correct message! Has the message been deleted?");
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

            options.Clear();
            if (context.Arguments.TotalCount > 1)
            {

                context.Arguments.Index++;

                bool parseError = false;
                foreach (string arg in context.Arguments)
                {
                    if (Enum.TryParse(arg, out ExecutionOptions option))
                    {
                        if (!options.Contains(option))
                        {
                            options.Add(option);
                        }
                    }
                    else
                    {
                        parseError = true;
                    }
                }

                context.Arguments.Index--;

                if (parseError)
                {
                    return new ArgumentParseResult(Arguments[1], $"Not a valid execution option! Available are: `{ string.Join(", ", Enum.GetNames(typeof(ExecutionOptions))) }`");
                }
            }
            return ArgumentParseResult.SuccessfullParse;
        }

        protected override async Task Execute(IDMCommandContext context)
        {
            EmbedHelper.GetJSONFromUserMessage(message, out JSONContainer json);
            IReadOnlyCollection<IAttachment> attachments = message.Attachments;

            bool pretty = options.Contains(ExecutionOptions.pretty);
            bool remove = options.Contains(ExecutionOptions.remove);

            EmbedBuilder embed;
            if (pretty)
            {

                embed = new EmbedBuilder()
                {
                    Color = BotCore.EmbedColor,
                    Title = $"Message JSON for original message in {guild.Name} - {channel.Name} by {message.Author}",
                    Description = ("```json\n" + json.Build(true).Replace("```", "[3`]")).MaxLength(EmbedHelper.EMBEDDESCRIPTION_MAX - 8) + "```",
                };
            }
            else
            {
                embed = new EmbedBuilder()
                {
                    Color = BotCore.EmbedColor,
                    Title = $"Message JSON for original message in {guild.Name} - {channel.Name} by {message.Author}",
                    Footer = new EmbedFooterBuilder()
                    {
                        Text = json.Build(false).MaxLength(EmbedHelper.EMBEDFOOTERTEXT_MAX)
                    }
                };
            }
            if (attachments.Count > 0)
            {
                StringBuilder attachments_str = new StringBuilder();
                foreach (IAttachment attachment in attachments)
                {
                    if (attachment.Url.IsValidImageURL() && string.IsNullOrEmpty(embed.ImageUrl))
                    {
                        embed.ImageUrl = attachment.Url;
                    }
                    attachments_str.AppendLine($"[{attachment.Filename}]({attachment.Url})");
                }
                embed.AddField("Attachments", attachments_str.ToString());
            }
            await context.Channel.SendEmbedAsync(embed);

            if (remove)
            {
                try
                {
                    await channel.DeleteMessageAsync(message);
                }
                catch (Exception e)
                {
                    await context.Channel.SendEmbedAsync($"Failed to remove the message. Probably missing permissions! Exception: {e.GetType()} - {e.Message}", true);
                }
            }
        }

        enum ExecutionOptions
        {
            pretty,
            remove
        }
    }

    #endregion
    #region replace

    class ReplaceEmbedCommand : Command
    {
        public override string Summary => "Edits a message to follow a new embedjson";
        public override string Remarks => "The message author has to be by the bot used to modify!";
        public override Argument[] Arguments => new Argument[] {
            new Argument("MessageLink", "A discord message link to select the source"),
            new Argument("EmbedJSON", "The embed, formatted as a JSON", multiple: true)
        };

        private IUserMessage message;
        private string messageContent;
        private EmbedBuilder embed;

        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;

        public override HandledContexts ExecutionMethod => HandledContexts.DMOnly;
        public override Precondition[] ExecutePreconditions => new Precondition[] { new IsOwnerOrAdminPrecondition() };
        public override Precondition[] ViewPreconditions => new Precondition[] { new IsOwnerOrAdminPrecondition() };


        public ReplaceEmbedCommand(string identifier, CommandCollection collection) : base()
        {
            Register(identifier, collection);
        }

        protected override async Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
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
                    message = await channel.GetMessageAsync(messageId) as IUserMessage;

                    if (message == null)
                    {
                        return new ArgumentParseResult(Arguments[0], "Found correct guild and correct channel, but not correct message! Has the message been deleted?");
                    }
                    else if (message.Author.Id != BotCore.Client.CurrentUser.Id)
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
                    return EmbedHelper.TryParseEmbedFromJSONObject(json, out embed, out messageContent);
                }
                else
                {
                    return new ArgumentParseResult(Arguments[1], $"Unable to parse JSON text to a json data structure! Error: `{errormessage}`");
                }
            }
            else
            {
                embed = null;
                return new ArgumentParseResult("Internal Error: " + Macros.GetCodeLocation());
            }
        }

        protected override async Task Execute(IDMCommandContext context)
        {
            EmbedHelper.GetJSONFromUserMessage(message, out JSONContainer json);

            await message.ModifyAsync(message =>
            {
                message.Content = messageContent; message.Embed = embed?.Build();
            });
            await context.Channel.SendEmbedAsync("Edit done!");
        }
    }

    #endregion
}