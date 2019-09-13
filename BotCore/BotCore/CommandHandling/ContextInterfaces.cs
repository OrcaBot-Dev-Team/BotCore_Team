using BotCoreNET.BotVars.GenericBotVars;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotCoreNET.CommandHandling
{
    public interface ICheckable
    {
        bool IsDefined { get; }
    }

    public interface IMessageContext : ICheckable
    {
        UserInformation UserInfo { get; }
        SocketUser User { get; }
        ISocketMessageChannel Channel { get; }
        SocketUserMessage Message { get; }
        string Content { get; }
    }

    public interface IGuildMessageContext : IMessageContext
    {
        SocketGuildUser GuildUser { get; }
        SocketTextChannel GuildChannel { get; }
        SocketGuild Guild { get; }
        GuildChannelMeta ChannelMeta { get; }
    }

    public interface ICommandContext : ICheckable
    {
        string ArgumentSection { get; }
        string[] Arguments { get; }
        int ArgumentCount { get; }
        int ArgPointer { get; set; }
        string Argument { get; }
        Command InterpretedCommand { get; }
        CommandSearchResult CommandSearch { get; }
        string RemoveArgumentsFront(int count);
    }

    public interface IDMCommandContext : IMessageContext, ICommandContext
    {
        bool IsGuildContext { get; }
    }

    public interface IGuildCommandContext : IGuildMessageContext, ICommandContext, IDMCommandContext { }
}
