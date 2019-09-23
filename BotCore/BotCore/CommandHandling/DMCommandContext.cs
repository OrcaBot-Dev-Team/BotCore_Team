namespace BotCoreNET.CommandHandling
{
    public class DMCommandContext : MessageContext, IDMCommandContext
    {
        private readonly ICommandContext commandContext;

        public DMCommandContext(IMessageContext messageContext, ICommandContext commandContext) : base(messageContext.Message)
        {
            this.commandContext = commandContext;
        }

        public string ArgumentSection => commandContext.ArgumentSection;
        public Command InterpretedCommand => commandContext.InterpretedCommand;
        public CommandSearchResult CommandSearch => commandContext.CommandSearch;

        public bool IsGuildContext => false;

        public IndexArray<string> Arguments => commandContext.Arguments;

        public string RemoveArgumentsFront(int count)
        {
            return commandContext.RemoveArgumentsFront(count);
        }

    }
}
