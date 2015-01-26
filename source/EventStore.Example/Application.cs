using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NeedfulThings.EventStore.MongoDB;

namespace NeedfulThings.EventStore.Example
{
    internal sealed class Application
    {
        private readonly EventStoreRepository<ProcessMonitor> _repository = new EventStoreRepository<ProcessMonitor>(new MongoEventStore());

        public void Run()
        {
            var aggregateId = Guid.NewGuid();

            var token = new Memory<int>();

            while (!Console.KeyAvailable)
            {
                var processes = GetProcesses();

                token.Value = processes.Sum(p => p.Id);
                if (!token.Changed)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                var stopwatch = Stopwatch.StartNew();
                var aggregate = _repository.GetById(aggregateId, id => new ProcessMonitor(id));
                stopwatch.Stop();

                Console.WriteLine(aggregate.Version + " - " + stopwatch.Elapsed);

                aggregate.InvalidateProcesses(processes);
                _repository.Save(aggregate);
            }
        }

        private IEnumerable<ProcessMonitor.ProcessInfo> GetProcesses()
        {
            var processes = new List<ProcessMonitor.ProcessInfo>();

            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    processes.Add(ProcessMonitor.ProcessInfo.FromProcess(process));
                }
                catch
                {
                }
            }

            return processes;
        }

        class Memory<T>
        {
            private T _value;
            private bool _changed;

            public T Value
            {
                get { return _value; }
                set
                {
                    _changed = !_value.Equals(value);

                    if (_changed)
                        _value = value;
                }
            }

            public bool Changed
            {
                get { return _changed; }
            }
        }
    }
}