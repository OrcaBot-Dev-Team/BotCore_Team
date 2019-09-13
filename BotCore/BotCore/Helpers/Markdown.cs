using System;
using System.Collections.Generic;
using System.Text;

namespace BotCoreNET.Helpers
{
    public static class Markdown
    {
        private const string CODEBLOCKBASESTRING = "``````";
        private const string INLINECODEBLOCKBASESTRING = "``";
        private const string FATBASESTRING = "****";

        /// <summary>
        /// Adds multiline codeblock markdown syntax around the given input
        /// </summary>
        /// <param name="input">Any object whichs .ToString() function is used as the text</param>
        /// <returns></returns>
        public static string MultiLineCodeBlock(object input)
        {
            return CODEBLOCKBASESTRING.Insert(3, input.ToString());
        }

        /// <summary>
        /// Adds inline codeblock markdown syntax around the given input
        /// </summary>
        /// <param name="input">Any object whichs .ToString() function is used as the text</param>
        /// <returns></returns>
        public static string InlineCodeBlock(object input)
        {
            return INLINECODEBLOCKBASESTRING.Insert(1, input.ToString());
        }

        /// <summary>
        /// Adds fat markdown syntax around the given input
        /// </summary>
        /// <param name="input">Any object whichs .ToString() function is used as the text</param>
        /// <returns></returns>
        public static string Fat(object input)
        {
            return FATBASESTRING.Insert(2, input.ToString());
        }

        /// <summary>
        /// Creates a mention for a given role Id
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public static string Mention_Role(ulong roleId)
        {
            return $"<@&{roleId}>";
        }

        /// <summary>
        /// Creates a mention for a given user id
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="nickname">Mention with nickname?</param>
        /// <returns></returns>
        public static string Mention_User(ulong userId, bool nickname = true)
        {
            if (nickname)
            {
                return $"<@!{userId}>";
            }
            else
            {
                return $"<@{userId}>";
            }
        }

        /// <summary>
        /// Creates a mention for a channel
        /// </summary>
        /// <param name="channelId"></param>
        /// <returns></returns>
        public static string Mention_Channel(ulong channelId)
        {
            return $"<#{channelId}>";
        }

        public static string MessageURL(ulong guildId, ulong channelId, ulong messageId)
        {
            return $"https://discordapp.com/channels/{guildId}/{channelId}/{messageId}";
        }
    }
}
