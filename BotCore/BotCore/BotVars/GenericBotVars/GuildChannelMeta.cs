using System;
using System.Collections.Generic;
using System.Text;
using BotCoreNET.CommandHandling;
using JSON;

namespace BotCoreNET.BotVars.GenericBotVars
{
    public class GuildChannelMeta : IGenericBotVar
    {
        #region static

        private static readonly Dictionary<ulong, GuildChannelMeta> guildDefaults = new Dictionary<ulong, GuildChannelMeta>();

        public static GuildChannelMeta GetDefault(ulong guildId)
        {
            if (!guildDefaults.TryGetValue(guildId, out GuildChannelMeta guildDefault))
            {
                BotVarCollection guildBotVarCollection = BotVarManager.GetGuildBotVarCollection(guildId);
                if (!guildBotVarCollection.TryGetBotVar("defaultguildchannelmeta", out guildDefault))
                {
                    guildDefault = new GuildChannelMeta();
                }
                guildDefaults.Add(guildId, guildDefault);
                guildBotVarCollection.SubscribeToBotVarUpdateEvent(OnBotVarUpdated, "defaultguildchannelmeta");
            }
            return guildDefault;
        }

        private static void OnBotVarUpdated(ulong guildid, BotVar var)
        {
            if (var.Identifier == "defaultguildchannelmeta" && var.TryConvert(out GuildChannelMeta newGuildDefault))
            {
                guildDefaults[guildid] = newGuildDefault;
            }
        }
        
        public static GuildChannelMeta GetDefaultOrSaved(ulong guildId, ulong channelId)
        {
            BotVarCollection guildBotVarCollection = BotVarManager.GetGuildBotVarCollection(guildId);
            if (!guildBotVarCollection.TryGetBotVar($"{channelId}.meta", out GuildChannelMeta channelMeta))
            {
                channelMeta = GetDefault(guildId);
            }
            return channelMeta;
        }

        #endregion

        public bool AllowCommands = false;
        public bool AllowShitposting = false;
        public HashSet<string> allowedCommandCollections = new HashSet<string>();

        private const string JSON_ALLOWCMDS = "AllowCMDs";
        private const string JSON_ALLOWSHITPOSTING = "AllowShitpost";
        private const string JSON_ALLOWEDCOLL = "Collections";

        public GuildChannelMeta()
        {

        }

        public bool ApplyJSON(JSONContainer json)
        {
            if (json.TryGetField(JSON_ALLOWCMDS, out AllowCommands) && json.TryGetField(JSON_ALLOWSHITPOSTING, out AllowShitposting))
            {
                if (json.TryGetArrayField(JSON_ALLOWEDCOLL, out JSONContainer collArray))
                {
                    foreach (JSONField field in collArray.Array)
                    {
                        if (field.IsString)
                        {
                            allowedCommandCollections.Add(field.String);
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public JSONContainer ToJSON()
        {
            JSONContainer json = JSONContainer.NewObject();
            json.TryAddField(JSON_ALLOWCMDS, AllowCommands);
            json.TryAddField(JSON_ALLOWSHITPOSTING, AllowShitposting);
            if (allowedCommandCollections.Count > 0)
            {
                JSONContainer collArray = JSONContainer.NewArray();
                foreach (string coll in allowedCommandCollections)
                {
                    collArray.Add(coll);
                }
                json.TryAddField(JSON_ALLOWEDCOLL, collArray);
            }
            return json;
        }

        public bool CheckChannelMeta(UserInformation userinfo, Command command, out string error)
        {
            error = null;
            if (userinfo.IsBotAdmin)
            {
                return true;
            }

            if (!AllowCommands)
            {
                error = "This channel does not allow command execution!";
                return false;
            }

            if (command != null)
            {
                if (command.IsShitposting && !AllowShitposting)
                {
                    error = "This channel does not allow shitposting commands!";
                    return false;
                }

                if (allowedCommandCollections.Count > 0)
                {
                    if (!allowedCommandCollections.Contains(command.Collection.Name))
                    {
                        error = $"This channel does not allow commands from the command collection `{command.Collection.Name}`!";
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
