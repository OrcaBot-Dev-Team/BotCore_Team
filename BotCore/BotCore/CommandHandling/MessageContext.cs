using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotCoreNET.CommandHandling
{
    public class MessageContext : IMessageContext
    {
        public UserInformation UserInfo { get; private set; }
        public SocketUser User { get; private set; }
        public ISocketMessageChannel Channel { get; private set; }
        public SocketUserMessage Message { get; private set; }
        public string Content => Message.Content;
        public virtual bool IsDefined
        {
            get
            {
                return Message != null && User != null && Channel != null;
            }
        }

        public MessageContext(SocketUserMessage message)
        {
            Message = message;
            if (message != null)
            {
                User = message.Author;
                UserInfo = new UserInformation(User.Id);
                Channel = message.Channel;
            }
        }
    }
}
