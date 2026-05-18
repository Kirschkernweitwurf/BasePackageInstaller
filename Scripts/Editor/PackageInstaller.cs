#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace Base.PackageInstaller.Editor
{
    /// <summary>
    /// Queues and installs UPM packages sequentially.
    /// </summary>
    public class PackageInstaller
    {
        public event Action<string> OnPackageStarted;
        public event Action<string> OnPackageInstalled;
        public event Action<string> OnPackageFailed;
        public event Action OnAllPackagesInstalled;

        public bool IsInstalling { get; private set; }

        private readonly Queue<string> _queue = new();
        private AddRequest _currentRequest;

        public void Install(IEnumerable<string> packageUrls)
        {
            if (IsInstalling)
                return;

            _queue.Clear();

            foreach (string url in packageUrls)
                _queue.Enqueue(url);

            if (_queue.Count == 0)
                return;

            IsInstalling = true;
            ProcessNext();
        }

        private void ProcessNext()
        {
            if (_queue.Count == 0)
            {
                IsInstalling = false;
                OnAllPackagesInstalled?.Invoke();
                return;
            }

            string url = _queue.Dequeue();
            OnPackageStarted?.Invoke(url);

            _currentRequest = Client.Add(url);
            EditorApplication.update += OnProgress;
        }

        private void OnProgress()
        {
            if (_currentRequest is not { IsCompleted: true })
                return;

            EditorApplication.update -= OnProgress;

            if (_currentRequest.Status == StatusCode.Failure)
            {
                IsInstalling = false;
                OnPackageFailed?.Invoke(_currentRequest.Error?.message ?? "Unknown error");
                return;
            }

            OnPackageInstalled?.Invoke(_currentRequest.Result.name);
            ProcessNext();
        }
    }
}
#endif