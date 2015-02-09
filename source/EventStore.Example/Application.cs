using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using NeedfulThings.EventStore.MongoDB;

namespace NeedfulThings.EventStore.Example
{
    internal sealed class Application
    {
        private static readonly MethodInfo GetProcessIdsMethodInfo = GetMethodInfo();

        private readonly EventStoreRepository<ProcessMonitor> _repository = new EventStoreRepository<ProcessMonitor>(new MongoEventStore());

        public void Run()
        {
            const int NumberOfEntities = 10000;

            // TODO:
            BsonClassMap.LookupClassMap(typeof(ProcessMonitor.ProcessStarted));
            BsonClassMap.LookupClassMap(typeof(ProcessMonitor.ProcessExited));

            var aggregateIds = Enumerable.Range(1, NumberOfEntities).Select(n =>
            {
                var bytes = Guid.Parse("8D0BE066-45B8-45A0-A18A-AA6B9E462A25").ToByteArray();
                bytes[0] = (byte) (n / 256);
                bytes[1] = (byte) (n % 256);

                return new Guid(bytes);
            }).ToList();

            var token = new Memory<int>();

            while (!Console.KeyAvailable)
            {
                token.Value = GetProcessIds().Sum();
                if (!token.Changed)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                var processes = GetProcesses();

                var stopwatch = Stopwatch.StartNew();
                var aggregates = aggregateIds.AsParallel().Select(aggregateId => _repository.GetById(aggregateId, id => new ProcessMonitor(id))).ToList();
                stopwatch.Stop();

                Console.WriteLine(aggregates.First().Version + " - " + stopwatch.Elapsed);

                Parallel.ForEach(aggregates, aggregate =>
                {
                    aggregate.InvalidateProcesses(processes);
                    _repository.Save(aggregate);
                });
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

        private int[] GetProcessIds()
        {
            var processIds = (int[])GetProcessIdsMethodInfo.Invoke(null, new object[0]);

            return processIds;
        }

        private static MethodInfo GetMethodInfo()
        {
            var type = Type.GetType("System.Diagnostics.ProcessManager, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            var methodInfo = type.GetMethod("GetProcessIds", Type.EmptyTypes);

            return methodInfo;
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