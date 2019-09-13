using System;
using System.Collections.Generic;
using System.Text;

namespace BotCoreNET.CommandHandling
{
    public class GuildCommandContext : GuildMessageContext, IGuildCommandContext
    {
        private readonly CommandContext commandContext;

        public GuildCommandContext(IGuildMessageContext context) : base(context.Message, context.Guild)
        {
            commandContext = new CommandContext(context.Content);
        }

        public string ArgumentSection => commandContext.ArgumentSection;

        public string[] Arguments => commandContext.Arguments;

        public int ArgumentCount => commandContext.ArgumentCount;

        public int ArgPointer { get => commandContext.ArgPointer; set => commandContext.ArgPointer = value; }

        public string Argument => commandContext.Argument;

        public Command InterpretedCommand => commandContext.InterpretedCommand;

        public CommandSearchResult CommandSearch => commandContext.CommandSearch;

        public bool IsGuildContext => true;

        public static bool TryConvert(IDMCommandContext context, out IGuildCommandContext guildContext)
        {
            guildContext = context as IGuildCommandContext;
            return context.IsGuildContext;
        }

        public string RemoveArgumentsFront(int count)
        {
            return commandContext.RemoveArgumentsFront(count);
        }
    }
}
