using Discord.WebSocket;
using JSON;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotCoreNET.BotVars.GenericBotVars
{
    class SpecificChannelBotVar : IGenericBotVar
    {
        public ulong GuildId;
        public ulong ChannelId;

        public SpecificChannelBotVar()
        {
        }

        public SpecificChannelBotVar(ulong guildId, ulong channelId)
        {
            GuildId = guildId;
            ChannelId = channelId;
        }

        private const string JSON_GUILDID = "GuildId";
        private const string JSON_CHANNELID = "ChannelId";

        public bool ApplyJSON(JSONContainer json)
        {
            return json.TryGetField(JSON_GUILDID, out GuildId) && json.TryGetField(JSON_CHANNELID, out ChannelId);
        }

        public JSONContainer ToJSON()
        {
            JSONContainer result = JSONContainer.NewObject();
            result.TryAddField(JSON_GUILDID, GuildId);
            result.TryAddField(JSON_CHANNELID, ChannelId);
            return result;
        }

        public bool TryGetChannel<T>(out T channel) where T : SocketGuildChannel
        {
            var guild = BotCore.Client.GetGuild(GuildId);
            if (guild != null)
            {
                channel = guild.GetChannel(ChannelId) as T;
                return channel != null;
            }
            channel = null;
            return false;
        }
    }
}
