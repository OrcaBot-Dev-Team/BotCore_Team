using System;
using System.Collections.Generic;
using System.Text;

namespace BotCoreNET.CommandHandling
{
    public struct UserInformation
    {

        public readonly ulong UserId;
        public readonly bool IsBotAdmin;

        public UserInformation(ulong userId)
        {
            UserId = userId;
            IsBotAdmin = BotCore.botAdmins.Contains(UserId);
        }
    }
}
