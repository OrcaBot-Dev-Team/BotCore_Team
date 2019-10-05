using Discord;
using System.Threading.Tasks;

namespace BotCoreNET.CommandHandling.Commands
{
    internal class AboutCommand : Command
    {
        public override HandledContexts ArgumentParserMethod => HandledContexts.None;
        public override HandledContexts ExecutionMethod => HandledContexts.DMOnly;
        public override string Summary => "Lists basic information about";

        internal static EmbedBuilder AboutEmbed;
        private const string Footer = "BotCore Discord.NET Command Handler built by BrainProtest";

        public static void SetEmbed(EmbedBuilder embed = null)
        {
            if (embed == null)
            {
                embed = new EmbedBuilder()
                {
                    Color = BotCore.EmbedColor,
                    Title = "BotCore Discord.NET Command Handler",
                    Description = "Github Link"
                };
            }
            embed.Footer = new EmbedFooterBuilder()
            {
                Text = Footer
            };
            AboutEmbed = embed;
        }

        internal AboutCommand()
        {
            Register("about");
        }

        protected override Task Execute(IDMCommandContext context, object argObj)
        {
            if (AboutEmbed.Author == null)
            {
                AboutEmbed.Author = new EmbedAuthorBuilder()
                {
                    IconUrl = BotCore.Client.CurrentUser.GetDefaultAvatarUrl()
                };
            }
            return context.Channel.SendEmbedAsync(AboutEmbed);
        }
    }
}
