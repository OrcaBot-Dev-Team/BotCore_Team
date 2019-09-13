using BotCoreNET.BotVars;
using BotCoreNET.Helpers;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotCoreNET.CommandHandling
{
    /// <summary>
    /// Represents a condition that is tested pre- command execution
    /// </summary>
    public abstract class Precondition
    {
        /// <summary>
        /// Wether the precondition can only be tested in a guild context
        /// </summary>
        public readonly bool RequireGuild;
        /// <summary>
        /// A description describing the precondition
        /// </summary>
        public readonly string Description;
        public readonly bool OverrideAsBotadmin;

        public static readonly string AUTHCHECKPASSED = "AuthCheck passed!";

        /// <summary>
        /// Tests a context versus a precondition
        /// </summary>
        /// <param name="context">The context to check against</param>
        /// <param name="message">Errormessage that is set if the test fails</param>
        /// <returns>True, if the context could be validated</returns>
        public virtual bool PreconditionCheck(IDMCommandContext context, out string message)
        {
            throw new UnpopulatedMethodException("The base authorization check method has not been overriden!");
        }

        /// <summary>
        /// Tests a context versus a precondition
        /// </summary>
        /// <param name="context">The context to check against</param>
        /// <param name="message">Errormessage that is set if the test fails</param>
        /// <returns>True, if the context could be validated</returns>
        public virtual bool PreconditionCheckGuild(IGuildCommandContext context, out string message)
        {
            throw new UnpopulatedMethodException("The guild authorization check method has not been overriden!");
        }

        public Precondition(bool requireGuild, string description, bool overrideAsBotAdmin = false)
        {
            RequireGuild = requireGuild;
            Description = description;
            OverrideAsBotadmin = overrideAsBotAdmin;
        }

        public override string ToString()
        {
            return Description;
        }
    }

    public class RequireBotAdminPrecondition : Precondition
    {
        private const string DESCR = "Requires BotAdmin Privileges";

        public RequireBotAdminPrecondition() : base(false, DESCR)
        {
        }

        public override bool PreconditionCheck(IDMCommandContext context, out string message)
        {
            if (context.UserInfo.IsBotAdmin)
            {
                message = null;
                return true;
            }
            else
            {
                message = "BotAdmin privileges required!";
                return false;
            }
        }
    }

    public class HasRolePrecondition : Precondition
    {
        private const string DESCR = "Requires a Role";

        public string BotVarId { get; private set; }
        private string roleMention = null;

        public HasRolePrecondition(string botVarId) : base(true, DESCR)
        {
            BotVarId = botVarId;
        }

        public override bool PreconditionCheckGuild(IGuildCommandContext context, out string message)
        {
            GuildBotVarCollection BotVarCollection = BotVarManager.GetGuildBotVarCollection(context.Guild.Id);
            if (!BotVarCollection.TryGetBotVar(BotVarId, out ulong roleId))
            {
                message = $"The guild config variable `{BotVarId}` has to be set to the correct role id!";
                return false;
            }
            bool hasRole = context.GuildUser.Roles.Any(role =>
            {
                if (role.Id == roleId)
                {
                    if (roleMention == null)
                    {
                        roleMention = role.Mention;
                    }
                    return true;
                }
                return false;
            });
            if (!hasRole)
            {
                message = $"You do not have required role {(roleMention == null ? Markdown.InlineCodeBlock(roleId) : roleMention)}!";
            }
            else
            {
                message = null;
            }
            return hasRole;
        }

        public override string ToString()
        {
            return $"Requires role {(roleMention == null ? Markdown.InlineCodeBlock(BotVarId) : roleMention)}";
        }
    }

    public class IsOwnerOrAdminPrecondition : Precondition
    {
        private const string DESCR = "Be Guildowner or Admin!";

        public IsOwnerOrAdminPrecondition() : base(true, DESCR)
        {
        }

        public override bool PreconditionCheckGuild(IGuildCommandContext context, out string message)
        {
            message = null;
            if (context.GuildUser.Id == context.Guild.OwnerId)
            {
                return true;
            }

            if (!context.GuildUser.Roles.Any(role => { return role.Permissions.Administrator == true; }))
            {
                message = "You are neither Owner of this Guild or have a role with Admin permission!";
                return false;
            }
            return true;
        }


    }
}
