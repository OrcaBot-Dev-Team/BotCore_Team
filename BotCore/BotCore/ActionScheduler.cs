using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BotCoreNET
{
    public static class ActionScheduler
    {
        private static readonly object listlock = new object();
        private static List<SchedulerEntry> schedulerEntries = new List<SchedulerEntry>();
        private static List<SchedulerEntry> toBeAdded = new List<SchedulerEntry>();
        private static List<SchedulerEntry> toBeRemoved = new List<SchedulerEntry>();
        internal static IReadOnlyList<SchedulerEntry> SchedulerEntries => schedulerEntries.AsReadOnly();

        private static Thread thread;

        static ActionScheduler()
        {
            thread = new Thread(new ThreadStart(InitiateSchedulerThread));
            thread.Start();
        }

        private static void InitiateSchedulerThread()
        {
            SchedulerLoop().GetAwaiter().GetResult();
        }

        private static async Task SchedulerLoop()
        {
            TimeSpan sleepDelay = TimeSpan.FromSeconds(1);
            while (true)
            {
                AsyncDelegate action = getAction();
                lock (listlock)
                {
                    schedulerEntries.RemoveRange(toBeRemoved);
                    toBeRemoved.Clear();
                    schedulerEntries.AddRange(toBeAdded);
                    toBeAdded.Clear();
                }
                if (action != null)
                {
                    try
                    {
                        await action();
                    }
                    catch (Exception e)
                    {
                        await ExceptionHandler.ReportException(e, "ACTNSCHDL", $"Failed to execute action `{action.Method.Name}`");
                    }
                }
                else
                {
                    await Task.Delay(sleepDelay);
                }
            }
        }

        private static AsyncDelegate getAction()
        {
            AsyncDelegate action = null;
            foreach (SchedulerEntry entry in schedulerEntries)
            {
                if (entry.IsDue)
                {
                    action = entry.Action;
                    toBeRemoved.Add(entry);
                    break;
                }
            }

            return action;
        }

        public static void AddSchedulerEntry(DateTimeOffset dueTime, AsyncDelegate callback)
        {
            AddSchedulerEntry(new SchedulerEntry(dueTime, callback));
        }

        public static void AddSchedulerEntry(TimeSpan delay, AsyncDelegate callback)
        {
            AddSchedulerEntry(new SchedulerEntry(DateTimeOffset.UtcNow + delay, callback));
        }

        public static void AddSchedulerEntry(SchedulerEntry entry)
        {
            lock (listlock)
            {
                toBeAdded.Add(entry);
            }
        }

        public static void RemoveSchedulerEntry(AsyncDelegate callback)
        {
            lock (listlock)
            {
                if (schedulerEntries.TryFind(entry => { return entry.Action == callback; }, out SchedulerEntry toberemoved))
                {
                    toBeRemoved.Add(toberemoved);
                }
            }
        }

        public static void RemoveSchedulerEntry(SchedulerEntry entry)
        {
            lock (listlock)
            {
                toBeRemoved.Add(entry);
            }
        }
    }

    public class SchedulerEntry
    {
        public readonly DateTimeOffset DueTime;
        public readonly AsyncDelegate Action;

        public SchedulerEntry(DateTimeOffset duetime, AsyncDelegate action)
        {
            DueTime = duetime;
            Action = action;
        }

        public bool IsDue { get { return DateTimeOffset.Now > DueTime; } }
    }

    public delegate Task AsyncDelegate();
}
