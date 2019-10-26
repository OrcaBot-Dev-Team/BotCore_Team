using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BotCoreNET.CommandHandling
{
    public abstract class Command
    {
        private const int UNLIMITEDARGS = int.MaxValue;

        // Set when inheriting
        public virtual Precondition[] ExecutePreconditions => new Precondition[0];
        public virtual Precondition[] ViewPreconditions => new Precondition[0];
        public virtual Argument[] Arguments => new Argument[0];
        public abstract HandledContexts ArgumentParserMethod { get; }
        public abstract HandledContexts ExecutionMethod { get; }
        public abstract string Summary { get; }
        public virtual string Remarks => null;
        public virtual string Link => null;
        public virtual bool RunInAsyncMode => false;
        public virtual bool IsShitposting => false;


        // Set by constructor or Setup call
        public string Identifier { get; private set; }
        public int MinimumArgumentCount { get; private set; }
        public int MaximumArgumentCount { get; private set; }
        public bool RequireGuildContext { get; private set; }
        public CommandCollection Collection { get; internal set; }
        public string Syntax => MessageHandler.CommandParser.CommandSyntax(Identifier);

        // Active properties
        public string FullSyntax => MessageHandler.CommandParser.CommandSyntax(Identifier, Arguments);

        protected void Register(string identifier, CommandCollection collection = null)
        {
            Identifier = identifier;
            if (Setup())
            {
                CommandCollection.AddCommand(this, collection);
            }
            else
            {
                throw new Exception("Invalid overrides detected!");
            }
        }

        private bool Setup()
        {
            if (ExecutePreconditions == null || ViewPreconditions == null || Arguments == null || Summary == null || ExecutionMethod == HandledContexts.None)
            {
                return false;
            }
            else
            {
                if (Arguments.Length == 0)
                {
                    MinimumArgumentCount = 0;
                    MaximumArgumentCount = 0;
                }
                else
                {
                    MaximumArgumentCount = Arguments.Length;
                    bool lastArgOptional = true;
                    for (int i = Arguments.Length - 1; i >= 0; i--)
                    {
                        if (lastArgOptional && !Arguments[i].Optional)
                        {
                            lastArgOptional = false;
                            MinimumArgumentCount = i + 1;
                        }
                        if (Arguments[i].Multiple)
                        {
                            MaximumArgumentCount = UNLIMITEDARGS;
                        }
                    }
                }

                RequireGuildContext = ArgumentParserMethod == HandledContexts.GuildOnly || ExecutionMethod == HandledContexts.GuildOnly || ExecutePreconditions.Any(cond => { return cond.RequireGuild; }) || ViewPreconditions.Any(cond => { return cond.RequireGuild; });

                return true;
            }
        }

        protected virtual Task<ArgumentParseResult> ParseArguments(IDMCommandContext context)
        {
            throw new UnpopulatedMethodException();
        }

        protected virtual Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            throw new UnpopulatedMethodException();
        }

        protected virtual Task Execute(IDMCommandContext context, object parsedArgs)
        {
            throw new UnpopulatedMethodException();
        }

        protected virtual Task ExecuteGuild(IGuildCommandContext context, object parsedArgs)
        {
            throw new UnpopulatedMethodException();
        }



        internal async Task HandleCommandAsync(IDMCommandContext context, IGuildCommandContext guildContext, bool runningAsync = false)
        {
            string stage = "Checking Preconditions";
            try
            {
                if (CanExecute(context, guildContext, context.UserInfo.IsBotAdmin, out List<string> errors))
                {
                    stage = "Parsing Arguments";
                    ArgumentParseResult parseResult = await parseArguments(context, guildContext);
                    if (parseResult.Success)
                    {
                        stage = "Executing Command";
                        await execute(context, guildContext, parseResult.ParseResult);
                    }
                    else
                    {
                        await context.Channel.SendEmbedAsync(new EmbedBuilder()
                        {
                            Color = BotCore.ErrorColor,
                            Title = "Argument Parsing Failed!",
                            Description = parseResult.ToString()
                        });
                    }
                }
                else
                {
                    await context.Channel.SendEmbedAsync(new EmbedBuilder()
                    {
                        Title = "Command Execution Failed",
                        Color = BotCore.ErrorColor,
                        Description = errors.Join("\n")
                    });
                }
            }
            catch (Exception e)
            {
                await context.Channel.SendMessageAsync($"Exception at stage `{stage}`", embed: Macros.EmbedFromException(e).Build());
            }
        }

        private Task execute(IDMCommandContext context, IGuildCommandContext guildContext, object parsedArgs)
        {
            if (RunInAsyncMode)
                return executeAsyncMode(context, guildContext, parsedArgs);
            else
            {
                return executeSyncMode(context, guildContext, parsedArgs);
            }
        }

        private Task executeSyncMode(IDMCommandContext context, IGuildCommandContext guildContext, object parsedArgs)
        {
            switch (ExecutionMethod)
            {
                case HandledContexts.None:
                    return context.Channel.SendEmbedAsync("INTERNAL ERROR", true);
                case HandledContexts.DMOnly:
                    return Execute(context, parsedArgs);
                case HandledContexts.GuildOnly:
                    return ExecuteGuild(guildContext, parsedArgs);
                case HandledContexts.Both:
                    if (context.IsGuildContext)
                    {
                        return ExecuteGuild(guildContext, parsedArgs);
                    }
                    else
                    {
                        return Execute(context, parsedArgs);
                    }
                default:
                    return Task.CompletedTask;
            }
        }

        private Task executeAsyncMode(IDMCommandContext context, IGuildCommandContext guildContext, object parsedArgs)
        {
            switch (ExecutionMethod)
            {
                case HandledContexts.None:
                    return context.Channel.SendEmbedAsync("INTERNAL ERROR", true);
                case HandledContexts.DMOnly:
                    AsyncCommandContainer.NewAsyncCommand(Execute, context, parsedArgs);
                    break;
                case HandledContexts.GuildOnly:
                    AsyncCommandContainer.NewAsyncCommand(ExecuteGuild, guildContext, parsedArgs);
                    break;
                case HandledContexts.Both:
                    if (context.IsGuildContext)
                    {
                        AsyncCommandContainer.NewAsyncCommand(ExecuteGuild, guildContext, parsedArgs);
                    }
                    else
                    {
                        AsyncCommandContainer.NewAsyncCommand(Execute, context, parsedArgs);
                    }
                    break;
            }
            return Task.CompletedTask;
        }

        private Task<ArgumentParseResult> parseArguments(IDMCommandContext context, IGuildCommandContext guildContext)
        {
            switch (ArgumentParserMethod)
            {
                case HandledContexts.None:
                    return Task.FromResult(ArgumentParseResult.DefaultNoArguments);
                case HandledContexts.DMOnly:
                    return ParseArguments(context);
                case HandledContexts.GuildOnly:
                    return ParseArgumentsGuildAsync(guildContext);
                case HandledContexts.Both:
                    if (context.IsGuildContext)
                    {
                        return ParseArgumentsGuildAsync(guildContext);
                    }
                    else
                    {
                        return ParseArguments(context);
                    }
            }
            return Task.FromResult(new ArgumentParseResult("INTERNAL ERROR"));
        }

        public bool CanExecute(IDMCommandContext context, IGuildCommandContext guildContext, bool isBotAdmin, out List<string> errors)
        {
            return CheckPreconditions(ExecutePreconditions, context, guildContext, isBotAdmin, out errors);
        }

        public bool CanView(IDMCommandContext context, IGuildCommandContext guildContext, bool isBotAdmin, out List<string> errors)
        {
            return CheckPreconditions(ViewPreconditions, context, guildContext, isBotAdmin, out errors);
        }

        private bool CheckPreconditions(Precondition[] preconditions, IDMCommandContext context, IGuildCommandContext guildContext, bool isBotAdmin, out List<string> errors)
        {
            errors = new List<string>();
            if (!context.IsGuildContext && RequireGuildContext)
            {
                errors.Add("This command can not be used in DM channels!");
                return false;
            }

            foreach (Precondition precondition in preconditions)
            {
                bool check;
                string error;
                if (precondition.RequireGuild)
                {
                    check = precondition.PreconditionCheckGuild(guildContext, out error);
                }
                else
                {
                    check = precondition.PreconditionCheck(context, out error);
                }
                if (!check && !(precondition.OverrideAsBotadmin && isBotAdmin))
                {
                    errors.Add(error);
                }
            }

            return errors.Count == 0;
        }

        public override string ToString()
        {
            return FullSyntax;
        }
    }
    public enum HandledContexts
    {
        /// <summary>
        /// No method has to be called
        /// </summary>
        None,
        /// <summary>
        /// Only the DM context method has been overridden, and will be called from guild contexts aswell
        /// </summary>
        DMOnly,
        /// <summary>
        /// Only the guild context method has been overridden, preventing the command from functioning in a DM context
        /// </summary>
        GuildOnly,
        /// <summary>
        /// Both DM and guild context methods have been overridden, and the respective fit is called depending on the context
        /// </summary>
        Both
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    [Obsolete]
    public sealed class CommandAttribute : Attribute
    {
        private const int UNLIMITEDARGS = int.MaxValue;

        public readonly Precondition[] ExecutePreconditions;
        public readonly Precondition[] ViewPreconditions;
        public readonly Argument[] Arguments;
        public readonly HandledContexts ArgumentParserMethod;
        public readonly HandledContexts ExecutionMethod;
        public readonly string Identifier;
        public readonly string Summary;
        public readonly string Remarks;
        public readonly string Link;
        public readonly bool RunInAsyncMode;
        public readonly bool IsShitposting;
        public readonly int MinimumArgumentCount;
        public readonly int MaximumArgumentCount;
        public readonly bool RequireGuildContext;
        public readonly CommandCollection Collection;

        // Properties
        public string Syntax => MessageHandler.CommandParser.CommandSyntax(Identifier);
        public string FullSyntax => MessageHandler.CommandParser.CommandSyntax(Identifier, Arguments);

        public CommandAttribute(CommandProperties properties)
        {
            if (properties.ExecutePreconditions == null || properties.ViewPreconditions == null || properties.Arguments == null || properties.Summary == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            Identifier = properties.Identifier;
            Summary = properties.Summary;
            Remarks = properties.Remarks;
            Link = properties.Link;
            RunInAsyncMode = properties.RunInAsyncMode;
            IsShitposting = properties.IsShitposting;

            if (Arguments.Length == 0)
            {
                MinimumArgumentCount = 0;
                MaximumArgumentCount = 0;
            }
            else
            {
                MaximumArgumentCount = Arguments.Length;
                bool lastArgOptional = true;
                for (int i = Arguments.Length - 1; i >= 0; i--)
                {
                    if (lastArgOptional && !Arguments[i].Optional)
                    {
                        lastArgOptional = false;
                        MinimumArgumentCount = i + 1;
                    }
                    if (Arguments[i].Multiple)
                    {
                        MaximumArgumentCount = UNLIMITEDARGS;
                    }
                }
            }

            RequireGuildContext = ArgumentParserMethod == HandledContexts.GuildOnly || ExecutionMethod == HandledContexts.GuildOnly || ExecutePreconditions.Any(cond => { return cond.RequireGuild; }) || ViewPreconditions.Any(cond => { return cond.RequireGuild; });
        }

        [Obsolete]
        public class CommandProperties
        {
            public static string test;

            public readonly string Identifier;
            public readonly string Summary;
            public readonly string Collection;

            public Argument[] Arguments = new Argument[0];
            public Precondition[] ExecutePreconditions = new Precondition[0];
            public Precondition[] ViewPreconditions = new Precondition[0];
            public string Remarks = null;
            public string Link = null;
            public bool RunInAsyncMode = false;
            public bool IsShitposting = false;

            public CommandProperties(string identifier, string summary, string collection = null)
            {
                Identifier = identifier;
                Summary = summary;
                Collection = collection;


            }
        }
    }
}
