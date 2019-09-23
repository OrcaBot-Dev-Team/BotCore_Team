using BotCoreNET.Helpers;
using Discord;
using JSON;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static BotCoreNET.BotCore;

namespace BotCoreNET.BotVars
{
    /// <summary>
    /// Manages <see cref="BotVarCollection"/> instances
    /// </summary>
    public static class BotVarManager
    {
        /// <summary>
        /// <see cref="BotVarCollection"/> instance used for globally saved bot variables
        /// </summary>
        public static readonly BotVarCollection GlobalBotVars = new BotVarCollection();
        private static readonly Dictionary<ulong, BotVarCollection> GuildBotVars = new Dictionary<ulong, BotVarCollection>();

        private static readonly Thread saveRoutineThread;

        static BotVarManager()
        {
            saveRoutineThread = new Thread(new ThreadStart(SetupSaveRoutine));
            saveRoutineThread.Start();
        }

        private static void SetupSaveRoutine()
        {
            SaveRoutine().GetAwaiter().GetResult();
        }

        private static async Task SaveRoutine()
        {
            TimeSpan sleepDelay = TimeSpan.FromMinutes(5);
            while (true)
            {
                await Task.Delay(sleepDelay);
                await SaveAllBotVars();
            }
        }

        internal static async Task SaveAllBotVars()
        {
            await GlobalBotVars.CheckSaveBotVars();
            foreach (BotVarCollection botVarCollection in GuildBotVars.Values)
            {
                await botVarCollection.CheckSaveBotVars();
            }
        }

        /// <summary>
        /// Retrieve or generate a guild <see cref="BotVarCollection"/>
        /// </summary>
        /// <param name="guildId">Discord Guild Id</param>
        public static BotVarCollection GetGuildBotVarCollection(ulong guildId)
        {
            if (!GuildBotVars.TryGetValue(guildId, out BotVarCollection result))
            {
                result = new BotVarCollection(guildId);
                GuildBotVars.Add(guildId, result);
            }
            return result;
        }

    }

    public delegate void BotVarUpdated(BotVar updated);
}
