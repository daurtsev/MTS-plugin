using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Resto.Front.Api;

namespace MTS_plugin.Requests
{
    public class RequestQueue : IDisposable
    {
        private static RequestQueue _instance;
        private readonly List<MtsRequestObj> _hash = new List<MtsRequestObj>();
        private readonly object _syncRoot = new object();
        private bool _isRunning;
        private Timer _timer;
        public static RequestQueue Instance => _instance ??= new RequestQueue();

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private void Run()
        {
            if (_isRunning) return;
            _timer = new Timer(async _ => await ExecuteAsync(), null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
            _isRunning = true;
        }

        private void Stop()
        {
            if (!_isRunning) return;
            _timer.Change(TimeSpan.Zero, TimeSpan.FromDays(1));
            _isRunning = false;
        }

        private async Task ExecuteAsync()
        {
            MtsRequestObj task;
            lock (_syncRoot)
            {
                task = _hash.OrderByDescending(r => r.Priority).ElementAtOrDefault(0);
                _hash.Remove(task);
            }
            if (task == null) 
                return;
            try
            {
                task.Task.Start();
                await task.Task;
            }
            catch (Exception e)
            {
                PluginContext.Log.Info($"[RequestQueue.ExecuteAsync]: Ooops... One of the tasks {task.Task.Id} cannot be completed");
            }
            _hash.Remove(task);
            if (_hash.Count == 0) Stop();
        }

        public void Add(MtsRequestObj obj)
        {
            lock (_syncRoot)
            {
                _hash.Add(obj);
                Run();
            }
        }
    }
}