using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace PhotoBank.UnitTests
{

    public class OperationCompletedEventArgs : EventArgs
    {
        internal OperationCompletedEventArgs(int id, DateTimeOffset start, DateTimeOffset end)
        {
            Id = id;
            Start = start;
            End = end;
        }

        public int Id { get; private set; }
        public DateTimeOffset Start { get; private set; }
        public DateTimeOffset End { get; private set; }
    }

    public class DependencyManager
    {
        private class OperationData
        {
            internal int Id;
            internal Action Operation;
            internal int[] Dependencies;
            internal ExecutionContext Context;
            internal int NumRemainingDependencies;
            internal DateTimeOffset Start, End;
        }

        public void AddOperation(int id, Action operation, params int[] dependencies)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (dependencies == null)
            {
                throw new ArgumentNullException(nameof(dependencies));
            }

            var data = new OperationData
            {
                Context = ExecutionContext.Capture(),
                Id = id,
                Operation = operation,
                Dependencies = dependencies
            };

            _operations.Add(id, data);
        }

        public event EventHandler<OperationCompletedEventArgs> OperationCompleted;

        public void Execute()
        {
            lock (_stateLock)
            {
                // Fill dependency data structures 
                _dependenciesFromTo = new Dictionary<int, List<int>>();

                foreach (var op in _operations.Values)
                {
                    op.NumRemainingDependencies = op.Dependencies.Length;
                    foreach (var from in op.Dependencies)
                    {
                        if (!_dependenciesFromTo.TryGetValue(from, out var toList))
                        {
                            toList = new List<int>();
                            _dependenciesFromTo.Add(from, toList);
                        }

                        toList.Add(op.Id);
                    }
                }

                // Launch and wait
                _remainingCount = _operations.Count;
                using (_doneEvent = new ManualResetEvent(false))
                {
                    lock (_stateLock)
                    {
                        foreach (var op in _operations.Values)
                        {
                            if (op.NumRemainingDependencies == 0)
                            {
                                QueueOperation(op);
                            }
                        }
                    }

                    _doneEvent.WaitOne();
                }
            }
        }

        private void QueueOperation(OperationData data)
        {
            ThreadPool.UnsafeQueueUserWorkItem(state => ProcessOperation((OperationData)state), data);
        }

        private void ProcessOperation(OperationData data)
        {
            // Time and run the operation's delegate
            data.Start = DateTimeOffset.Now;
            if (data.Context != null)
            {
                ExecutionContext.Run(data.Context.CreateCopy(), op => ((OperationData)op).Operation(), data);
            }
            else
            {
                data.Operation();
            }

            data.End = DateTimeOffset.Now;

            // Raise the operation completed event
            OnOperationCompleted(data);

            // Signal to all that depend on this operation of its
            // completion, and potentially launch newly available
            lock (_stateLock)
            {
                if (_dependenciesFromTo.TryGetValue(data.Id, out var toList))
                {
                    foreach (var targetId in toList)
                    {
                        var targetData = _operations[targetId];
                        if (--targetData.NumRemainingDependencies == 0)
                        {
                            QueueOperation(targetData);
                        }
                    }
                }

                _dependenciesFromTo.Remove(data.Id);
                if (--_remainingCount == 0)
                {
                    _doneEvent.Set();
                }
            }
        }

        private void OnOperationCompleted(OperationData data)
        {
            var handler = OperationCompleted;
            if (handler != null)
            {
                handler(this, new OperationCompletedEventArgs(data.Id, data.Start, data.End));
            }
        }

        private readonly Dictionary<int, OperationData> _operations = new Dictionary<int, OperationData>();
        private Dictionary<int, List<int>> _dependenciesFromTo;
        private readonly object _stateLock = new object();
        private ManualResetEvent _doneEvent;
        private int _remainingCount;

        private void VerifyThatAllOperationsHaveBeenRegistered()
        {
            foreach (var op in _operations.Values)
            {
                foreach (var dependency in op.Dependencies)
                {
                    if (!_operations.ContainsKey(dependency))
                    {
                        throw new InvalidOperationException("Missing operation: " + dependency);
                    }
                }
            }
        }

        private void VerifyThereAreNoCycles()
        {
            if (CreateTopologicalSort() == null)
            {
                throw new InvalidOperationException("Cycle detected");
            }
        }

        private List<int> CreateTopologicalSort()
        {
            // Build up the dependencies graph
            var dependenciesToFrom = new Dictionary<int, List<int>>();
            var dependenciesFromTo = new Dictionary<int, List<int>>();

            foreach (var op in _operations.Values)
            {
                // Note that op.Id depends on each of op.Dependencies
                dependenciesToFrom.Add(op.Id, new List<int>(op.Dependencies));

                // Note that each of op.Dependencies is relied on by op.Id
                foreach (var depId in op.Dependencies)
                {
                    if (!dependenciesFromTo.TryGetValue(depId, out var ids))
                    {
                        ids = new List<int>();
                        dependenciesFromTo.Add(depId, ids);
                    }

                    ids.Add(op.Id);
                }
            }

            // Create the sorted list
            var overallPartialOrderingIds = new List<int>(dependenciesToFrom.Count);

            var thisIterationIds = new List<int>(dependenciesToFrom.Count);
            while (dependenciesToFrom.Count > 0)
            {
                thisIterationIds.Clear();
                foreach (var item in dependenciesToFrom)
                {
                    // If an item has zero input operations, remove it.
                    if (item.Value.Count == 0)
                    {
                        thisIterationIds.Add(item.Key);

                        // Remove all outbound edges
                        if (dependenciesFromTo.TryGetValue(item.Key, out var depIds))
                        {
                            foreach (var depId in depIds)
                            {
                                dependenciesToFrom[depId].Remove(item.Key);
                            }
                        }
                    }
                }

                // If nothing was found to remove, there's no valid sort.
                if (thisIterationIds.Count == 0) return null;

                // Remove the found items from the dictionary and
                // add them to the overall ordering
                foreach (var id in thisIterationIds)
                {
                    dependenciesToFrom.Remove(id);
                }

                overallPartialOrderingIds.AddRange(thisIterationIds);
            }

            return overallPartialOrderingIds;
        }
    }


    [TestFixture]
    public class DependencyManagerTests
    {
        [Test]
        public void TestRun()
        {
            Action oneSecond = () =>
            {
                Debug.WriteLine("Hello");
            };
            DependencyManager dm = new DependencyManager();
            dm.AddOperation(1, oneSecond);
            dm.AddOperation(2, oneSecond);
            dm.AddOperation(3, oneSecond);
            dm.AddOperation(4, oneSecond, 1);
            dm.AddOperation(5, oneSecond, 1, 2, 3);
            dm.AddOperation(6, oneSecond, 3, 4);
            dm.AddOperation(7, oneSecond, 5, 6);
            dm.AddOperation(8, oneSecond, 5);
            dm.Execute();
        }
    }
}
