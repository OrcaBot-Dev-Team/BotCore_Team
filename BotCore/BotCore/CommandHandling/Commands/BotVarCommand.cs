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

        private CommandMode mode;
        private BotVar BotVar;
        private BotVarType assignType;
        private string BotVarId;
        private string value;
        private BotVarCollection TargetBotVarCollection;

        protected override Task<ArgumentParseResult> ParseArguments(IDMCommandContext context)
        {
            // Parse <Context> argument
            if (context.Arguments.First.ToLower() == "save")
            {
                mode = CommandMode.save;
                return Task.FromResult(ArgumentParseResult.SuccessfullParse);
            }
            else if (context.Arguments.First.ToLower() == "global")
            {
                TargetBotVarCollection = BotVarManager.GlobalBotVars;
            }
            else
            {
                if (!ArgumentParsing.TryParseGuild(context, context.Arguments.First, out SocketGuild guild))
                {
                    return Task.FromResult(new ArgumentParseResult(Arguments[0]));
                }
                TargetBotVarCollection = BotVarManager.GetGuildBotVarCollection(guild.Id);
            }

            context.Arguments.Index++;

            if (context.Arguments.TotalCount == 1) // If argcnt is 1 (only context provided), commandmode is to list all botvars
            {
                mode = CommandMode.listall;
                return Task.FromResult(ArgumentParseResult.SuccessfullParse);
            }

            // Parse <BotVar Id> argument

            BotVarId = context.Arguments.First;
            TargetBotVarCollection.TryGetBotVar(BotVarId, out BotVar);

            if (context.Arguments.TotalCount == 2) // If argcnt is 2 (only context and BotVar id provided), commandmode is to display the requested botvar
            {
                mode = CommandMode.get;
                if (BotVar.IsDefined)
                {
                    return Task.FromResult(ArgumentParseResult.SuccessfullParse);
                }
                else
                {
                    return Task.FromResult(new ArgumentParseResult(Arguments[0], $"Couldn't locate a config variable named `{BotVarId}`!"));
                }
            }

            context.Arguments.Index++;

            // Parse argument <Type>

            if (!parseArgument_Type(context, out ArgumentParseResult failedTypeArgParse))
            {
                return Task.FromResult(failedTypeArgParse);
            }

            if (mode == CommandMode.delete) // No further parsing required as mode is delete
            {
                return Task.FromResult(ArgumentParseResult.SuccessfullParse);
            }

            if (context.Arguments.TotalCount == 3) // No value argument provided
            {
                return Task.FromResult(new ArgumentParseResult(Arguments[2], "Cannot assign an empty value!"));
            }

            context.Arguments.Index++;

            // Parse value argument
            if (!parseArgument_Value(context, out ArgumentParseResult failedValueArgParse))
            {
                return Task.FromResult(failedValueArgParse);
            }

            return Task.FromResult(ArgumentParseResult.SuccessfullParse);
        }

        private bool parseArgument_Type(IDMCommandContext  context, out ArgumentParseResult failedParse)
        {
            if (context.Arguments.First.ToLower() == "delete") // Type is to delete the value
            {
                mode = CommandMode.delete;
                if (BotVar.IsDefined)
                {
                    failedParse = null;
                    return true;
                }
                else
                {
                    failedParse = new ArgumentParseResult(Arguments[0], $"Couldn't locate a config variable named `{BotVarId}`!");
                    return false;
                }
            }
            else
            {
                mode = CommandMode.set;
            }

            if (!Enum.TryParse(context.Arguments.First, true, out assignType))
            {
                failedParse = new ArgumentParseResult(Arguments[2]);
                return false;
            }

            if (assignType == BotVarType.Undefined || assignType == BotVarType.Deleted)
            {
                failedParse = new ArgumentParseResult(Arguments[2]);
                return false;
            }

            failedParse = null;
            return true;
        }

        private bool parseArgument_Value(IDMCommandContext context, out ArgumentParseResult failedParse)
        {
            value = context.Arguments.First;

            switch (assignType)
            {
                case BotVarType.UInt64:
                    if (ulong.TryParse(value, out ulong uint64Val))
                    {
                        BotVar = new BotVar(BotVarId, uint64Val);
                    }
                    else
                    {
                        failedParse = new ArgumentParseResult(Arguments[3]);
                        return false;
                    }
                    break;
                case BotVarType.Int64:
                    if (long.TryParse(value, out long int64Val))
                    {
                        BotVar = new BotVar(BotVarId, int64Val);
                    }
                    else
                    {
                        failedParse = new ArgumentParseResult(Arguments[3]);
                        return false;
                    }
                    break;
                case BotVarType.Float64:
                    if (double.TryParse(value, out double float64Val))
                    {
                        BotVar = new BotVar(BotVarId, float64Val);
                    }
                    else
                    {
                        failedParse = new ArgumentParseResult(Arguments[3]);
                        return false;
                    }
                    break;
                case BotVarType.String:
                    BotVar = new BotVar(BotVarId, value);
                    break;
                case BotVarType.Bool:
                    if (bool.TryParse(value, out bool boolVal))
                    {
                        BotVar = new BotVar(BotVarId, boolVal);
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
                        BotVar = new BotVar(BotVarId, json);
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

        protected override async Task Execute(IDMCommandContext context)
        {
            EmbedFooterBuilder footer = new EmbedFooterBuilder() { Text = TargetBotVarCollection.ToString() };
            switch (mode)
            {
                case CommandMode.save:
                    await BotVarManager.SaveAllBotVars();
                    await context.Channel.SendEmbedAsync("Saved all bot variables");
                    break;
                case CommandMode.listall:
                    List<EmbedFieldBuilder> embedFields = TargetBotVarCollection.GetBotVarList();
                    if (embedFields.Count == 0)
                    {
                        await context.Channel.SendEmbedAsync(new EmbedBuilder() { Title = $"{TargetBotVarCollection} - 0", Color = BotCore.EmbedColor, Description = "None" });
                    }
                    else
                    {
                        await context.Channel.SendSafeEmbedList($"{TargetBotVarCollection} - {embedFields.Count}", embedFields);
                    }
                    break;
                case CommandMode.get:
                    EmbedBuilder embed = new EmbedBuilder()
                    {
                        Color = BotCore.EmbedColor,
                        Title = $"Bot Variable \"{BotVarId}\"",
                        Description = BotVar.ToString(),
                        Footer = footer
                    };
                    await context.Channel.SendEmbedAsync(embed);
                    break;
                case CommandMode.set:
                    TargetBotVarCollection.SetBotVar(BotVar);
                    EmbedBuilder setembed = new EmbedBuilder()
                    {
                        Color = BotCore.EmbedColor,
                        Title = $"Set Bot Variable \"{BotVarId}\" to:",
                        Description = BotVar.ToString(),
                        Footer = footer
                    };
                    await context.Channel.SendEmbedAsync(setembed);
                    break;
                case CommandMode.delete:
                    TargetBotVarCollection.DeleteBotVar(BotVarId);
                    await context.Channel.SendEmbedAsync($"Deleted Bot Variable `{BotVarId}`");
                    break;
            }
        }

        private enum CommandMode
        {
            save,
            listall,
            get,
            set,
            delete
        }
    }
}
