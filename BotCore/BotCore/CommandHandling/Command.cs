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

        protected virtual Task Execute(IDMCommandContext context)
        {
            throw new UnpopulatedMethodException();
        }

        protected virtual Task ExecuteGuild(IGuildCommandContext context)
        {
            throw new UnpopulatedMethodException();
        }

        internal async Task HandleCommandAsync(IDMCommandContext context, IGuildCommandContext guildContext)
        {
            string stage = "Checking Preconditions";
            try
            {
                if (CanExecute(context, guildContext, context.UserInfo.IsBotAdmin, out List<string> errors))
                {
                    stage = "Parsing Arguments";
                    ArgumentParseResult parseResult = await parseArguments(context, guildContext);
                    if (!parseResult.Success)
                    {
                        EmbedBuilder parseFailed = new EmbedBuilder()
                        {
                            Color = BotCore.ErrorColor,
                            Title = "Argument Parsing Failed!",
                            Description = parseResult.ToString()
                        };
                        await context.Channel.SendEmbedAsync(parseFailed);
                    }
                    else
                    {
                        stage = "Executing Command";
                        await execute(context, guildContext);
                    }
                }
                else
                {
                    EmbedBuilder embed = new EmbedBuilder()
                    {
                        Title = "Command Execution Failed",
                        Color = BotCore.ErrorColor,
                        Description = errors.Join("\n")
                    };
                    await context.Channel.SendEmbedAsync(embed);
                }
            }
            catch (Exception e)
            {
                await context.Channel.SendMessageAsync($"Exception at stage `{stage}`", embed: Macros.EmbedFromException(e).Build());
            }
        }

        private Task execute(IDMCommandContext context, IGuildCommandContext guildContext)
        {
            if (RunInAsyncMode)
            {
                switch (ExecutionMethod)
                {
                    case HandledContexts.None:
                        return context.Channel.SendEmbedAsync("INTERNAL ERROR", true);
                    case HandledContexts.DMOnly:
                        AsyncCommandContainer.NewAsyncCommand(Execute, context);
                        break;
                    case HandledContexts.GuildOnly:
                        AsyncCommandContainer.NewAsyncCommand(ExecuteGuild, guildContext);
                        break;
                    case HandledContexts.Both:
                        if (context.IsGuildContext)
                        {
                            AsyncCommandContainer.NewAsyncCommand(ExecuteGuild, guildContext);
                        }
                        else
                        {
                            AsyncCommandContainer.NewAsyncCommand(Execute, context);
                        }
                        break;
                }
                return Task.CompletedTask;
            }
            else
            {
                switch (ExecutionMethod)
                {
                    case HandledContexts.None:
                        return context.Channel.SendEmbedAsync("INTERNAL ERROR", true);
                    case HandledContexts.DMOnly:
                        return Execute(context);
                    case HandledContexts.GuildOnly:
                        return ExecuteGuild(guildContext);
                    case HandledContexts.Both:
                        if (context.IsGuildContext)
                        {
                            return ExecuteGuild(guildContext);
                        }
                        else
                        {
                            return Execute(context);
                        }
                }
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
                if (precondition.RequireGuild)
                {
                    if (!precondition.PreconditionCheckGuild(guildContext, out string error))
                    {
                        if (!precondition.OverrideAsBotadmin || !precondition.OverrideAsBotadmin)
                        {
                            errors.Add(error);
                        }
                    }
                }
                else
                {
                    if (!precondition.PreconditionCheck(context, out string error))
                    {
                        if (!precondition.OverrideAsBotadmin || !precondition.OverrideAsBotadmin)
                        {
                            errors.Add(error);
                        }
                    }
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
}
