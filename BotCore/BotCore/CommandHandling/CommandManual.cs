using BotCoreNET.Helpers;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotCoreNET.CommandHandling
{
    static class CommandManual
    {
        #region Command Collection Help

        public static Task SendCommandCollectionHelp(IDMCommandContext context, CommandCollection collection, ISocketMessageChannel outputchannel = null)
        {
            EmbedBuilder embed = GetCommandCollectionEmbed(context, collection);
            if (outputchannel == null)
            {
                outputchannel = context.Channel;
            }
            return outputchannel.SendEmbedAsync(embed);
        }

        public static EmbedBuilder GetCommandCollectionEmbed(IDMCommandContext context, CommandCollection collection)
        {
            string contextType = "";
            if (GuildCommandContext.TryConvert(context, out IGuildCommandContext guildContext))
            {
                contextType = "Guild";
            }
            else
            {
                contextType = "PM";
            }

            string embedTitle = $"Command Collection \"{collection.Name}\"";
            string embedDesc = "This list only shows commands where all preconditions have been met!";

            List<EmbedFieldBuilder> helpFields = new List<EmbedFieldBuilder>();

            foreach (Command command in collection.Commands)
            {
                if (command.CanView(context, guildContext, context.UserInfo.IsBotAdmin, out _))
                {
                    helpFields.Add(Macros.EmbedField(command.Syntax, command.Summary, true));
                }
            }

            if (helpFields.Count == 0)
            {
                embedDesc = "No command's precondition has been met!";
                return new EmbedBuilder() { Title = embedTitle, Description = embedDesc, Color = BotCore.ErrorColor, Footer = new EmbedFooterBuilder() { Text = "Context: " + contextType } };
            }
            else
            {
                return new EmbedBuilder() { Title = embedTitle, Description = embedDesc, Color = BotCore.EmbedColor, Footer = new EmbedFooterBuilder() { Text = "Context: " + contextType }, Fields = helpFields };
            }
        }

        public static Task SendHelpList(IDMCommandContext context, ISocketMessageChannel outputchannel = null)
        {
            EmbedBuilder embed = GetHelpListEmbed(context);
            if (outputchannel == null)
            {
                outputchannel = context.Channel;
            }
            return outputchannel.SendEmbedAsync(embed);
        }

        public static EmbedBuilder GetHelpListEmbed(IDMCommandContext context)
        {
            string contextType = "";
            if (GuildCommandContext.TryConvert(context, out IGuildCommandContext guildContext))
            {
                contextType = "Guild";
            }
            else
            {
                contextType = "PM";
            }

            string embedTitle = "List of all Commands";
            string embedDesc = "This list only shows commands where all preconditions have been met!";

            List<EmbedFieldBuilder> helpFields = new List<EmbedFieldBuilder>();

            foreach (CommandCollection collection in CommandCollection.AllCollections)
            {
                bool collectionAllowed = true;
                if (context.IsGuildContext)
                {
                    collectionAllowed = guildContext.ChannelMeta.allowedCommandCollections.Count == 0 || guildContext.ChannelMeta.allowedCommandCollections.Contains(collection.Name);
                }
                int availableCommands = collection.ViewableCommands(context, guildContext);
                if (availableCommands > 0 && collectionAllowed)
                {
                    helpFields.Add(Macros.EmbedField($"Collection \"{collection.Name}\"", $"{availableCommands} commands.{(string.IsNullOrEmpty(collection.Description) ? string.Empty : $" {collection.Description}.")} Use `{MessageHandler.Prefix}man:{collection.Name}` to see a summary of commands in this command family!", true));
                }
            }

            foreach (Command command in CommandCollection.BaseCollection.Commands)
            {
                bool commandAllowed = true;
                if (context.IsGuildContext)
                {
                    commandAllowed = guildContext.ChannelMeta.CheckChannelMeta(guildContext.UserInfo, command, out _);
                }
                if (command.CanView(context, guildContext, context.UserInfo.IsBotAdmin, out _) && commandAllowed)
                {
                    helpFields.Add(Macros.EmbedField(command.Syntax, command.Summary, true));
                }
            }

            if (helpFields.Count == 0)
            {
                embedDesc = "No command's precondition has been met!";
                return new EmbedBuilder() { Title = embedTitle, Description = embedDesc, Color = BotCore.ErrorColor, Footer = new EmbedFooterBuilder() { Text = "Context: " + contextType } };
            }
            else
            {
                return new EmbedBuilder() { Title = embedTitle, Description = embedDesc, Color = BotCore.EmbedColor, Footer = new EmbedFooterBuilder() { Text = "Context: " + contextType }, Fields = helpFields };
            }
        }

        #endregion

        private static readonly EmbedFieldBuilder syntaxHelpField = new EmbedFieldBuilder() { Name = "Syntax Help", Value = "`key` = command identifier\n`<key>` = required argument\n`(key)` = optional argument\n`[key]` = multiple arguments possible" };

        public static Task SendCommandHelp(IDMCommandContext context, Command command, ISocketMessageChannel outputchannel = null)
        {
            EmbedBuilder embed = GetCommandEmbed(context, command);
            if (outputchannel == null)
            {
                outputchannel = context.Channel;
            }
            return outputchannel.SendEmbedAsync(embed);
        }

        public static EmbedBuilder GetCommandEmbed(IDMCommandContext context, Command command)
        {
            GuildCommandContext.TryConvert(context, out IGuildCommandContext guildContext);
            if (command.CanView(context, guildContext, context.UserInfo.IsBotAdmin, out List<string> failedPreconditionChecks))
            {
                EmbedBuilder embed = new EmbedBuilder()
                {
                    Title = $"Help For `{command.Syntax}`",
                    Color = BotCore.EmbedColor,
                    Description = $"Collection: `{command.Collection}`\n{command.Summary}",
                };
                if (command.Link != null)
                {
                    embed.AddField("Documentation", $"[Online Documentation for `{command.FullSyntax}`]({command.Link})");
                }
                if (command.Remarks != null)
                {
                    embed.AddField("Remarks", command.Remarks);
                }

                if (command.Arguments.Length > 0)
                {
                    string[] argumentInfo = new string[command.Arguments.Length];
                    for (int i = 0; i < command.Arguments.Length; i++)
                    {
                        Argument argument = command.Arguments[i];
                        argumentInfo[i] = $"**{UnicodeEmoteService.TraingleRight}`{argument}`**\n{argument.Help}";
                    }

                    embed.AddField("Syntax", Markdown.MultiLineCodeBlock(command.FullSyntax) + "\n" + string.Join("\n\n", argumentInfo));
                    embed.AddField(syntaxHelpField);

                    string contextType;
                    if (command.RequireGuildContext)
                    {
                        contextType = "Guild";
                    }
                    else
                    {
                        contextType = "PM";
                    }
                    embed.AddField("Access Requirements", $"\nRequired Execution Location `{contextType}`\n\n**Execution Preconditions**\n{command.ExecutePreconditions.Join("\n")}\n\n**ViewPreconditions**\n{command.ViewPreconditions.Join("\n")}");
                    embed.Footer = new EmbedFooterBuilder() { Text = "Context: " + contextType };
                }
                else
                {
                    embed.AddField("Syntax", Markdown.MultiLineCodeBlock(command.FullSyntax));
                }

                return embed;
            }
            else
            {
                return new EmbedBuilder()
                {
                    Title = $"Help For `{command.FullSyntax}`",
                    Description = $"**Failed view precondition check!**\n{failedPreconditionChecks.Join("\n")}",
                    Color = BotCore.ErrorColor
                };
            }
        }
    }
}
