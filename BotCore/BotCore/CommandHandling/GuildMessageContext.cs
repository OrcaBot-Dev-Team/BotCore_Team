using BotCoreNET.BotVars.GenericBotVars;
using Discord.WebSocket;

namespace BotCoreNET.CommandHandling
{
    public class GuildMessageContext : MessageContext, IGuildMessageContext
    {
        public SocketGuildUser GuildUser { get; private set; }

        public SocketTextChannel GuildChannel { get; private set; }

        public SocketGuild Guild { get; private set; }
        public GuildChannelMeta ChannelMeta { get; private set; }

        public override bool IsDefined
        {
            get
            {
                return base.IsDefined && Guild != null && GuildUser != null && GuildChannel != null;
            }
        }

        public GuildMessageContext(SocketUserMessage message, SocketGuild guild) : base(message)
        {
            Guild = guild;
            if (base.IsDefined && guild != null)
            {
                GuildUser = guild.GetUser(message.Author.Id);
                GuildChannel = guild.GetTextChannel(Channel.Id);
                ChannelMeta = GuildChannelMeta.GetDefaultOrSaved(Guild.Id, GuildChannel.Id);
            }
        }


    }
}
