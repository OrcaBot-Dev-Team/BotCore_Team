using Discord;
using JSON;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotCoreNET.BotVars.GenericBotVars
{
    class ColorBotVar : IGenericBotVar
    {
        public Color C;

        private const string JSON_RED = "r";
        private const string JSON_GREEN = "g";
        private const string JSON_BLUE = "b";

        public ColorBotVar() { }

        public ColorBotVar(Color c)
        {
            C = c;
        }

        public bool ApplyJSON(JSONContainer json)
        {
            if (json.TryGetField(JSON_RED, out uint red) && json.TryGetField(JSON_GREEN, out uint green) && json.TryGetField(JSON_BLUE, out uint blue))
            {
                C = new Color((byte)red, (byte)green, (byte)blue);
                return true;
            }
            return false;
        }

        public JSONContainer ToJSON()
        {
            JSONContainer result = JSONContainer.NewObject();
            result.TryAddField(JSON_RED, C.R);
            result.TryAddField(JSON_GREEN, C.G);
            result.TryAddField(JSON_BLUE, C.B);
            return result;
        }
    }
}
