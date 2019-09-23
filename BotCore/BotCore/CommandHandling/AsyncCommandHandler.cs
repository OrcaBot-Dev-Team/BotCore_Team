using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BotCoreNET.CommandHandling
{
    class AsyncCommandHandler
    {
        const int COMMANDHANLDERTHREADCOUNT = 1;

        #region static
        private static object queueLock = new object();
        private static Queue<AsyncCommandContainer> asyncCommandQueue = new Queue<AsyncCommandContainer>();

        private static AsyncCommandHandler[] commandHandlers = new AsyncCommandHandler[COMMANDHANLDERTHREADCOUNT];

        static AsyncCommandHandler()
        {
            for (int i = 0; i < COMMANDHANLDERTHREADCOUNT; i++)
            {
                commandHandlers[i] = new AsyncCommandHandler();
            }
        }

        internal static void AddCommandContainer(AsyncCommandContainer container)
        {
            lock (queueLock)
            {
                asyncCommandQueue.Enqueue(container);
            }
        }

        #endregion
        #region thread

        private AsyncCommandHandler()
        {
            thread = new Thread(new ThreadStart(AsyncCommandHandlerLoop));
            thread.Start();
        }

        private Thread thread;
        private AsyncCommandContainer commandContainer = null;
        private static readonly TimeSpan sleep = TimeSpan.FromSeconds(0.5);

        private async void AsyncCommandHandlerLoop()
        {
            while (true)
            {
                lock (asyncCommandQueue)
                {
                    if (asyncCommandQueue.Count > 0)
                    {
                        commandContainer = asyncCommandQueue.Dequeue();
                    }
                }
                if (commandContainer != null)
                {
                    await commandContainer.Execute();
                    commandContainer = null;
                }
                else
                {
                    Thread.Sleep(sleep);
                }
            }
        }

        #endregion
    }

    class AsyncCommandContainer
    {
        private IDMCommandContext context;
        private IDisposable typingState;
        private AsyncDelegate executionDelegate;

        private AsyncCommandContainer(AsyncDelegate task, IDMCommandContext context)
        {
            executionDelegate = task;
            this.context = context;
        }

        public async Task Execute()
        {
            try
            {
                await executionDelegate();
            }
            catch (Exception e)
            {
                await context.Channel.SendMessageAsync($"Exception:", embed: Macros.EmbedFromException(e).Build());
            }
            typingState.Dispose();
        }

        internal static void NewAsyncCommand(HandleAsync handle, IDMCommandContext context)
        {
            NewAsyncCommand(() => { return handle(context); }, context);
        }

        internal static void NewAsyncCommand(HandleGuildAsync handle, IGuildCommandContext context)
        {
            NewAsyncCommand(() => { return handle(context); }, context);
        }

        private static void NewAsyncCommand(AsyncDelegate task, IDMCommandContext context)
        {
            AsyncCommandContainer container = new AsyncCommandContainer(task, context);
            container.typingState = context.Channel.EnterTypingState();
            AsyncCommandHandler.AddCommandContainer(container);
        }
    }

    internal delegate Task AsyncDelegate();
    internal delegate Task HandleAsync(IDMCommandContext context);
    internal delegate Task HandleGuildAsync(IGuildCommandContext context);
}
