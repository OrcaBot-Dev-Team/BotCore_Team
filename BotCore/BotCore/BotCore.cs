using BotCoreNET.CommandHandling;
using BotCoreNET.CommandHandling.Commands;
using BotCoreNET.BotVars;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BotCoreNET.BotVars.GenericBotVars;

namespace BotCoreNET
{
    public static class BotCore
    {
        public static Color EmbedColor = new Color(255, 255, 255);
        public static Color ErrorColor = new Color(255, 0, 0);

        internal static ULongHashsetBotVar botAdmins = new ULongHashsetBotVar();
        public static IReadOnlyCollection<ulong> BotAdmins = botAdmins.hashset as IReadOnlyCollection<ulong>;

#if DEBUG

        static void Main(string[] args)
        {
            Run();
        }

#endif

        public static readonly DiscordSocketClient Client;

        static BotCore()
        {
            Client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 1024
            });
            Client.Log += Log;

            Client.MessageReceived += MessageHandler.Client_MessageReceived;
        }

        public static void Run(string baseDirectory = null, ICommandParser commandParser = null, EmbedBuilder aboutEmbed = null)
        {
            if (commandParser == null)
            {
                MessageHandler.CommandParser = new BuiltInCommandParser();
            }
            else
            {
                MessageHandler.CommandParser = commandParser;
            }
            Resources.Setup(baseDirectory);

            SetupBotVarDefaults();

            AboutCommand.SetEmbed(aboutEmbed);
            runAsync().GetAwaiter().GetResult();
        }

        private static async Task runAsync()
        {
            await LoadOrGenerateBotVars();
            string token = await retrieveToken();
            await checkBotAdmins();

            registerBasicCommands();

            await Client.LoginAsync(TokenType.Bot, token);
            await Client.StartAsync();
            await Task.Delay(-1);
        }

        #region Startup

        private static void registerBasicCommands()
        {
            new BotVarCommand("botvar");
            new ManualCommand("help");
            new ManualCommand("man");
            new AboutCommand();
            CommandCollection embedCollection = new CommandCollection("Embed", "Commands for parsing, sending and editing embeds");
            new EmbedSendCommand("embed-send", embedCollection);
            new EmbedPreviewCommand("embed-preview", embedCollection);
            new EmbedGetCommand("embed-get", embedCollection);
            new EmbedReplaceCommand("embed-replace", embedCollection);

            ActionScheduler.AddSchedulerEntry(TimeSpan.FromSeconds(20), SetActivitySchedulerAction);
        }

        private static async Task LoadOrGenerateBotVars()
        {
            bool loadsuccess = await BotVarManager.GlobalBotVars.TryLoadBotVars();
            await Resources.LoadGuildFiles();

            if (!loadsuccess)
            {
                Console.WriteLine("BotCore couldn't load config variables. Create a new config variable file now? Y/N");
                if (Console.ReadLine().ToLower().StartsWith('y'))
                {
                    Directory.CreateDirectory(Resources.BaseDirectory);
                    await BotVarManager.GlobalBotVars.CheckSaveBotVars();
                }
            }
        }

        private static async Task<string> retrieveToken()
        {
            if (!BotVarManager.GlobalBotVars.TryGetBotVar("discordtoken", out string discordToken))
            {
                Console.WriteLine("BotCore could not find the bot token config variable. Enter the bot token now:");
                discordToken = Console.ReadLine();
                BotVarManager.GlobalBotVars.SetBotVar("discordtoken", discordToken);
                await BotVarManager.GlobalBotVars.CheckSaveBotVars();
            }
            return discordToken;
        }

        private static async Task checkBotAdmins()
        {
            if (!BotVarManager.GlobalBotVars.TryGetBotVar("botadmins", out botAdmins))
            {
                botAdmins = new ULongHashsetBotVar();
                Console.Write("BotCore could not find any bot admins. ");
                bool addedBotAdmin = false;
                while (!addedBotAdmin)
                {
                    Console.WriteLine("Enter a ulong Discord user Id:");
                    string botAdminId_str = Console.ReadLine();
                    if (ulong.TryParse(botAdminId_str, out ulong botAdminId))
                    {
                        botAdmins.Add(botAdminId);
                        BotVarManager.GlobalBotVars.SetBotVar("botadmins", botAdmins);
                        await BotVarManager.GlobalBotVars.CheckSaveBotVars();
                        addedBotAdmin = true;
                    }
                    else
                    {
                        Console.Write("Failed to parse entered value to a ulong user Id! ");
                    }
                }
            }
        }

        #endregion
        #region Logging

        internal static Task Log(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }
            Console.WriteLine($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}");
            Console.ResetColor();
            return Task.CompletedTask;
        }

        #endregion
        #region BotVars

        public delegate void SimpleDelegate();
        public static event SimpleDelegate OnBotVarDefaultSetup;

        private static void SetupBotVarDefaults()
        {
            BotVar defaultColor = new BotVar("embedcolor", new ColorBotVar(new Color(255, 255, 255)));
            BotVarCollection.SetDefault(defaultColor);
            BotVarManager.GlobalBotVars.SubscribeToBotVarUpdateEvent(OnBotVarUpdated, "botadmins", "embedcolor");

            MessageHandler.SetupBotVarSubscription();
            ExceptionHandler.SetupBotVar();

            OnBotVarDefaultSetup?.Invoke();
        }

        private static void OnBotVarUpdated(ulong guildId, BotVar var)
        {
            switch (var.Identifier)
            {
                case "botadmins":
                    if (var.TryConvert(out ULongHashsetBotVar newBotAdmins))
                    {
                        botAdmins = newBotAdmins;
                    }
                    break;
                case "embedcolor":
                    if (var.TryConvert(out ColorBotVar color))
                    {
                        EmbedColor = color.C;
                    }
                    break;
            }
        }

        #endregion
        #region SetActivity SchedulerAction

        private static Task SetActivitySchedulerAction()
        {
            DateTimeOffset utcnow = DateTimeOffset.UtcNow;
            IActivity activity = new GenericActivity($"{utcnow.Year}-{utcnow.Month.ToString().PadLeft(2, '0')}-{utcnow.Day.ToString().PadLeft(2, '0')} {utcnow.Hour.ToString().PadLeft(2, '0')}:{utcnow.Minute.ToString().PadLeft(2, '0')} UTC", ActivityType.Watching);
            utcnow.AddSeconds(utcnow.Second * -1);
            utcnow.AddMinutes(1);
            ActionScheduler.AddSchedulerEntry(utcnow, SetActivitySchedulerAction);
            return Client.SetActivityAsync(activity);
        }

        /// <summary>
        /// Container for updating the discord bots activity
        /// </summary>
        private class GenericActivity : IActivity
        {
            public string Name { get; private set; }

            public GenericActivity(string name, ActivityType activity)
            {
                Name = name;
                Type = activity;
            }

            public ActivityType Type { get; private set; }
        }

        #endregion
    }

}
