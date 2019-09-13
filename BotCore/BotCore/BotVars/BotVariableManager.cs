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
    public static class BotVarManager
    {
        private static readonly Dictionary<string, BotVar> botVars = new Dictionary<string, BotVar>();
        private static readonly Dictionary<string, BotVar> botVarDefaults = new Dictionary<string, BotVar>();

        private static readonly Dictionary<string, HashSet<BotVarUpdated>> onBotVarUpdated = new Dictionary<string, HashSet<BotVarUpdated>>();

        private static readonly object savelock = new object();

        private static bool isDirty;

        private static void SetDirty()
        {
            isDirty = true;
        }

        static BotVarManager()
        {
            saveRoutineThread = new Thread(new ThreadStart(SetupSaveRoutine));
            saveRoutineThread.Start();
        }

        private static readonly Thread saveRoutineThread;
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
                if (isDirty)
                {
                    await SaveBotVars();
                }
            }
        }

        #region botVarsUpdateEvents

        public static void SubscribeToBotVarUpdateEvent(BotVarUpdated updateHandler, params string[] ids)
        {
            foreach (string id in ids)
            {
                if (onBotVarUpdated.TryGetValue(id, out HashSet<BotVarUpdated> subscriberList))
                {
                    subscriberList.Add(updateHandler);
                }
                else
                {
                    onBotVarUpdated[id] = new HashSet<BotVarUpdated>(new BotVarUpdated[] { updateHandler });
                }
            }
        }

        public static void UnsubscribeFromBotVarUpdateEvent(BotVarUpdated updateHandler, params string[] ids)
        {
            foreach (string id in ids)
            {
                if (onBotVarUpdated.TryGetValue(id, out HashSet<BotVarUpdated> subscriberList))
                {
                    subscriberList.Remove(updateHandler);
                    if (subscriberList.Count == 0)
                    {
                        onBotVarUpdated.Remove(id);
                    }
                }
            }
        }

        private static void handleBotVarUpdated(BotVar var)
        {
            if (onBotVarUpdated.TryGetValue(var.Identifier, out HashSet<BotVarUpdated> subscriberList))
            {
                foreach (BotVarUpdated updateHandler in subscriberList)
                {
                    updateHandler.Invoke(var);
                }
            }
        }

        #endregion
        #region retrieve config variables

        public static bool TryGetBotVar(string id, out BotVar var)
        {
            if (botVars.TryGetValue(id, out var))
            {
                return true;
            }
            else return botVarDefaults.TryGetValue(id, out var);
        }

        /// <summary>
        /// Retrieves a config variable of type unsigned integer 64
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="value">Result</param>
        /// <returns>True, if either a variable or a default value was found</returns>
        public static bool TryGetBotVar(string id, out ulong value)
        {
            if (!botVars.TryGetValue(id, out BotVar var))
            {
                botVarDefaults.TryGetValue(id, out var);
            }

            value = var.UInt64;
            return var.Type == BotVarType.UInt64;
        }

        /// <summary>
        /// Retrieves a config variable of type signed integer 64
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="value">Result</param>
        /// <returns>True, if either a variable or a default value was found</returns>
        public static bool TryGetBotVar(string id, out long value)
        {
            if (!botVars.TryGetValue(id, out BotVar var))
            {
                botVarDefaults.TryGetValue(id, out var);
            }

            value = var.Int64;
            return var.Type == BotVarType.Int64;
        }

        /// <summary>
        /// Retrieves a config variable of type floating point 64
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="value">Result</param>
        /// <returns>True, if either a variable or a default value was found</returns>
        public static bool TryGetBotVar(string id, out double value)
        {
            if (!botVars.TryGetValue(id, out BotVar var))
            {
                botVarDefaults.TryGetValue(id, out var);
            }

            value = var.Float64;
            return var.Type == BotVarType.Float64;
        }

        /// <summary>
        /// Retrieves a config variable of type string
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="value">Result</param>
        /// <returns>True, if either a variable or a default value was found</returns>
        public static bool TryGetBotVar(string id, out string value)
        {
            if (!botVars.TryGetValue(id, out BotVar var))
            {
                botVarDefaults.TryGetValue(id, out var);
            }

            value = var.String;
            return var.Type == BotVarType.String;
        }

        /// <summary>
        /// Retrieves a config variable of type boolean
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="value">Result</param>
        /// <returns>True, if either a variable or a default value was found</returns>
        public static bool TryGetBotVar(string id, out bool value)
        {
            if (!botVars.TryGetValue(id, out BotVar var))
            {
                botVarDefaults.TryGetValue(id, out var);
            }

            value = var.Bool;
            return var.Type == BotVarType.Bool;
        }

        /// <summary>
        /// Retrieves a generic config variable
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="value">Result</param>
        /// <returns>True, if either a variable or a default value was found</returns>
        public static bool TryGetBotVar(string id, out JSONContainer value)
        {
            if (!botVars.TryGetValue(id, out BotVar var))
            {
                botVarDefaults.TryGetValue(id, out var);
            }

            value = var.Generic;
            return var.Type == BotVarType.Generic;
        }

        /// <summary>
        /// Retrieves a generic, defined config variable
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="value">Result</param>
        /// <returns>True, if either a variable or a default value was found</returns>
        public static bool TryGetBotVar<T>(string id, out T value) where T : class, IGenericBotVar, new()
        {
            if (!botVars.TryGetValue(id, out BotVar var))
            {
                botVarDefaults.TryGetValue(id, out var);
            }

            value = new T();
            if ((var.Generic != null) && value.ApplyJSON(var.Generic))
            {
                return true;
            }
            value = default;
            return false;
        }

        #endregion
        #region set config variables

        public static void SetBotVar(BotVar var)
        {
            lock (savelock)
            {
                botVars[var.Identifier] = var;
                SetDirty();
            }
            handleBotVarUpdated(var);
        }

        /// <summary>
        /// Sets or creates a config variable of type unsigned integer 64
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="value">Value to assign</param>
        public static void SetBotVar(string id, ulong value)
        {
            BotVar var = new BotVar(id, value);
            lock (savelock)
            {
                botVars[id] = var;
                SetDirty();
            }
            handleBotVarUpdated(var);
        }

        /// <summary>
        /// Sets or creates a config variable of type signed integer 64
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="value">Value to assign</param>
        public static void SetBotVar(string id, long value)
        {
            BotVar var = new BotVar(id, value);
            lock (savelock)
            {
                botVars[id] = var;
                SetDirty();
            }
            handleBotVarUpdated(var);
        }

        /// <summary>
        /// Sets or creates a config variable of type floating point 64
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="value">Value to assign</param>
        public static void SetBotVar(string id, double value)
        {
            BotVar var = new BotVar(id, value);
            lock (savelock)
            {
                botVars[id] = var;
                SetDirty();
            }
            handleBotVarUpdated(var);
        }

        /// <summary>
        /// Sets or creates a config variable of type string
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="value">Value to assign</param>
        public static void SetBotVar(string id, string value)
        {
            BotVar var = new BotVar(id, value);
            lock (savelock)
            {
                botVars[id] = var;
                SetDirty();
            }
            handleBotVarUpdated(var);
        }

        /// <summary>
        /// Sets or creates a config variable of type boolean
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="value">Value to assign</param>
        public static void SetBotVar(string id, bool value)
        {
            BotVar var = new BotVar(id, value);
            lock (savelock)
            {
                botVars[id] = var;
                SetDirty();
            }
            handleBotVarUpdated(var);
        }

        /// <summary>
        /// Sets or creates a generic config variable
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="value">Value to assign</param>
        public static void SetBotVar(string id, IGenericBotVar value)
        {
            BotVar var = new BotVar(id, value.ToJSON());
            lock (savelock)
            {
                botVars[id] = var;
                SetDirty();
            }
            handleBotVarUpdated(var);
        }

        /// <summary>
        /// Sets or creates a generic config variable
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="value">Value to assign</param>
        public static void SetBotVar(string id, JSONContainer value)
        {
            BotVar var = new BotVar(id, value);
            lock (savelock)
            {
                botVars[id] = var;
                SetDirty();
            }
            handleBotVarUpdated(var);
        }

        public static bool DeleteBotVar(string id)
        {
            bool removesuccess;
            lock (savelock)
            {
                removesuccess = botVars.Remove(id);
            }
            botVarDefaults.TryGetValue(id, out BotVar def);
            if (def.Identifier == null)
            {
                def.Identifier = id;
            }
            handleBotVarUpdated(def);
            return removesuccess;
        }

        #endregion
        #region save

        internal static async Task SaveBotVars()
        {
            OnBotVarSave?.Invoke();
            JSONContainer json;
            lock (savelock)
            {
                json = ToJSON();
                isDirty = false;
            }
            await Resources.SaveJSONFile(Resources.BotVariablesFilePath, json);
            foreach (GuildBotVarCollection guildCollection in guildBotVars.Values)
            {
                await guildCollection.SaveBotVars();
            }
        }

        private static JSONContainer ToJSON()
        {
            JSONContainer result = JSONContainer.NewArray();
            foreach (BotVar var in botVars.Values)
            {
                if (var.Type != BotVarType.Undefined)
                {
                    result.Add(var.ToJSON());
                }
            }
            return result;
        }

        #endregion
        #region load

        internal static async Task<bool> TryLoadBotVars()
        {
            JSONContainer json = await Resources.LoadJSONFile(Resources.BotVariablesFilePath);
            if (json == null)
            {
                return false;
            }
            else
            {
                lock (savelock)
                {
                    FromJSON(json);
                }
                return true;
            }
        }

        private static void FromJSON(JSONContainer source)
        {
            botVars.Clear();
            if (source.IsArray)
            {
                foreach (JSONField jsonField in source.Array)
                {
                    if (jsonField.IsObject)
                    {
                        BotVar var = new BotVar();
                        if (var.FromJSON(jsonField.Container))
                        {
                            botVars.Add(var.Identifier, var);
                            handleBotVarUpdated(var);
                        }
                    }
                }
            }
        }

        #endregion
        #region loading defaults

        public static void SetDefault(string id, ulong var)
        {
            botVarDefaults[id] = new BotVar(id, var);
        }

        public static void SetDefault(string id, long var)
        {
            botVarDefaults[id] = new BotVar(id, var);
        }

        public static void SetDefault(string id, double var)
        {
            botVarDefaults[id] = new BotVar(id, var);
        }

        public static void SetDefault(string id, string var)
        {
            botVarDefaults[id] = new BotVar(id, var);
        }

        public static void SetDefault(string id, bool var)
        {
            botVarDefaults[id] = new BotVar(id, var);
        }

        public static void SetDefault(string id, IGenericBotVar var)
        {
            botVarDefaults[id] = new BotVar(id, var.ToJSON());
        }

        public static void SetDefault(string id, JSONContainer var)
        {
            botVarDefaults[id] = new BotVar(id, var);
        }

        public static void SetDefault(BotVar var)
        {
            botVarDefaults[var.Identifier] = var;
        }

        #endregion
        #region GuildBotVarManager

        private static readonly Dictionary<ulong, GuildBotVarCollection> guildBotVars = new Dictionary<ulong, GuildBotVarCollection>();

        public static GuildBotVarCollection GetGuildBotVarCollection(ulong guildId)
        {
            if (!guildBotVars.TryGetValue(guildId, out GuildBotVarCollection result))
            {
                result = new GuildBotVarCollection(guildId);
                guildBotVars.Add(guildId, result);
            }
            return result;
        }

        #endregion

        internal static List<EmbedFieldBuilder> GetBotVarList()
        {
            List<EmbedFieldBuilder> result = new List<EmbedFieldBuilder>(botVars.Count);
            foreach (BotVar var in botVars.Values)
            {
                result.Add(Macros.EmbedField(var.Identifier, var.Type, true));
            }
            return result;
        }

        public static event SimpleDelegate OnBotVarSave;
    }

    public delegate void BotVarUpdated(BotVar updated);
}
