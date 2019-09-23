namespace BotCoreNET.CommandHandling
{
    /// <summary>
    /// Provides parsing results for argument parsers. Only the static readonly objects represent a successful parse, all other represent a parse error!
    /// </summary>
    public class ArgumentParseResult
    {
        /// <summary>
        /// Wether parsing was successful or not
        /// </summary>
        public bool Success { get; private set; } = false;
        /// <summary>
        /// The error message, if parsing was unsuccessful
        /// </summary>
        public string Message { get; private set; }

        public static readonly ArgumentParseResult DefaultNoArguments = new ArgumentParseResult("No arguments given");
        public static readonly ArgumentParseResult SuccessfullParse = new ArgumentParseResult("Successful parse!");

        static ArgumentParseResult()
        {
            DefaultNoArguments.Success = true;
            SuccessfullParse.Success = true;
        }

        /// <summary>
        /// Creates an ArgumentParseResult with a simple error message
        /// </summary>
        /// <param name="errormessage">The error message text</param>
        public ArgumentParseResult(string errormessage)
        {
            Message = errormessage;
        }

        /// <summary>
        /// Creates an ArgumentParseResult based on a CommandArgument
        /// </summary>
        /// <param name="argument">Command Argument which could not be parsed</param>
        public ArgumentParseResult(Argument argument)
        {
            Message = $"*`{argument}`*: Failed to parse!";
        }

        /// <summary>
        /// Creates a detailed ArgumentParseResult based on a CommandArgument
        /// </summary>
        /// <param name="argument">Command Argument which could not be parsed</param>
        /// <param name="errormessage">Error message text that explains why parsing failed</param>
        public ArgumentParseResult(Argument argument, string errormessage)
        {
            Message = $"*`{argument}`*: {errormessage}";
        }

        public override string ToString()
        {
            return Message;
        }
    }
}
