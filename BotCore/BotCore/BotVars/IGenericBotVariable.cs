using JSON;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotCoreNET.BotVars
{
    public interface IGenericBotVar
    {
        bool ApplyJSON(JSONContainer json);
        JSONContainer ToJSON();
    }
}
