using System.Threading.Tasks;

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

        private class ArgumentContainer
        {
            public string identifier;
            public Command command;
            public CommandCollection collection;
        }

        protected override Task<ArgumentParseResult> ParseArguments(IDMCommandContext context)
        {
            ArgumentContainer argOut = new ArgumentContainer();

            if (context.Arguments.TotalCount == 0)
            {
                return Task.FromResult(ArgumentParseResult.DefaultNoArguments);
            }

            argOut.identifier = context.Arguments.First;
            if (!CommandCollection.TryFindCommand(argOut.identifier, out argOut.command))
            {
                if (!CommandCollection.AllCollections.TryFind(collection => { return collection.Name.ToLower() == argOut.identifier.ToLower(); }, out argOut.collection))
                {
                    return Task.FromResult(new ArgumentParseResult(argOut));
                }
            }

            return Task.FromResult(new ArgumentParseResult(argOut));
        }

        protected override Task Execute(IDMCommandContext context, object argObj)
        {
            ArgumentContainer args = argObj as ArgumentContainer;

            if (args.collection != null)
            {
                return CommandManual.SendCommandCollectionHelp(context, args.collection);
            }
            else if (args.command != null)
            {
                return CommandManual.SendCommandHelp(context, args.command);
            }
            else
            {
                return CommandManual.SendHelpList(context);
            }
        }
    }
}
