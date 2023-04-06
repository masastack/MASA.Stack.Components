using System.Runtime.CompilerServices;

namespace Masa.Stack.Components.Extensions
{
    public class AwaitableTask
    {
        public bool NotExecutable { get; private set; }

        public void SetNotExecutable()
        {
            NotExecutable = true;
        }

        public bool IsInvalid { get; private set; } = true;

        public void MarkTaskValid()
        {
            IsInvalid = false;
        }

        private readonly Task _task;

        public AwaitableTask(Task task) => _task = task;

        public bool IsCompleted => _task.IsCompleted;

        public int TaskId => _task.Id;

        public void Start() => _task.Start();

        public void RunSynchronously() => _task.RunSynchronously();

        public TaskAwaiter GetAwaiter() => new(this);

        public readonly struct TaskAwaiter : INotifyCompletion
        {
            private readonly AwaitableTask _task;

            public TaskAwaiter(AwaitableTask awaitableTask) => _task = awaitableTask;

            public bool IsCompleted => _task._task.IsCompleted;

            public void OnCompleted(Action continuation)
            {
                var This = this;
                _task._task.ContinueWith(t =>
                {
                    if (!This._task.NotExecutable) continuation?.Invoke();
                });
            }

            public void GetResult() => _task._task.Wait();
        }
    }

    public class AwaitableTask<TResult> : AwaitableTask
    {
        private readonly Task<TResult> _task;

        public AwaitableTask(Task<TResult> task) : base(task) => _task = task;

        public new TaskAwaiter GetAwaiter() => new TaskAwaiter(this);

        public new readonly struct TaskAwaiter : INotifyCompletion
        {
            private readonly AwaitableTask<TResult> _task;

            public TaskAwaiter(AwaitableTask<TResult> awaitableTask) => _task = awaitableTask;

            public bool IsCompleted => _task._task.IsCompleted;

            public void OnCompleted(Action continuation)
            {
                var This = this;
                _task._task.ContinueWith(t =>
                {
                    if (!This._task.NotExecutable) continuation?.Invoke();
                });
            }

            public TResult GetResult() => _task._task.Result;
        }
    }

    public class AsyncTaskQueue : IDisposable
    {
        public AsyncTaskQueue()
        {
            _autoResetEvent = new AutoResetEvent(false);
            _thread = new Thread(InternalRunning) { IsBackground = true };
            _thread.Start();
        }

        public async Task<(bool isInvalid, T result)> ExecuteAsync<T>(Func<Task<T>> func)
        {
            var task = GetExecutableTask(func);
            var result = await await task;
            if (!task.IsInvalid)
            {
                result = default(T);
            }
            return (task.IsInvalid, result);
        }

        public async Task<bool> ExecuteAsync<T>(Func<Task> func)
        {
            var task = GetExecutableTask(func);
            await await task;
            return task.IsInvalid;
        }

        private AwaitableTask GetExecutableTask(Action action)
        {
            var awaitableTask = new AwaitableTask(new Task(action));
            AddPenddingTaskToQueue(awaitableTask);
            return awaitableTask;
        }

        private AwaitableTask<TResult> GetExecutableTask<TResult>(Func<TResult> function)
        {
            var awaitableTask = new AwaitableTask<TResult>(new Task<TResult>(function));
            AddPenddingTaskToQueue(awaitableTask);
            return awaitableTask;
        }

        private void AddPenddingTaskToQueue(AwaitableTask task)
        {
            lock (_queue)
            {
                _queue.Enqueue(task);
                _autoResetEvent.Set();
            }
        }

        private void InternalRunning()
        {
            while (!_isDisposed)
            {
                if (_queue.Count == 0)
                {
                    _autoResetEvent.WaitOne();
                }
                while (TryGetNextTask(out var task))
                {
                    if (task.NotExecutable) continue;

                    if (UseSingleThread)
                    {
                        task.RunSynchronously();
                    }
                    else
                    {
                        task.Start();
                    }
                }
            }
        }

        private AwaitableTask _lastDoingTask;
        private bool TryGetNextTask(out AwaitableTask task)
        {
            task = default;
            while (_queue.Count > 0)
            {
                if (_queue.TryDequeue(out task) && (!AutoCancelPreviousTask || _queue.Count == 0))
                {
                    _lastDoingTask?.MarkTaskValid();
                    _lastDoingTask = task;
                    return true;
                }
                task.SetNotExecutable();
            }
            return false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AsyncTaskQueue() => Dispose(false);

        private void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                _autoResetEvent.Dispose();
            }
            _thread = null;
            _autoResetEvent = null;
            _isDisposed = true;
        }

        public bool UseSingleThread { get; set; } = true;

        public bool AutoCancelPreviousTask { get; set; } = false;

        private bool _isDisposed;
        private readonly ConcurrentQueue<AwaitableTask> _queue = new ConcurrentQueue<AwaitableTask>();
        private Thread _thread;
        private AutoResetEvent _autoResetEvent;
    }
}