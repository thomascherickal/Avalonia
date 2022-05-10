using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server
{
    internal class ServerCompositor : IRenderLoopTask
    {
        private readonly IRenderLoop _renderLoop;
        private readonly Queue<Batch> _batches = new Queue<Batch>(); 
        public long LastBatchId { get; private set; }
        public Stopwatch Clock { get; } = Stopwatch.StartNew();
        public TimeSpan ServerNow { get; private set; }
        private List<ServerCompositionTarget> _activeTargets = new();

        public ServerCompositor(IRenderLoop renderLoop)
        {
            _renderLoop = renderLoop;
            _renderLoop.Add(this);
        }

        public void EnqueueBatch(Batch batch)
        {
            lock (_batches) 
                _batches.Enqueue(batch);
        }

        internal void UpdateServerTime() => ServerNow = Clock.Elapsed;

        List<Batch> _reusableToCompleteList = new();
        void ApplyPendingBatches()
        {
            while (true)
            {
                Batch batch;
                lock (_batches)
                {
                    if(_batches.Count == 0)
                        break;
                    batch = _batches.Dequeue();
                }

                foreach (var change in batch.Changes)
                {
                    if (change.Dispose)
                    {
                        //TODO
                    }
                    change.Target!.Apply(change);
                    change.Reset();
                }

                _reusableToCompleteList.Add(batch);
                LastBatchId = batch.SequenceId;
            }
        }

        void CompletePendingBatches()
        {
            foreach(var batch in _reusableToCompleteList)
                batch.Complete();
            _reusableToCompleteList.Clear();
        }

        bool IRenderLoopTask.NeedsUpdate => false;
        void IRenderLoopTask.Update(TimeSpan time)
        {
        }

        void IRenderLoopTask.Render()
        {
            ApplyPendingBatches();
            foreach (var t in _activeTargets)
                t.Render();
            
            CompletePendingBatches();
        }

        public void AddCompositionTarget(ServerCompositionTarget target)
        {
            _activeTargets.Add(target);
        }

        public void RemoveCompositionTarget(ServerCompositionTarget target)
        {
            _activeTargets.Remove(target);
        }
    }
}