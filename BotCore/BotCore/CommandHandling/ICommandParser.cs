using System;
using System.Collections.Generic;
using System.Text;

namespace BotCoreNET.CommandHandling
{
    public interface ICommandParser
    {
        /// <summary>
        /// Called when bot vars are setup, to subscribe to command parser specific events
        /// </summary>
        void OnBotVarSetup();
        /// <summary>
        /// Specifies wether a message content could be a command in a PM
        /// </summary>
        /// <returns>True, if identified as a potential command</returns>
        bool IsPotentialCommand(string messageContent);
        /// <summary>
        /// Specifies wether a message content could be a command for a given guild as context
        /// </summary>
        /// <returns>True, if identified as a potential command</returns>
        bool IsPotentialCommand(string messageContent, ulong guildId);
        /// <summary>
        /// Parses an <see cref= "IGuildMessageContext"/> to an <see cref="ICommandContext"/>
        /// </summary>
        ICommandContext ParseCommand(IGuildMessageContext guildContext);
        /// <summary>
        /// Parses an <see cref= "IMessageContext"/> to an <see cref="ICommandContext"/>
        /// </summary>
        ICommandContext ParseCommand(IMessageContext dmContext);
        /// <summary>
        /// Provides a full syntax string based on the command identifier
        /// </summary>
        /// <param name="commandidentifier">Command identifier / key</param>
        string CommandSyntax(string commandidentifier);
        /// <summary>
        /// Provides a full syntax string based on the command identifier and the expected arguments
        /// </summary>
        /// <param name="commandidentifier">Command identifier / key</param>
        /// <param name="arguments">Arguments</param>
        string CommandSyntax(string commandidentifier, Argument[] arguments);
        /// <summary>
        /// Provides a substring of the argumentSection after the specified arguments infront.
        /// </summary>
        /// <param name="count">Amount of arguments to remove from the front</param>
        /// <param name="argumentSection">The argumentsection</param>
        /// <returns>The substring, or null if count is out of range</returns>
        string RemoveArgumentsFront(int count, string argumentSection);
    }
}
