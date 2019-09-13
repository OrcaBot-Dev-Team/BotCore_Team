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
            new Argument("ConfigValue Id", "Id of the config value. `save` to save all previous changes immediately", optional:true),
            new Argument("Type", $"What type to parse a new value as. Available are `delete` (deletes instead of assigning a value), `{BotVarType.UInt64}`, `{BotVarType.Int64}`, " +
                $"`{BotVarType.Float64}`, `{BotVarType.String}`, `{BotVarType.Bool}`, `{BotVarType.Generic}` (JSON)", optional:true),
            new Argument("Value", "Value to assign", optional:true, multiple:true)
        };
        public override Precondition[] ViewPreconditions => new Precondition[] { new RequireBotAdminPrecondition() };
        public override Precondition[] ExecutePreconditions => new Precondition[] { new RequireBotAdminPrecondition() };

        internal BotVarCommand() : base()
        {
            Register("botvar", null);
        }

        private CommandMode mode;
        private BotVar BotVar;
        private BotVarType assignType;
        private string BotVarId;
        private string value;

        protected override Task<ArgumentParseResult> ParseArguments(IDMCommandContext context)
        {
            if (context.ArgumentCount == 0)
            {
                mode = CommandMode.listall;
                return Task.FromResult(ArgumentParseResult.DefaultNoArguments);
            }

            if (context.Argument.ToLower() == "save")
            {
                mode = CommandMode.save;
                return Task.FromResult(ArgumentParseResult.SuccessfullParse);
            }

            BotVarId = context.Argument;
            BotVarManager.TryGetBotVar(BotVarId, out BotVar);

            if (context.ArgumentCount == 1)
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
            else
            {
                mode = CommandMode.set;
            }

            context.ArgPointer++;

            if (context.Argument.ToLower() == "delete")
            {
                mode = CommandMode.delete;
                if (!BotVar.IsDefined)
                {
                    return Task.FromResult(new ArgumentParseResult(Arguments[0], $"Couldn't locate a config variable named `{BotVarId}`!"));
                }
                else
                {
                    return Task.FromResult(ArgumentParseResult.SuccessfullParse);
                }
            }

            if (context.ArgumentCount == 2)
            {
                return Task.FromResult(new ArgumentParseResult(Arguments[2], "Cannot assign an empty value!"));
            }

            if (!Enum.TryParse(context.Argument, true, out assignType))
            {
                return Task.FromResult(new ArgumentParseResult(Arguments[1]));
            }

            if (assignType == BotVarType.Undefined)
            {
                return Task.FromResult(new ArgumentParseResult(Arguments[1]));
            }

            context.ArgPointer++;

            value = context.Argument;

            switch (assignType)
            {
                case BotVarType.UInt64:
                    if (ulong.TryParse(value, out ulong uint64Val))
                    {
                        BotVar = new BotVar(BotVarId, uint64Val);
                    }
                    else
                    {
                        return Task.FromResult(new ArgumentParseResult(Arguments[2]));
                    }
                    break;
                case BotVarType.Int64:
                    if (long.TryParse(value, out long int64Val))
                    {
                        BotVar = new BotVar(BotVarId, int64Val);
                    }
                    else
                    {
                        return Task.FromResult(new ArgumentParseResult(Arguments[2]));
                    }
                    break;
                case BotVarType.Float64:
                    if (double.TryParse(value, out double float64Val))
                    {
                        BotVar = new BotVar(BotVarId, float64Val);
                    }
                    else
                    {
                        return Task.FromResult(new ArgumentParseResult(Arguments[2]));
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
                        return Task.FromResult(new ArgumentParseResult(Arguments[2]));
                    }
                    break;
                case BotVarType.Generic:
                    if (JSONContainer.TryParse(value, out JSONContainer json, out string error))
                    {
                        BotVar = new BotVar(BotVarId, json);
                    }
                    else
                    {
                        return Task.FromResult(new ArgumentParseResult(Arguments[2], error));
                    }
                    break;
                default:
                    return Task.FromResult(new ArgumentParseResult(Arguments[2]));
            }

            return Task.FromResult(ArgumentParseResult.SuccessfullParse);
        }

        protected override async Task Execute(IDMCommandContext context)
        {
            switch (mode)
            {
                case CommandMode.save:
                    await BotVarManager.SaveBotVars();
                    await context.Channel.SendEmbedAsync($"Saved all config variables!");
                    break;
                case CommandMode.listall:
                    List<EmbedFieldBuilder> embedFields = BotVarManager.GetBotVarList();
                    if (embedFields.Count == 0)
                    {
                        await context.Channel.SendEmbedAsync(new EmbedBuilder() { Title = $"Config Variables - 0", Color = BotCore.EmbedColor, Description = "None" });
                    }
                    else
                    {
                        await context.Channel.SendSafeEmbedList($"Config Variables - {embedFields.Count}", embedFields);
                    }
                    break;
                case CommandMode.get:
                    EmbedBuilder embed = new EmbedBuilder()
                    {
                        Color = BotCore.EmbedColor,
                        Title = $"Config Variable \"{BotVarId}\"",
                        Description = BotVar.ToString()
                    };
                    await context.Channel.SendEmbedAsync(embed);
                    break;
                case CommandMode.set:
                    EmbedBuilder setembed = new EmbedBuilder()
                    {
                        Color = BotCore.EmbedColor,
                        Title = $"Set Config Variable \"{BotVarId}\" to:",
                        Description = BotVar.ToString()
                    };
                    BotVarManager.SetBotVar(BotVar);
                    await context.Channel.SendEmbedAsync(setembed);
                    break;
                case CommandMode.delete:
                    BotVarManager.DeleteBotVar(BotVarId);
                    await context.Channel.SendEmbedAsync($"Deleted Config Variable `{BotVarId}`");
                    break;
            }
        }

        private enum CommandMode
        {
            listall,
            get,
            set,
            delete,
            save
        }
    }

    internal class GuildBotVarCommand : Command
    {
        public override HandledContexts ArgumentParserMethod => HandledContexts.DMOnly;
        public override HandledContexts ExecutionMethod => HandledContexts.DMOnly;
        public override string Summary => "View, set and delete config values";
        public override Argument[] Arguments => new Argument[]
        {
            new Argument("Guild", "Id of the Discord Guild (commonly referred to as Server) to set a config value for"),
            new Argument("ConfigValue Id", "Id of the config value. `save` to save all previous changes immediately", optional:true),
            new Argument("Type", $"What type to parse a new value as. Available are `delete` (deletes instead of assigning a value), `{BotVarType.UInt64}`, `{BotVarType.Int64}`, " +
                $"`{BotVarType.Float64}`, `{BotVarType.String}`, `{BotVarType.Bool}`, `{BotVarType.Generic}` (JSON)", optional:true),
            new Argument("Value", "Value to assign", optional:true, multiple:true)
        };
        public override Precondition[] ViewPreconditions => new Precondition[] { new RequireBotAdminPrecondition() };
        public override Precondition[] ExecutePreconditions => new Precondition[] { new RequireBotAdminPrecondition() };

        internal GuildBotVarCommand() : base()
        {
            Register("guildbotvar", null);
        }

        private CommandMode mode;
        private BotVar BotVar;
        private BotVarType assignType;
        private string BotVarId;
        private string value;
        private GuildBotVarCollection BotVarCollection;

        protected override Task<ArgumentParseResult> ParseArguments(IDMCommandContext context)
        {
            if (!ArgumentParsing.TryParseGuild(context, context.Argument, out SocketGuild guild))
            {
                return Task.FromResult(new ArgumentParseResult(Arguments[0]));
            }
            BotVarCollection = BotVarManager.GetGuildBotVarCollection(guild.Id);

            context.ArgPointer++;

            if (context.ArgumentCount == 1)
            {
                mode = CommandMode.listall;
                return Task.FromResult(ArgumentParseResult.DefaultNoArguments);
            }

            BotVarId = context.Argument;
            BotVarCollection.TryGetBotVar(BotVarId, out BotVar);

            if (context.ArgumentCount == 2)
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
            else
            {
                mode = CommandMode.set;
            }

            context.ArgPointer++;

            if (context.Argument.ToLower() == "delete")
            {
                mode = CommandMode.delete;
                if (!BotVar.IsDefined)
                {
                    return Task.FromResult(new ArgumentParseResult(Arguments[0], $"Couldn't locate a config variable named `{BotVarId}`!"));
                }
                else
                {
                    return Task.FromResult(ArgumentParseResult.SuccessfullParse);
                }
            }

            if (context.ArgumentCount == 2)
            {
                return Task.FromResult(new ArgumentParseResult(Arguments[2], "Cannot assign an empty value!"));
            }

            if (!Enum.TryParse(context.Argument, true, out assignType))
            {
                return Task.FromResult(new ArgumentParseResult(Arguments[1]));
            }

            if (assignType == BotVarType.Undefined)
            {
                return Task.FromResult(new ArgumentParseResult(Arguments[1]));
            }

            context.ArgPointer++;

            value = context.Argument;

            switch (assignType)
            {
                case BotVarType.UInt64:
                    if (ulong.TryParse(value, out ulong uint64Val))
                    {
                        BotVar = new BotVar(BotVarId, uint64Val);
                    }
                    else
                    {
                        return Task.FromResult(new ArgumentParseResult(Arguments[2]));
                    }
                    break;
                case BotVarType.Int64:
                    if (long.TryParse(value, out long int64Val))
                    {
                        BotVar = new BotVar(BotVarId, int64Val);
                    }
                    else
                    {
                        return Task.FromResult(new ArgumentParseResult(Arguments[2]));
                    }
                    break;
                case BotVarType.Float64:
                    if (double.TryParse(value, out double float64Val))
                    {
                        BotVar = new BotVar(BotVarId, float64Val);
                    }
                    else
                    {
                        return Task.FromResult(new ArgumentParseResult(Arguments[2]));
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
                        return Task.FromResult(new ArgumentParseResult(Arguments[2]));
                    }
                    break;
                case BotVarType.Generic:
                    if (JSONContainer.TryParse(value, out JSONContainer json, out string error))
                    {
                        BotVar = new BotVar(BotVarId, json);
                    }
                    else
                    {
                        return Task.FromResult(new ArgumentParseResult(Arguments[2], error));
                    }
                    break;
                default:
                    return Task.FromResult(new ArgumentParseResult(Arguments[2]));
            }

            return Task.FromResult(ArgumentParseResult.SuccessfullParse);
        }

        protected override async Task Execute(IDMCommandContext context)
        {
            switch (mode)
            {
                case CommandMode.listall:
                    List<EmbedFieldBuilder> embedFields = BotVarCollection.GetBotVarList();
                    if (embedFields.Count == 0)
                    {
                        await context.Channel.SendEmbedAsync(new EmbedBuilder() { Title = $"Config Variables for guild {BotVarCollection.GuildID} - 0", Color = BotCore.EmbedColor, Description = "None" });
                    }
                    else
                    {
                        await context.Channel.SendSafeEmbedList($"Config Variables for guild {BotVarCollection.GuildID} - {embedFields.Count}", embedFields);
                    }
                    break;
                case CommandMode.get:
                    EmbedBuilder embed = new EmbedBuilder()
                    {
                        Color = BotCore.EmbedColor,
                        Title = $"Config Variable \"{BotVarId}\"",
                        Description = BotVar.ToString()
                    };
                    await context.Channel.SendEmbedAsync(embed);
                    break;
                case CommandMode.set:
                    EmbedBuilder setembed = new EmbedBuilder()
                    {
                        Color = BotCore.EmbedColor,
                        Title = $"Set Config Variable \"{BotVarId}\" to:",
                        Description = BotVar.ToString()
                    };
                    BotVarCollection.SetBotVar(BotVar);
                    await context.Channel.SendEmbedAsync(setembed);
                    break;
                case CommandMode.delete:
                    BotVarCollection.DeleteBotVar(BotVarId);
                    await context.Channel.SendEmbedAsync($"Deleted Config Variable `{BotVarId}`");
                    break;
            }
        }

        private enum CommandMode
        {
            listall,
            get,
            set,
            delete
        }
    }
}
