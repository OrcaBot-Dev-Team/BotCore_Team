using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace BotCoreNET.CommandHandling
{
    /// <summary>
    /// Collection of methods useful for parsing string command arguments to discord objects
    /// </summary>
    public static class ArgumentParsing
    {
        public const string GENERIC_PARSED_USER = "A Discord User, specified either by a mention, the Discord Snowflake Id, the keyword \"self\" or Username#Discriminator";
        public const string GENERIC_PARSED_ROLE = "A Guild Role, specified either by a mention, the Discord Snowflake Id or role name";
        public const string GENERIC_PARSED_CHANNEL = "A Guild Channel, specified either by a mention, the Discord Snowflake Id, the keyword \"this\" or channel name";

        /// <summary>
        /// Parses a user given a commandcontext. Because it works without guild context it needs to be asynchronous
        /// </summary>
        /// <param name="context">The commandcontext to parse the user from</param>
        /// <param name="argument">The argument string to parse the user from</param>
        /// <param name="allowMention">Wether mentioning is enabled for parsing user</param>
        /// <param name="allowSelf">Wether pointing to self is allowed</param>
        /// <param name="allowId">Wether the ulong id is enabled for parsing user</param>
        /// <returns>The parsed user if parsing succeeded, null instead</returns>
        public static async Task<SocketUser> ParseUser(IDMCommandContext context, string argument, bool allowMention = true, bool allowSelf = true, bool allowId = true)
        {
            SocketUser result = null;
            if (allowSelf && argument.Equals("self"))
            {
                result = context.User;
            }
            else if (allowMention && argument.StartsWith("<@") && argument.EndsWith('>') && argument.Length > 3)
            {
                if (ulong.TryParse(argument.Substring(2, argument.Length - 3), out ulong userId))
                {
                    result = await context.Channel.GetUserAsync(userId) as SocketUser;
                }
            }
            else if (allowMention && argument.StartsWith("<@!") && argument.EndsWith('>') && argument.Length > 3)
            {
                if (ulong.TryParse(argument.Substring(3, argument.Length - 3), out ulong userId))
                {
                    result = await context.Channel.GetUserAsync(userId) as SocketUser;
                }
            }
            else if (allowId && ulong.TryParse(argument, out ulong userId))
            {
                result = await context.Channel.GetUserAsync(userId) as SocketUser;
            }

            return result;
        }

        /// <summary>
        /// Attempts to parse a guild user
        /// </summary>
        /// <param name="context">The guild commandcontext to parse the user from</param>
        /// <param name="argument">The argument string to parse the user from</param>
        /// <param name="result">The resulting socketguild user</param>
        /// <param name="allowMention">Wether mentioning is enabled for parsing user</param>
        /// <param name="allowSelf">Wether pointing to self is allowed</param>
        /// <param name="allowId">Wether the ulong id is enabled for parsing user</param>
        /// <returns>True if parsing was successful</returns>
        public static bool TryParseGuildUser(IGuildCommandContext context, string argument, out SocketGuildUser result, bool allowMention = true, bool allowSelf = true, bool allowId = true, bool allowName = true)
        {
            result = null;
            if (allowSelf && argument.Equals("self"))
            {
                result = context.GuildUser;
                return true;
            }
            else if (allowMention && argument.StartsWith("<@") && argument.EndsWith('>') && argument.Length > 3)
            {
                if (argument.StartsWith("<@!") && argument.Length > 4)
                {
                    if (ulong.TryParse(argument.Substring(3, argument.Length - 4), out ulong userId2))
                    {
                        result = context.Guild.GetUser(userId2);
                        return result != null;
                    }
                }
                if (ulong.TryParse(argument.Substring(2, argument.Length - 3), out ulong userId))
                {
                    result = context.Guild.GetUser(userId);
                    return result != null;
                }
            }
            else if (allowMention && argument.StartsWith("<@!") && argument.EndsWith('>') && argument.Length > 3)
            {
                if (ulong.TryParse(argument.Substring(3, argument.Length - 3), out ulong userId))
                {
                    result = context.Guild.GetUser(userId);
                    return result != null;
                }
            }
            else if (allowId && ulong.TryParse(argument, out ulong userId))
            {
                result = context.Guild.GetUser(userId);
                return result != null;
            }
            else if (allowName)
            {
                foreach (SocketGuildUser user in context.Guild.Users)
                {
                    if (user.ToString() == argument)
                    {
                        result = user;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to parse a guild role
        /// </summary>
        /// <param name="context">The guild commandcontext to parse the role with</param>
        /// <param name="argument">The argument string to parse the role from</param>
        /// <param name="result">The resulting socketguild user</param>
        /// <param name="allowMention">Wether mentioning is enabled for parsing role</param>
        /// <param name="allowId">Wether the ulong id is enabled for parsing role</param>
        /// <returns>True if parsing was successful</returns>
        public static bool TryParseRole(IGuildCommandContext context, string argument, out SocketRole result, bool allowMention = true, bool allowId = true, bool allowName = true)
        {
            result = null;

            if (allowMention && argument.StartsWith("<@&") && argument.EndsWith('>') && argument.Length > 3)
            {
                if (ulong.TryParse(argument.Substring(3, argument.Length - 4), out ulong roleId))
                {
                    result = context.Guild.GetRole(roleId);
                    return result != null;
                }
            }
            else if (allowId && ulong.TryParse(argument, out ulong roleId))
            {
                result = context.Guild.GetRole(roleId);
                return result != null;
            }
            else if (allowName)
            {
                foreach (SocketRole role in context.Guild.Roles)
                {
                    if (role.Name == argument)
                    {
                        result = role;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to parse a guild channel
        /// </summary>
        /// <param name="context">The guild commandcontext to parse the channel with</param>
        /// <param name="argument">The argument string to parse the channel from</param>
        /// <param name="result">The socketguildchannel result</param>
        /// <param name="allowMention">Wether mentioning is enabled for parsing role</param>
        /// <param name="allowThis">Wether pointing to current channel is enabled</param>
        /// <param name="allowId">Wether the ulong id is enabled for parsing role</param>
        /// <returns>True, if parsing was successful</returns>
        public static bool TryParseGuildChannel(IGuildCommandContext context, string argument, out SocketGuildChannel result, bool allowMention = true, bool allowThis = true, bool allowId = true, bool allowName = true)
        {
            result = null;
            if (allowId && ulong.TryParse(argument, out ulong Id))
            {
                result = context.Guild.GetChannel(Id);
                return result != null;
            }
            if (allowMention && argument.StartsWith("<#") && argument.EndsWith('>') && argument.Length > 3)
            {
                if (ulong.TryParse(argument.Substring(2, argument.Length - 3), out ulong Id2))
                {
                    result = context.Guild.GetTextChannel(Id2);
                    return result != null;
                }
            }
            if (allowThis && argument.Equals("this"))
            {
                result = context.GuildChannel;
                return true;
            }
            if (allowName)
            {
                foreach (SocketGuildChannel channel in context.Guild.Channels)
                {
                    if (channel.Name == argument)
                    {
                        result = channel;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Attempts to parse a guild text channel without guild command context
        /// </summary>
        /// <param name="context">The commandcontext to parse the channel with</param>
        /// <param name="argument">The argument string to parse the channel from</param>
        /// <param name="result">The sockettextchannel result</param>
        /// <param name="allowMention">Wether mentioning is enabled for parsing role</param>
        /// <param name="allowThis">Wether pointing to current channel is enabled</param>
        /// <param name="allowId">Wether the ulong id is enabled for parsing role</param>
        /// <returns>True, if parsing was successful</returns>
        public static bool TryParseGuildTextChannel(IDMCommandContext context, string argument, out SocketTextChannel result, bool allowMention = true, bool allowThis = true, bool allowId = true)
        {
            result = null;
            if (allowId && ulong.TryParse(argument, out ulong Id))
            {
                result = BotCore.Client.GetChannel(Id) as SocketTextChannel;
                return result != null;
            }
            if (allowMention && argument.StartsWith("<#") && argument.EndsWith('>') && argument.Length > 3)
            {
                if (ulong.TryParse(argument.Substring(2, argument.Length - 3), out ulong Id2))
                {
                    result = BotCore.Client.GetChannel(Id2) as SocketTextChannel;
                    return result != null;
                }
            }
            if (allowThis && argument.Equals("this") && GuildCommandContext.TryConvert(context, out IGuildCommandContext guildContext))
            {
                result = guildContext.GuildChannel;
                return result != null;
            }
            return false;
        }

        public static bool TryParseGuild(IDMCommandContext context, string argument, out SocketGuild guild, bool allowthis = true, bool allowId = true)
        {
            if (allowthis && argument.ToLower() == "this" && GuildCommandContext.TryConvert(context, out IGuildCommandContext guildContext))
            {
                guild = guildContext.Guild;
                return guild != null;
            }
            else if (allowId && ulong.TryParse(argument, out ulong guildId))
            {
                guild = BotCore.Client.GetGuild(guildId);
                return guild != null;
            }
            guild = null;
            return false;
        }
    }

    public class ArgumentParser
    {
        internal static readonly Dictionary<Type, ArgumentParser> parsers = new Dictionary<Type, ArgumentParser>();
        internal readonly string ErrorMessage;

        internal ArgumentParser(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }

        internal static bool TryParseArgument<T>(IDMCommandContext context, out T value, out string error) where T : class
        {
            if (parsers.TryGetValue(typeof(T), out ArgumentParser rawParser))
            {
                ArgumentParser<T> parser = rawParser as ArgumentParser<T>;
                if (parser != null)
                {
                    if (parser.TryParseArgument(context, out value))
                    {
                        error = null;
                        return true;
                    }
                    else
                    {
                        error = parser.ErrorMessage;
                        return false;
                    }
                }
                else
                {
                    value = null;
                    error = $"Internal Error at: {Macros.GetCodeLocation()}";
                    return false;
                }
            }
            else
            {
                value = null;
                error = $"Could not locate an argument parser for the type \"{typeof(T)}\"!";
                return false;
            }
        }

        internal static void SetupBuiltinArgumentParsers()
        {
            ArgumentParser<string>.AddArgumentParser(TryParseStringArgument, TryParseStringArgumentGuild, string.Empty);
            ArgumentParser<ObjectWrapper<long>>.AddArgumentParser(TryParseLongArgument, TryParseLongArgumentGuild, ERROR_PARSELONG);
            ArgumentParser<ObjectWrapper<ulong>>.AddArgumentParser(TryParseUlongArgument, TryParseUlongArgumentGuild, ERROR_PARSEULONG);
            ArgumentParser<ObjectWrapper<double>>.AddArgumentParser(TryParseDoubleArgument, TryParseDoubleArgumentGuild, ERROR_PARSEDOUBLE);

            ArgumentParser<SocketGuild>.AddArgumentParser(TryParseSocketGuildArgument, TryParseSocketGuildArgumentGuild, ERROR_PARSESOCKETGUILD);
            ArgumentParser<SocketGuildUser>.AddArgumentParser(TryParseSocketGuildUserArgument, TryParseSocketGuildUserArgumentGuild, ERROR_PARSESOCKETGUILDUSER);
            ArgumentParser<SocketRole>.AddArgumentParser(TryParseSocketRoleArgument, TryParseSocketRoleArgumentGuild, ERROR_PARSESOCKETROLE);

            ArgumentParser<SocketGuildChannel>.AddArgumentParser(TryParseSocketGuildChannelArgument, TryParseSocketGuildChannelArgumentGuild, ERROR_PARSESOCKETGUILDCHANNEL);
            ArgumentParser<SocketTextChannel>.AddArgumentParser(TryParseSocketTextChannelArgument, TryParseSocketTextChannelArgumentGuild, ERROR_PARSESOCKETTEXTCHANNEL);
            ArgumentParser<SocketCategoryChannel>.AddArgumentParser(TryParseSocketCategoryChannelArgument, TryParseSocketCategoryChannelArgumentGuild, ERROR_PARSESOCKETCATEGORYCHANNEL);
            ArgumentParser<SocketVoiceChannel>.AddArgumentParser(TryParseSocketVoiceChannelArgument, TryParseSocketVoiceChannelArgumentGuild, ERROR_PARSESOCKETVOICECHANNEL);
        }

        #region String, Long, Ulong, Double

        private static bool TryParseStringArgument(IDMCommandContext context, out string value)
        {
            value = context.Arguments.First;
            return true;
        }

        private static bool TryParseStringArgumentGuild(IGuildCommandContext context, out string value)
        {
            value = context.Arguments.First;
            return true;
        }

        private const string ERROR_PARSELONG = "Could not parse to a signed 64-bit integer value!";

        private static bool TryParseLongArgument(IDMCommandContext context, out ObjectWrapper<long> value)
        {
            bool success = long.TryParse(context.Arguments.First, out long rawValue);
            value = rawValue;
            return success;
        }

        private static bool TryParseLongArgumentGuild(IDMCommandContext context, out ObjectWrapper<long> value)
        {
            return TryParseLongArgument(context, out value);
        }

        private const string ERROR_PARSEULONG = "Could not parse to an unsigned 64-bit integer value!";

        private static bool TryParseUlongArgument(IDMCommandContext context, out ObjectWrapper<ulong> value)
        {
            bool success = ulong.TryParse(context.Arguments.First, out ulong rawValue);
            value = rawValue;
            return success;
        }

        private static bool TryParseUlongArgumentGuild(IDMCommandContext context, out ObjectWrapper<ulong> value)
        {
            return TryParseUlongArgument(context, out value);
        }

        private const string ERROR_PARSEDOUBLE = "Could not parse to a 64-bit floating point value!";

        private static bool TryParseDoubleArgument(IDMCommandContext context, out ObjectWrapper<double> value)
        {
            bool success = double.TryParse(context.Arguments.First, out double rawValue);
            value = rawValue;
            return success;
        }

        private static bool TryParseDoubleArgumentGuild(IDMCommandContext context, out ObjectWrapper<double> value)
        {
            return TryParseDoubleArgument(context, out value);
        }

        #endregion
        #region SocketGuild, SocketGuildUser, SocketRole

        private const string ERROR_PARSESOCKETGUILD = "Could not find a matching discord guild!";

        private static bool TryParseSocketGuildArgument(IDMCommandContext context, out SocketGuild value)
        {
            value = null;
            return false;
        }

        private static bool TryParseSocketGuildArgumentGuild(IGuildCommandContext context, out SocketGuild value)
        {
            if (context.Arguments.First.ToLower() == "this")
            {
                value = context.Guild;
                return value != null;
            }
            else if (ulong.TryParse(context.Arguments.First, out ulong guildId))
            {
                value = BotCore.Client.GetGuild(guildId);
                if (value != null)
                {
                    return !value.HasAllMembers || value.Users.Any(user => { return user.Id == context.User.Id; });
                }
                return false;
            }
            value = null;
            return false;
        }

        private const string ERROR_PARSESOCKETGUILDUSER = "Could not find a matching discord guild user!";

        private static bool TryParseSocketGuildUserArgument(IDMCommandContext context, out SocketGuildUser result)
        {
            result = null;
            return false;
        }

        private static bool TryParseSocketGuildUserArgumentGuild(IGuildCommandContext context, out SocketGuildUser result)
        {
            string argument = context.Arguments.First;
            result = null;
            if (argument.Equals("self"))
            {
                result = context.GuildUser;
                return true;
            }
            else if (argument.StartsWith("<@") && argument.EndsWith('>') && argument.Length > 3)
            {
                if (argument.StartsWith("<@!") && argument.Length > 4)
                {
                    if (ulong.TryParse(argument.Substring(3, argument.Length - 4), out ulong userId2))
                    {
                        result = context.Guild.GetUser(userId2);
                        return result != null;
                    }
                }
                if (ulong.TryParse(argument.Substring(2, argument.Length - 3), out ulong userId))
                {
                    result = context.Guild.GetUser(userId);
                    return result != null;
                }
            }
            else if (argument.StartsWith("<@!") && argument.EndsWith('>') && argument.Length > 3)
            {
                if (ulong.TryParse(argument.Substring(3, argument.Length - 3), out ulong userId))
                {
                    result = context.Guild.GetUser(userId);
                    return result != null;
                }
            }
            else if (ulong.TryParse(argument, out ulong userId))
            {
                result = context.Guild.GetUser(userId);
                return result != null;
            }
            return false;
        }

        private const string ERROR_PARSESOCKETROLE = "Could not find a matching discord guild role!";

        private static bool TryParseSocketRoleArgumentGuild(IGuildCommandContext context, out SocketRole value)
        {
            string argument = context.Arguments.First;
            value = null;

            if (argument.StartsWith("<@&") && argument.EndsWith('>') && argument.Length > 3)
            {
                if (ulong.TryParse(argument.Substring(3, argument.Length - 4), out ulong roleId))
                {
                    value = context.Guild.GetRole(roleId);
                    return value != null;
                }
            }
            else if (ulong.TryParse(argument, out ulong roleId))
            {
                value = context.Guild.GetRole(roleId);
                return value != null;
            }

            return false;
        }

        private static bool TryParseSocketRoleArgument(IDMCommandContext context, out SocketRole value)
        {
            value = null;
            return false;
        }

        #endregion
        #region SocketTextChannel, SocketCategoryChannel, SocketVoiceChannel

        private const string ERROR_PARSESOCKETGUILDCHANNEL = "Could not find a matching discord guild textchannel";

        private static bool TryParseSocketGuildChannelArgument(IDMCommandContext context, out SocketGuildChannel value)
        {
            value = null;
            return false;
        }

        private static bool TryParseSocketGuildChannelArgumentGuild(IGuildCommandContext context, out SocketGuildChannel value)
        {
            string argument = context.Arguments.First;
            value = null;
            if (argument.Equals("this"))
            {
                value = context.GuildChannel;
                return value != null;
            }
            else if (ulong.TryParse(argument, out ulong Id))
            {
                value = context.Guild.GetChannel(Id);
                return value != null;
            }
            else if (argument.StartsWith("<#") && argument.EndsWith('>') && argument.Length > 3)
            {
                if (ulong.TryParse(argument.Substring(2, argument.Length - 3), out ulong Id2))
                {
                    value = context.Guild.GetTextChannel(Id2);
                    return value != null;
                }
            }
            return false;
        }

        private const string ERROR_PARSESOCKETTEXTCHANNEL = "Could not find a matching discord guild textchannel";

        private static bool TryParseSocketTextChannelArgument(IDMCommandContext context, out SocketTextChannel value)
        {
            value = null;
            return false;
        }

        private static bool TryParseSocketTextChannelArgumentGuild(IGuildCommandContext context, out SocketTextChannel value)
        {
            string argument = context.Arguments.First;
            value = null;
            if (argument.Equals("this"))
            {
                value = context.GuildChannel;
                return value != null;
            }
            else if (ulong.TryParse(argument, out ulong Id))
            {
                value = context.Guild.GetTextChannel(Id);
                return value != null;
            }
            else if (argument.StartsWith("<#") && argument.EndsWith('>') && argument.Length > 3)
            {
                if (ulong.TryParse(argument.Substring(2, argument.Length - 3), out ulong Id2))
                {
                    value = context.Guild.GetTextChannel(Id2);
                    return value != null;
                }
            }
            return false;
        }

        private const string ERROR_PARSESOCKETCATEGORYCHANNEL = "Could not find a matching discord guild category";

        private static bool TryParseSocketCategoryChannelArgument(IDMCommandContext context, out SocketCategoryChannel value)
        {
            value = null;
            return false;
        }

        private static bool TryParseSocketCategoryChannelArgumentGuild(IGuildCommandContext context, out SocketCategoryChannel value)
        {
            value = null;
            if (ulong.TryParse(context.Arguments.First, out ulong Id))
            {
                value = context.Guild.GetCategoryChannel(Id);
                return value != null;
            }
            return false;
        }

        private const string ERROR_PARSESOCKETVOICECHANNEL = "Could not find a matching discord guild category";

        private static bool TryParseSocketVoiceChannelArgument(IDMCommandContext context, out SocketVoiceChannel value)
        {
            value = null;
            return false;
        }

        private static bool TryParseSocketVoiceChannelArgumentGuild(IGuildCommandContext context, out SocketVoiceChannel value)
        {
            value = null;
            if (ulong.TryParse(context.Arguments.First, out ulong Id))
            {
                value = context.Guild.GetVoiceChannel(Id);
                return value != null;
            }
            return false;
        }

        #endregion
    }

    public class ArgumentParser<T> : ArgumentParser where T : class
    {
        internal readonly TryParseArgumentDelegate TryParseArgument;
        internal readonly TryParseArgumentGuildDelegate TryParseArgumentGuild;

        private ArgumentParser(TryParseArgumentDelegate parser, TryParseArgumentGuildDelegate guildParser, string errorMessage) : base(errorMessage)
        {
            TryParseArgument = parser;
            TryParseArgumentGuild = guildParser;
        }

        public static void AddArgumentParser(TryParseArgumentDelegate parserDelegate, TryParseArgumentGuildDelegate guildParserDelegate, string errorMessage)
        {
            ArgumentParser<T> parser = new ArgumentParser<T>(parserDelegate, guildParserDelegate, errorMessage);
            parsers[typeof(T)] = parser;
        }

        public delegate bool TryParseArgumentDelegate(IDMCommandContext context, out T value);
        public delegate bool TryParseArgumentGuildDelegate(IGuildCommandContext context, out T value);
    }

    public class ObjectWrapper<T> where T : struct
    {
        public T value;

        public ObjectWrapper()
        {
            value = default;
        }

        public ObjectWrapper(T value)
        {
            this.value = value;
        }

        public static implicit operator T(ObjectWrapper<T> wrapper)
        {
            return wrapper.value;
        }

        public static implicit operator ObjectWrapper<T>(T value)
        {
            return new ObjectWrapper<T>(value);
        }
    }
}