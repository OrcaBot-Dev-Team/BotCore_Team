using BotCoreNET.BotVars;
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
    internal class BotVarCommand : Command
    {
        public override HandledContexts ArgumentParserMethod => HandledContexts.DMOnly;
        public override HandledContexts ExecutionMethod => HandledContexts.DMOnly;
        public override string Summary => "View, set and delete config values";
        public override Argument[] Arguments => new Argument[]
        {
            new Argument("Context", "Id of the Discord Guild, keyword `global` to access global bot variables or keyword `save` to save all botvars"),
            new Argument("BotVar Id", "Id of the config value", optional:true),
            new Argument("Type", $"What type to parse a new value as. Available are `delete` (deletes instead of assigning a value), `{BotVarType.UInt64}`, `{BotVarType.Int64}`, " +
                $"`{BotVarType.Float64}`, `{BotVarType.String}`, `{BotVarType.Bool}`, `{BotVarType.Generic}` (JSON)", optional:true),
            new Argument("Value", "Value to assign", optional:true, multiple:true)
        };
        public override Precondition[] ViewPreconditions => new Precondition[] { new RequireBotAdminPrecondition() };
        public override Precondition[] ExecutePreconditions => new Precondition[] { new RequireBotAdminPrecondition() };

        internal BotVarCommand(string identifier, CommandCollection collection = null) : base()
        {
            Register(identifier, collection);
        }

        private class ArgumentContainer
        {
            public CommandMode mode;
            public BotVar BotVar;
            public BotVarType assignType;
            public string BotVarId;
            public string value;
            public BotVarCollection TargetBotVarCollection;
        }

        protected override Task<ArgumentParseResult> ParseArguments(IDMCommandContext context)
        {
            ArgumentContainer argOut = new ArgumentContainer();

            // Parse <Context> argument
            if (context.Arguments.First.ToLower() == "save")
            {
                argOut.mode = CommandMode.save;
                return Task.FromResult(new ArgumentParseResult(argOut));
            }
            else if (context.Arguments.First.ToLower() == "global")
            {
                argOut.TargetBotVarCollection = BotVarManager.GlobalBotVars;
            }
            else
            {
                if (!ArgumentParsing.TryParseGuild(context, context.Arguments.First, out SocketGuild guild))
                {
                    return Task.FromResult(new ArgumentParseResult(Arguments[0]));
                }
                argOut.TargetBotVarCollection = BotVarManager.GetGuildBotVarCollection(guild.Id);
            }

            context.Arguments.Index++;

            if (context.Arguments.TotalCount == 1) // If argcnt is 1 (only context provided), commandmode is to list all botvars
            {
                argOut.mode = CommandMode.list;
                return Task.FromResult(new ArgumentParseResult(argOut));
            }

            // Parse <BotVar Id> argument

            argOut.BotVarId = context.Arguments.First;
            argOut.TargetBotVarCollection.TryGetBotVar(argOut.BotVarId, out argOut.BotVar);

            if (context.Arguments.TotalCount == 2) // If argcnt is 2 (only context and BotVar id provided), commandmode is to display the requested botvar
            {
                argOut.mode = CommandMode.get;
                if (argOut.BotVar.IsDefined)
                {
                    return Task.FromResult(new ArgumentParseResult(argOut));
                }
                else
                {
                    return Task.FromResult(new ArgumentParseResult(Arguments[0], $"Couldn't locate a config variable named `{argOut.BotVarId}`!"));
                }
            }

            context.Arguments.Index++;

            // Parse argument <Type>

            if (!parseArgument_Type(context, argOut, out ArgumentParseResult failedTypeArgParse))
            {
                return Task.FromResult(failedTypeArgParse);
            }

            if (argOut.mode == CommandMode.delete) // No further parsing required as mode is delete
            {
                return Task.FromResult(new ArgumentParseResult(argOut));
            }

            if (context.Arguments.TotalCount == 3) // No value argument provided
            {
                return Task.FromResult(new ArgumentParseResult(Arguments[2], "Cannot assign an empty value!"));
            }

            context.Arguments.Index++;

            // Parse value argument
            if (!parseArgument_Value(context, argOut, out ArgumentParseResult failedValueArgParse))
            {
                return Task.FromResult(failedValueArgParse);
            }

            return Task.FromResult(new ArgumentParseResult(argOut));
        }

        private bool parseArgument_Type(IDMCommandContext  context, ArgumentContainer argOut, out ArgumentParseResult failedParse)
        {
            if (context.Arguments.First.ToLower() == "delete") // Type is to delete the value
            {
                argOut.mode = CommandMode.delete;
                if (argOut.BotVar.IsDefined)
                {
                    failedParse = null;
                    return true;
                }
                else
                {
                    failedParse = new ArgumentParseResult(Arguments[0], $"Couldn't locate a config variable named `{argOut.BotVarId}`!");
                    return false;
                }
            }
            else
            {
                argOut.mode = CommandMode.set;
            }

            if (!Enum.TryParse(context.Arguments.First, true, out argOut.assignType))
            {
                failedParse = new ArgumentParseResult(Arguments[2]);
                return false;
            }

            if (argOut.assignType == BotVarType.Undefined || argOut.assignType == BotVarType.Deleted)
            {
                failedParse = new ArgumentParseResult(Arguments[2]);
                return false;
            }

            failedParse = null;
            return true;
        }

        private bool parseArgument_Value(IDMCommandContext context, ArgumentContainer argOut, out ArgumentParseResult failedParse)
        {
            argOut.value = context.Arguments.First;

            switch (argOut.assignType)
            {
                case BotVarType.UInt64:
                    if (ulong.TryParse(argOut.value, out ulong uint64Val))
                    {
                        argOut.BotVar = new BotVar(argOut.BotVarId, uint64Val);
                    }
                    else
                    {
                        failedParse = new ArgumentParseResult(Arguments[3]);
                        return false;
                    }
                    break;
                case BotVarType.Int64:
                    if (long.TryParse(argOut.value, out long int64Val))
                    {
                        argOut.BotVar = new BotVar(argOut.BotVarId, int64Val);
                    }
                    else
                    {
                        failedParse = new ArgumentParseResult(Arguments[3]);
                        return false;
                    }
                    break;
                case BotVarType.Float64:
                    if (double.TryParse(argOut.value, out double float64Val))
                    {
                        argOut.BotVar = new BotVar(argOut.BotVarId, float64Val);
                    }
                    else
                    {
                        failedParse = new ArgumentParseResult(Arguments[3]);
                        return false;
                    }
                    break;
                case BotVarType.String:
                    argOut.BotVar = new BotVar(argOut.BotVarId, argOut.value);
                    break;
                case BotVarType.Bool:
                    if (bool.TryParse(argOut.value, out bool boolVal))
                    {
                        argOut.BotVar = new BotVar(argOut.BotVarId, boolVal);
                    }
                    else
                    {
                        failedParse = new ArgumentParseResult(Arguments[3]);
                        return false;
                    }
                    break;
                case BotVarType.Generic:
                    string json_str = context.RemoveArgumentsFront(3);
                    if (JSONContainer.TryParse(json_str, out JSONContainer json, out string error))
                    {
                        argOut.BotVar = new BotVar(argOut.BotVarId, json);
                    }
                    else
                    {
                        failedParse = new ArgumentParseResult(Arguments[3], error);
                        return false;
                    }
                    break;
                default:
                    failedParse = new ArgumentParseResult("Internal Error!");
                    return false;
            }
            failedParse = null;
            return true;
        }

        protected override Task Execute(IDMCommandContext context, object args)
        {
            ArgumentContainer argContainer = args as ArgumentContainer;
            EmbedFooterBuilder footer = null;
            if (argContainer.TargetBotVarCollection != null)
            {
                footer = new EmbedFooterBuilder() { Text = argContainer.TargetBotVarCollection.ToString() };
            }
            switch (argContainer.mode)
            {
                case CommandMode.save:
                    return execute_saveAllBotVars(context);
                case CommandMode.list:
                    return execute_listBotVars(context, argContainer);
                case CommandMode.get:
                    return execute_getBotVar(context, footer, argContainer);
                case CommandMode.set:
                    return execute_setBotVar(context, footer, argContainer);
                case CommandMode.delete:
                    return execute_deleteBotVar(context, argContainer);
                default:
                    return Task.CompletedTask;
            }
        }

        private Task execute_deleteBotVar(IDMCommandContext context, ArgumentContainer argContainer)
        {
            argContainer.TargetBotVarCollection.DeleteBotVar(argContainer.BotVarId);
            return context.Channel.SendEmbedAsync($"Deleted Bot Variable `{argContainer.BotVarId}`");
        }

        private Task execute_setBotVar(IDMCommandContext context, EmbedFooterBuilder footer, ArgumentContainer argContainer)
        {
            argContainer.TargetBotVarCollection.SetBotVar(argContainer.BotVar);
            EmbedBuilder setembed = new EmbedBuilder()
            {
                Color = BotCore.EmbedColor,
                Title = $"Set Bot Variable \"{argContainer.BotVarId}\" to:",
                Description = argContainer.BotVar.ToString(),
                Footer = footer
            };
            return context.Channel.SendEmbedAsync(setembed);
        }

        private Task execute_getBotVar(IDMCommandContext context, EmbedFooterBuilder footer, ArgumentContainer argContainer)
        {
            EmbedBuilder embed = new EmbedBuilder()
            {
                Color = BotCore.EmbedColor,
                Title = $"Bot Variable \"{argContainer.BotVarId}\"",
                Description = argContainer.BotVar.ToString(),
                Footer = footer
            };
            return context.Channel.SendEmbedAsync(embed);
        }

        private static async Task execute_saveAllBotVars(IDMCommandContext context)
        {
            await BotVarManager.SaveAllBotVars();
            await context.Channel.SendEmbedAsync("Saved all bot variables");
        }

        private Task execute_listBotVars(IDMCommandContext context, ArgumentContainer argContainer)
        {
            List<EmbedFieldBuilder> embedFields = argContainer.TargetBotVarCollection.GetBotVarList();
            if (embedFields.Count == 0)
            {
                return context.Channel.SendEmbedAsync(new EmbedBuilder() { Title = $"{argContainer.TargetBotVarCollection} - 0", Color = BotCore.EmbedColor, Description = "None" });
            }
            else
            {
                return context.Channel.SendSafeEmbedList($"{argContainer.TargetBotVarCollection} - {embedFields.Count}", embedFields);
            }
        }

        private enum CommandMode
        {
            save,
            list,
            get,
            set,
            delete
        }
    }
}
