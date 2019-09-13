using System;
using System.Collections.Generic;
using System.Text;

namespace BotCoreNET.CommandHandling
{
    public class DMCommandContext : MessageContext, IDMCommandContext
    {
        private readonly CommandContext commandContext;

        public DMCommandContext(IMessageContext context) : base(context.Message)
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

        public bool IsGuildContext => false;

        public string RemoveArgumentsFront(int count)
        {
            return commandContext.RemoveArgumentsFront(count);
        }

    }
}
