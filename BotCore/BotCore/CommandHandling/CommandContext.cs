using System;
using System.Collections.Generic;
using System.Text;

namespace BotCoreNET.CommandHandling
{
    class CommandContext : ICommandContext
    {
        public string ArgumentSection { get; private set; }

        public int ArgumentCount => Arguments.Length;
        public string[] Arguments { get; private set; }

        public int ArgPointer { get; set; }

        public string Argument
        {
            get
            {
                return Arguments[ArgPointer];
            }
        }

        public Command InterpretedCommand { get; private set; }
        public CommandSearchResult CommandSearch { get; private set; }

        public bool IsDefined
        {
            get
            {
                return ArgumentSection != null && Arguments != null && (CommandSearch == CommandSearchResult.PerfectMatch || CommandSearch == CommandSearchResult.TooManyArguments) && InterpretedCommand != null;
            }
        }

        internal static MessageParser CommandParser;

        public CommandContext(string messageContent)
        {
            InterpretedCommand = CommandParser(messageContent, MessageHandler.Prefix, out _, out string argumentSection, out string[] arguments, out CommandSearchResult commandSearch);
            ArgumentSection = argumentSection;
            Arguments = arguments;
            CommandSearch = commandSearch;
        }

        internal static Command DefaultMessageParser(string msg, string prefix, out string commandIdentifier, out string argumentSection, out string[] arguments, out CommandSearchResult searchResult)
        {

            if (!string.IsNullOrEmpty(msg))
            {
                ParseInputString(msg, prefix, out commandIdentifier, out argumentSection, out arguments);

                searchResult = CommandCollection.TryFindCommand(commandIdentifier, arguments.Length, out Command command);
                return command;
            }
            commandIdentifier = null;
            argumentSection = null;
            arguments = null;
            searchResult = default;
            return null;
        }

        private static void ParseInputString(string msg, string prefix, out string commandIdentifier, out string argumentSection, out string[] arguments)
        {
            msg = msg.Substring(prefix.Length).Trim();
            int argStartPointer = msg.IndexOf(':', prefix.Length);
            if (argStartPointer == -1 || argStartPointer == msg.Length - 1)
            {
                argumentSection = string.Empty;
                arguments = new string[0];
                commandIdentifier = msg;
            }
            else
            {
                argumentSection = msg.Substring(argStartPointer + 1);
                commandIdentifier = msg.Substring(0, argStartPointer);
                int argcnt = 1;
                for (int i = 0; i < argumentSection.Length; i++)
                {
                    bool isUnescapedComma = argumentSection[i] == ',';
                    if (i > 0 && isUnescapedComma)
                    {
                        isUnescapedComma = argumentSection[i - 1] != '\\';
                    }
                    if (isUnescapedComma)
                    {
                        argcnt++;
                    }
                }
                arguments = new string[argcnt];
                int argindex = 0;
                int lastindex = 0;
                for (int i = 0; i < argumentSection.Length; i++)
                {
                    bool isUnescapedComma = argumentSection[i] == ',';
                    if (i > 0 && isUnescapedComma)
                    {
                        isUnescapedComma = argumentSection[i - 1] != '\\';
                    }
                    if (isUnescapedComma)
                    {
                        if (lastindex < i)
                        {
                            arguments[argindex] = argumentSection.Substring(lastindex, i - lastindex).Trim().Replace("\\,", ",");
                        }
                        else
                        {
                            arguments[argindex] = string.Empty;
                        }
                        argindex++;
                        lastindex = i + 1;
                    }
                }
                if (lastindex <= argumentSection.Length - 1)
                {
                    arguments[argindex] = argumentSection.Substring(lastindex, argumentSection.Length - lastindex).Trim().Replace("\\,", ",");
                }
                else
                {
                    arguments[argindex] = string.Empty;
                }
            }
        }

        public string RemoveArgumentsFront(int count)
        {
            for (int i = 0; i < ArgumentSection.Length; i++)
            {
                bool isUnescapedComma = ArgumentSection[i] == ',';
                if (i > 0 && isUnescapedComma)
                {
                    isUnescapedComma = ArgumentSection[i - 1] != '\\';
                }
                if (isUnescapedComma)
                {
                    count--;
                    if (count == 0)
                    {
                        return ArgumentSection.Substring(i);
                    }
                }
            }
            return null;
        }

    }

    public delegate Command MessageParser(string message, string prefix, out string commandIdentifier, out string argumentSection, out string[] arguments, out CommandSearchResult searchResult);
}
