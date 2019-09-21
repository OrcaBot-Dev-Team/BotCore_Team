using BotCoreNET.BotVars;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotCoreNET.CommandHandling
{
    public class BuiltInCommandParser : ICommandParser
    {
        public string Prefix { get; private set; } = "/";

        public void OnBotVarSetup()
        {
            BotVarManager.GlobalBotVars.SubscribeToBotVarUpdateEvent(OnBotVarUpdate, "prefix");
        }

        private void OnBotVarUpdate(ulong guildId, BotVar var)
        {
            if (var.IsString && !string.IsNullOrEmpty(var.String))
            {
                Prefix = var.String;
            }
        }

        public virtual ICommandContext ParseCommand(IGuildMessageContext guildContext)
        {
            return ParseCommand(guildContext as IMessageContext);
        }

        public virtual ICommandContext ParseCommand(IMessageContext dmContext)
        {
            string message = dmContext.Content.Substring(Prefix.Length).Trim();
            string commandIdentifier;
            string argumentSection;
            IndexArray<string> arguments;
            Command interpretedCommand;
            CommandSearchResult commandSearch;

            int argStartPointer = message.IndexOf(':', Prefix.Length);
            if (argStartPointer == -1 || argStartPointer == message.Length - 1)
            {
                argumentSection = string.Empty;
                arguments = new string[0];
                commandIdentifier = message;
            }
            else
            {
                argumentSection = message.Substring(argStartPointer + 1);
                commandIdentifier = message.Substring(0, argStartPointer);
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

            commandSearch = CommandCollection.TryFindCommand(commandIdentifier, arguments.TotalCount, out interpretedCommand);

            return new CommandContext(interpretedCommand, commandSearch, argumentSection, arguments);
        }

        public virtual bool IsPotentialCommand(string messageContent)
        {
            return messageContent.StartsWith(Prefix) && messageContent.Length > Prefix.Length;
        }

        public virtual bool IsPotentialCommand(string messageContent, ulong guildId)
        {
            return messageContent.StartsWith(Prefix) && messageContent.Length > Prefix.Length;
        }

        public virtual string RemoveArgumentsFront(int count, string argumentSection)
        {
            if (count == 0)
            {
                return argumentSection;
            }
            for (int i = 0; i < argumentSection.Length; i++)
            {
                bool isUnescapedComma = argumentSection[i] == ',';
                if (i > 0 && isUnescapedComma)
                {
                    isUnescapedComma = argumentSection[i - 1] != '\\';
                }
                if (isUnescapedComma)
                {
                    count--;
                    if (count == 0)
                    {
                        if (i < argumentSection.Length - 1)
                        {
                            return argumentSection.Substring(i + 1);
                        }
                        else
                        {
                            return string.Empty;
                        }
                    }
                }
            }
            return null;
        }

        public virtual string CommandSyntax(string commandidentifier)
        {
            return $"{Prefix}{commandidentifier}";
        }

        public virtual string CommandSyntax(string commandidentifier, Argument[] arguments)
        {
            if (arguments.Length == 0)
            {
                return $"{Prefix}{commandidentifier}";
            }
            else
            {
                return $"{Prefix}{commandidentifier}: {string.Join(", ", arguments, 0, arguments.Length)}";
            }
        }
    }
}
