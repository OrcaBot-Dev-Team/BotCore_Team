using System;
using System.Collections.Generic;
using System.Text;

namespace BotCoreNET.CommandHandling
{
    public class GuildCommandContext : GuildMessageContext, IGuildCommandContext
    {
        private readonly ICommandContext commandContext;

        public GuildCommandContext(IGuildMessageContext messagecontext, ICommandContext commandcontext) : base(messagecontext.Message, messagecontext.Guild)
        {
            commandContext = commandcontext;
        }

        public string ArgumentSection => commandContext.ArgumentSection;

        public IndexArray<string> Arguments => commandContext.Arguments;

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
