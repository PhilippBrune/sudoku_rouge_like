using System;
using System.Threading.Tasks;
using UnityEngine;

namespace SudokuRoguelike.Save
{
    public sealed class SaveWriteQueue
    {
        private readonly object _lock = new object();
        private Task _pendingWrite = Task.CompletedTask;
        private int _enqueuedWrites;
        private int _completedWrites;
        private int _failedWrites;
        private Exception _lastError;

        public static SaveWriteQueue Shared { get; } = new SaveWriteQueue();

        public int EnqueuedWrites
        {
            get { lock (_lock) return _enqueuedWrites; }
        }

        public int CompletedWrites
        {
            get { lock (_lock) return _completedWrites; }
        }

        public int FailedWrites
        {
            get { lock (_lock) return _failedWrites; }
        }

        public Exception LastError
        {
            get { lock (_lock) return _lastError; }
        }

        public bool HasPendingWrites
        {
            get
            {
                lock (_lock)
                    return !_pendingWrite.IsCompleted;
            }
        }

        public void Enqueue(Action writeAction)
        {
            if (writeAction == null) throw new ArgumentNullException(nameof(writeAction));

            lock (_lock)
            {
                _enqueuedWrites++;
                _pendingWrite = _pendingWrite.ContinueWith(
                    _ => ExecuteWrite(writeAction),
                    TaskScheduler.Default);
            }
        }

        public void Flush()
        {
            Task pending;
            lock (_lock)
                pending = _pendingWrite;

            pending.GetAwaiter().GetResult();
        }

        public void ResetDiagnostics()
        {
            lock (_lock)
            {
                _enqueuedWrites = 0;
                _completedWrites = 0;
                _failedWrites = 0;
                _lastError = null;
            }
        }

        private void ExecuteWrite(Action writeAction)
        {
            try
            {
                writeAction();
                lock (_lock)
                    _completedWrites++;
            }
            catch (Exception e)
            {
                lock (_lock)
                {
                    _failedWrites++;
                    _lastError = e;
                }
                Debug.LogError($"[SaveWriteQueue] Save write failed: {e.Message}");
            }
        }
    }
}
