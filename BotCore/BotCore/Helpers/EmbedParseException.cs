using System;
using System.Collections.Generic;
using System.Text;

namespace BotCoreNET.Helpers
{
    public class EmbedParseException : Exception
    {
        public EmbedParseException(string message) : base(message) { }
    }
}
