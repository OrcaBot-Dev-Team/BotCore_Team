using System;
using System.Collections.Generic;
using System.Text;

namespace BotCoreNET.CommandHandling
{
    interface ICommandParser
    {
        ICommandContext ParseCommand(IGuildMessageContext guildContext);
        ICommandContext ParseCommand(IMessageContext dmContext);
        string CommandSyntax(string commandidentifier, string[] arguments);
    }
}
