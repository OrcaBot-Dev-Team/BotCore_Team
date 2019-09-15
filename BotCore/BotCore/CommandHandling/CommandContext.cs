using System;
using System.Collections.Generic;
using System.Text;

namespace BotCoreNET.CommandHandling
{
    class CommandContext : ICommandContext
    {
        public string ArgumentSection { get; private set; }

        public Command InterpretedCommand { get; private set; }
        public CommandSearchResult CommandSearch { get; private set; }

        public bool IsDefined
        {
            get
            {
                return ArgumentSection != null && Arguments != null && (CommandSearch == CommandSearchResult.PerfectMatch || CommandSearch == CommandSearchResult.TooManyArguments) && InterpretedCommand != null;
            }
        }

        public IndexArray<string> Arguments { get; private set; }

        public CommandContext(Command interpretedcommand, CommandSearchResult commandSearch, string argSection, IndexArray<string> args)
        {
            InterpretedCommand = interpretedcommand;
            CommandSearch = commandSearch;
            ArgumentSection = argSection;
            Arguments = args;
        }


        public string RemoveArgumentsFront(int count)
        {
            return MessageHandler.CommandParser.RemoveArgumentsFront(count, ArgumentSection);
        }

    }

    public delegate Command MessageParser(string message, string prefix, out string commandIdentifier, out string argumentSection, out string[] arguments, out CommandSearchResult searchResult);
}
