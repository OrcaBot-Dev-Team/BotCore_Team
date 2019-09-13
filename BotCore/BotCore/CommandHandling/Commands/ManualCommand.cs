using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using BotCoreNET.Helpers;

namespace BotCoreNET.CommandHandling.Commands
{
    internal class ManualCommand : Command
    {
        public override HandledContexts ArgumentParserMethod => HandledContexts.DMOnly;

        public override HandledContexts ExecutionMethod => HandledContexts.DMOnly;

        public override string Summary => "Provides help and tips for command usage";

        public override Argument[] Arguments => new Argument[] 
        {
            new Argument("Command Identifier", "Command Identifier for the command you want to access the help for", optional:true, multiple:false)
        };

        internal ManualCommand(string identifier)
        {
            Register(identifier);
        }

        private string identifier;
        private Command command;
        private CommandCollection collection;

        protected override Task<ArgumentParseResult> ParseArguments(IDMCommandContext context)
        {
            if (context.ArgumentCount == 0)
            {
                identifier = null;
                command = null;
                return Task.FromResult(ArgumentParseResult.DefaultNoArguments);
            }

            identifier = context.Argument;
            if (!CommandCollection.TryFindCommand(identifier, out command))
            {
                if (!CommandCollection.AllCollections.TryFind(collection => { return collection.Name.ToLower() == identifier.ToLower(); }, out collection))
                {
                    return Task.FromResult(ArgumentParseResult.SuccessfullParse);
                }
            }

            return Task.FromResult(ArgumentParseResult.SuccessfullParse);
        }

        protected override Task Execute(IDMCommandContext context)
        {
            if (collection != null)
            {
                return CommandManual.SendCommandCollectionHelp(context, collection);
            }
            else if (command != null)
            {
                return CommandManual.SendCommandHelp(context, command);
            }
            else
            {
                return CommandManual.SendHelpList(context);
            }
        }
    }
}
