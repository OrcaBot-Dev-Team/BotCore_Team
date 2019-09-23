using System.Collections.Generic;

namespace BotCoreNET.CommandHandling
{
    public class CommandCollection
    {
        #region static

        private static readonly Dictionary<string, Command> commandDict = new Dictionary<string, Command>();

        private static readonly Dictionary<string, CommandCollection> collectionDict = new Dictionary<string, CommandCollection>();

        public static IReadOnlyCollection<Command> AllCommands => commandDict.Values;
        public static IReadOnlyCollection<CommandCollection> AllCollections => collectionDict.Values;

        public static CommandCollection BaseCollection = new CommandCollection("Basic", "Contains all basic commands");

        internal static void AddCommand(Command command, CommandCollection collection = null)
        {
            if (commandDict.TryAdd(command.Identifier, command))
            {
                if (collection == null)
                {
                    command.Collection = BaseCollection;
                    BaseCollection.commandsInCollection.Add(command);
                }
                else
                {
                    command.Collection = collection;
                    collection.commandsInCollection.Add(command);
                    if (!collectionDict.ContainsKey(collection.Name))
                    {
                        collectionDict.Add(collection.Name, collection);
                    }
                }
            }
        }

        internal static void AddCollection(CommandCollection collection)
        {
            collectionDict.TryAdd(collection.Name, collection);
        }

        public static bool TryFindCommand(string identifier, out Command command)
        {
            return commandDict.TryGetValue(identifier, out command);
        }

        public static CommandSearchResult TryFindCommand(string identifier, int ArgumentCount, out Command command)
        {
            if (!commandDict.TryGetValue(identifier, out command))
            {
                return CommandSearchResult.NoMatch;
            }
            else
            {
                if (ArgumentCount > command.MaximumArgumentCount)
                {
                    return CommandSearchResult.TooManyArguments;
                }
                else if (ArgumentCount < command.MinimumArgumentCount)
                {
                    return CommandSearchResult.TooFewArguments;
                }
                else
                {
                    return CommandSearchResult.PerfectMatch;
                }
            }
        }

        #endregion
        #region instance

        private readonly List<Command> commandsInCollection = new List<Command>();
        public IReadOnlyList<Command> Commands => commandsInCollection.AsReadOnly();

        public readonly string Name;
        public readonly string Description;

        public CommandCollection(string name, string descr)
        {
            Name = name;
            Description = descr;
            AddCollection(this);
        }

        public int ViewableCommands(IDMCommandContext context, IGuildCommandContext guildContext)
        {
            int count = 0;
            foreach (Command c in commandsInCollection)
            {
                if (c.CanView(context, guildContext, context.UserInfo.IsBotAdmin, out _))
                {
                    count++;
                }
            }
            return count;
        }

        public override string ToString()
        {
            return Name;
        }

        #endregion
    }

    public enum CommandSearchResult
    {
        NoMatch,
        PerfectMatch,
        TooFewArguments,
        TooManyArguments
    }
}
