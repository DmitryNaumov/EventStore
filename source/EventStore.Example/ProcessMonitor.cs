using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NeedfulThings.EventStore.Example
{
    public sealed class ProcessMonitor : AggregateRoot
    {
        private readonly List<ProcessInfo> _processes = new List<ProcessInfo>();

        public ProcessMonitor()
        {
        }

        public ProcessMonitor(Guid id) : base(id)
        {
        }

        public void InvalidateProcesses(IEnumerable<ProcessInfo> processes)
        {
            var commonSet = _processes.Intersect(processes).ToList();
            
            // These processes are no longer exist
            var events = _processes.Except(commonSet)
                .Select<ProcessInfo, IEvent>(process =>
                    new ProcessExited
                    {
                        ProcessId = process.Id,
                        ProcessName = process.ProcessName
                    });

            // These are processes that just started
            events = events.Union(processes.Except(commonSet).Select<ProcessInfo, IEvent>(process =>
                new ProcessStarted
                {
                    ProcessId = process.Id,
                    StartTime = process.StartTime,
                    ProcessName = process.ProcessName,
                    HandleCount = process.HandleCount,
                    WorkingSet = process.WorkingSet
                }));

            events.ToList().ForEach(@event => ApplyEvent(@event, false));
        }

        public void ApplyEvent(ProcessStarted @event)
        {
            var process = new ProcessInfo
            {
                Id = @event.ProcessId,
                StartTime = @event.StartTime,
                ProcessName = @event.ProcessName,
                HandleCount = @event.HandleCount,
                WorkingSet = @event.WorkingSet
            };

            _processes.Add(process);
        }

        public void ApplyEvent(ProcessExited @event)
        {
            _processes.RemoveAll(process => process.Id == @event.ProcessId);
        }

        public sealed class ProcessInfo
        {
            public int Id { get; set; }

            public DateTime StartTime { get; set; }

            public string ProcessName { get; set; }

            public int HandleCount { get; set; }

            public long WorkingSet { get; set; }

            public static ProcessInfo FromProcess(Process process)
            {
                return new ProcessInfo
                {
                    Id = process.Id,
                    StartTime = process.StartTime,
                    ProcessName = process.ProcessName,
                    HandleCount = process.HandleCount,
                    WorkingSet = process.WorkingSet
                };
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;

                if (ReferenceEquals(this, obj))
                    return true;

                return obj is ProcessInfo && Equals((ProcessInfo) obj);
            }

            public override int GetHashCode()
            {
                return Id;
            }

            private bool Equals(ProcessInfo other)
            {
                return Id == other.Id;
            }
        }

        public sealed class ProcessStarted : IEvent
        {
            public int ProcessId { get; set; }

            public DateTime StartTime { get; set; }

            public string ProcessName { get; set; }

            public int HandleCount { get; set; }

            public long WorkingSet { get; set; }
        }

        public sealed class ProcessExited : IEvent
        {
            public int ProcessId { get; set; }

            public string ProcessName { get; set; }
        }
    }
}