using BotCoreNET.Helpers;
using Discord;
using JSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BotCoreNET.BotVars
{
    public class GuildBotVarCollection
    {
        #region static
        private static readonly Dictionary<string, BotVar> BotVarDefaults = new Dictionary<string, BotVar>();
        #endregion

        public readonly ulong GuildID;
        private readonly Dictionary<string, BotVar> BotVars = new Dictionary<string, BotVar>();

        public List<BotVar> BotVarList => new List<BotVar>(BotVars.Values);

        private readonly object savelock = new object();
        private bool isDirty;

        private void SetDirty()
        {
            isDirty = true;
        }

        public GuildBotVarCollection(ulong guildId)
        {
            GuildID = guildId;
        }

        #region eventhandling

        private static readonly Dictionary<string, HashSet<BotVarUpdatedGuild>> onBotVarUpdatedGuild = new Dictionary<string, HashSet<BotVarUpdatedGuild>>();
        private readonly Dictionary<string, HashSet<BotVarUpdatedGuild>> onBotVarUpdated = new Dictionary<string, HashSet<BotVarUpdatedGuild>>();

        public static void SubscribeToBotVarUpdateStaticEvent(BotVarUpdatedGuild updateHandler, params string[] ids)
        {
            foreach (string id in ids)
            {
                if (onBotVarUpdatedGuild.TryGetValue(id, out HashSet<BotVarUpdatedGuild> subscriberList))
                {
                    subscriberList.Add(updateHandler);
                }
                else
                {
                    onBotVarUpdatedGuild[id] = new HashSet<BotVarUpdatedGuild>(new BotVarUpdatedGuild[] { updateHandler });
                }
            }
        }

        public static void UnsubscribeFromBotVarUpdateStaticEvent(BotVarUpdatedGuild updateHandler, params string[] ids)
        {
            foreach (string id in ids)
            {
                if (onBotVarUpdatedGuild.TryGetValue(id, out HashSet<BotVarUpdatedGuild> subscriberList))
                {
                    subscriberList.Remove(updateHandler);
                    if (subscriberList.Count == 0)
                    {
                        onBotVarUpdatedGuild.Remove(id);
                    }
                }
            }
        }

        internal void SubscribeToBotVarUpdateEvent(BotVarUpdatedGuild updateHandler, params string[] ids)
        {
            foreach (string id in ids)
            {
                if (onBotVarUpdated.TryGetValue(id, out HashSet<BotVarUpdatedGuild> subscriberList))
                {
                    subscriberList.Add(updateHandler);
                }
                else
                {
                    onBotVarUpdated[id] = new HashSet<BotVarUpdatedGuild>(new BotVarUpdatedGuild[] { updateHandler });
                }
            }
        }

        internal void UnsubscribeFromBotVarUpdateEvent(BotVarUpdatedGuild updateHandler, params string[] ids)
        {
            foreach (string id in ids)
            {
                if (onBotVarUpdated.TryGetValue(id, out HashSet<BotVarUpdatedGuild> subscriberList))
                {
                    subscriberList.Remove(updateHandler);
                    if (subscriberList.Count == 0)
                    {
                        onBotVarUpdated.Remove(id);
                    }
                }
            }
        }

        private void handleBotVarUpdated(BotVar var)
        {
            if (onBotVarUpdated.TryGetValue(var.Identifier, out HashSet<BotVarUpdatedGuild> subscriberList))
            {
                foreach (BotVarUpdatedGuild updateHandler in subscriberList)
                {
                    updateHandler.Invoke(GuildID, var);
                }
            }
            if (onBotVarUpdatedGuild.TryGetValue(var.Identifier, out HashSet<BotVarUpdatedGuild> guildSubscriberList))
            {
                foreach (BotVarUpdatedGuild updateHandler in guildSubscriberList)
                {
                    updateHandler.Invoke(GuildID, var);
                }
            }
        }

        #endregion
        #region retrieve config variables

        public bool TryGetBotVar(string id, out BotVar var)
        {
            if (BotVars.TryGetValue(id, out var))
            {
                return true;
            }
            else return BotVarDefaults.TryGetValue(id, out var);
        }

        /// <summary>
        /// Retrieves a config variable of type unsigned integer 64
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="value">Result</param>
        /// <returns>True, if either a variable or a default value was found</returns>
        public bool TryGetBotVar(string id, out ulong value)
        {
            if (!BotVars.TryGetValue(id, out BotVar var))
            {
                BotVarDefaults.TryGetValue(id, out var);
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
        public bool TryGetBotVar(string id, out long value)
        {
            if (!BotVars.TryGetValue(id, out BotVar var))
            {
                BotVarDefaults.TryGetValue(id, out var);
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
        public bool TryGetBotVar(string id, out double value)
        {
            if (!BotVars.TryGetValue(id, out BotVar var))
            {
                BotVarDefaults.TryGetValue(id, out var);
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
        public bool TryGetBotVar(string id, out string value)
        {
            if (!BotVars.TryGetValue(id, out BotVar var))
            {
                BotVarDefaults.TryGetValue(id, out var);
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
        public bool TryGetBotVar(string id, out bool value)
        {
            if (!BotVars.TryGetValue(id, out BotVar var))
            {
                BotVarDefaults.TryGetValue(id, out var);
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
        public bool TryGetBotVar(string id, out JSONContainer value)
        {
            if (!BotVars.TryGetValue(id, out BotVar var))
            {
                BotVarDefaults.TryGetValue(id, out var);
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
        public bool TryGetBotVar<T>(string id, out T value) where T : class, IGenericBotVar, new()
        {
            if (!BotVars.TryGetValue(id, out BotVar var))
            {
                BotVarDefaults.TryGetValue(id, out var);
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

        public void SetBotVar(BotVar var)
        {
            lock (savelock)
            {
                BotVars[var.Identifier] = var;
                SetDirty();
            }
            handleBotVarUpdated(var);
        }

        /// <summary>
        /// Sets or creates a config variable of type unsigned integer 64
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="value">Value to assign</param>
        public void SetBotVar(string id, ulong value)
        {
            BotVar var = new BotVar(id, value);
            lock (savelock)
            {
                BotVars[id] = var;
                SetDirty();
            }
            handleBotVarUpdated(var);
        }

        /// <summary>
        /// Sets or creates a config variable of type signed integer 64
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="value">Value to assign</param>
        public void SetBotVar(string id, long value)
        {
            BotVar var = new BotVar(id, value);
            lock (savelock)
            {
                BotVars[id] = var;
                SetDirty();
            }
            handleBotVarUpdated(var);
        }

        /// <summary>
        /// Sets or creates a config variable of type floating point 64
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="value">Value to assign</param>
        public void SetBotVar(string id, double value)
        {
            BotVar var = new BotVar(id, value);
            lock (savelock)
            {
                BotVars[id] = var;
                SetDirty();
            }
            handleBotVarUpdated(var);
        }

        /// <summary>
        /// Sets or creates a config variable of type string
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="value">Value to assign</param>
        public void SetBotVar(string id, string value)
        {
            BotVar var = new BotVar(id, value);
            lock (savelock)
            {
                BotVars[id] = var;
                SetDirty();
            }
            handleBotVarUpdated(var);
        }

        /// <summary>
        /// Sets or creates a config variable of type boolean
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="value">Value to assign</param>
        public void SetBotVar(string id, bool value)
        {
            BotVar var = new BotVar(id, value);
            lock (savelock)
            {
                BotVars[id] = var;
                SetDirty();
            }
            handleBotVarUpdated(var);
        }

        /// <summary>
        /// Sets or creates a generic config variable
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="value">Value to assign</param>
        public void SetBotVar(string id, IGenericBotVar value)
        {
            BotVar var = new BotVar(id, value.ToJSON());
            lock (savelock)
            {
                BotVars[id] = var;
                SetDirty();
            }
            handleBotVarUpdated(var);
        }

        /// <summary>
        /// Sets or creates a generic config variable
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="value">Value to assign</param>
        public void SetBotVar(string id, JSONContainer value)
        {
            BotVar var = new BotVar(id, value);
            lock (savelock)
            {
                BotVars[id] = var;
                SetDirty();
            }
            handleBotVarUpdated(var);
        }

        public bool DeleteBotVar(string id)
        {
            bool removesuccess;
            lock (savelock)
            {
                removesuccess = BotVars.Remove(id);
            }
            BotVarDefaults.TryGetValue(id, out BotVar def);
            if (def.Identifier == null)
            {
                def.Identifier = id;
            }
            handleBotVarUpdated(def);
            return removesuccess;
        }

        #endregion
        #region save

        public static event UlongDelegate OnGuildBotVarsSaved;

        internal Task SaveBotVars()
        {
            if (isDirty)
            {
                JSONContainer json;
                OnGuildBotVarsSaved?.Invoke(GuildID);
                lock (savelock)
                {
                    json = ToJSON();
                    isDirty = false;
                }
                string filepath = Resources.GetGuildBotVarSaveFileName(GuildID);
                string directory = Path.GetDirectoryName(filepath);
                Directory.CreateDirectory(directory);
                return Resources.SaveJSONFile(filepath, json);
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        private JSONContainer ToJSON()
        {
            JSONContainer result = JSONContainer.NewArray();
            foreach (BotVar var in BotVars.Values)
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

        internal async Task<bool> TryLoadBotVars()
        {
            string filepath = Resources.GetGuildBotVarSaveFileName(GuildID);
            if (File.Exists(filepath))
            {
                JSONContainer json = await Resources.LoadJSONFile(filepath);
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
            else
            {
                return false;
            }
        }

        private void FromJSON(JSONContainer source)
        {
            BotVars.Clear();
            if (source.IsArray)
            {
                foreach (JSONField jsonField in source.Array)
                {
                    if (jsonField.IsObject)
                    {
                        BotVar var = new BotVar();
                        if (var.FromJSON(jsonField.Container))
                        {
                            BotVars.Add(var.Identifier, var);
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
            BotVarDefaults[id] = new BotVar(id, var);
        }

        public static void SetDefault(string id, long var)
        {
            BotVarDefaults[id] = new BotVar(id, var);
        }

        public static void SetDefault(string id, double var)
        {
            BotVarDefaults[id] = new BotVar(id, var);
        }

        public static void SetDefault(string id, string var)
        {
            BotVarDefaults[id] = new BotVar(id, var);
        }

        public static void SetDefault(string id, bool var)
        {
            BotVarDefaults[id] = new BotVar(id, var);
        }

        public static void SetDefault(string id, IGenericBotVar var)
        {
            BotVarDefaults[id] = new BotVar(id, var.ToJSON());
        }

        public static void SetDefault(string id, JSONContainer var)
        {
            BotVarDefaults[id] = new BotVar(id, var);
        }

        public static void SetDefault(BotVar var)
        {
            BotVarDefaults[var.Identifier] = var;
        }

        #endregion

        internal List<EmbedFieldBuilder> GetBotVarList()
        {
            List<EmbedFieldBuilder> result = new List<EmbedFieldBuilder>(BotVars.Count);
            foreach (BotVar var in BotVars.Values)
            {
                result.Add(Macros.EmbedField(var.Identifier, var.Type));
            }
            return result;
        }

    }

    public delegate void BotVarUpdatedGuild(ulong guildID, BotVar var);
    public delegate void UlongDelegate(ulong id);
}
