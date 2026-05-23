#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace Base.PackageInstaller.Editor
{
    /// <summary>
    /// Base class for sequential package operations.
    /// </summary>
    public abstract class PackageOperation
    {
        /// <summary>
        /// Invoked when a package operation starts.
        /// The string parameter is the name of the package being processed.
        /// </summary>
        public event Action<string> OnPackageStarted;

        /// <summary>
        /// Invoked when a package operation completes successfully.
        /// The string parameter is the name of the completed package.
        /// </summary>
        public event Action<string> OnPackageCompleted;

        /// <summary>
        /// Invoked when a package operation fails.
        /// The string parameter is the error message describing the failure.
        /// </summary>
        public event Action<string> OnPackageFailed;

        /// <summary>
        /// Invoked when all package operations have completed successfully.
        /// </summary>
        public event Action OnAllPackagesCompleted;

        /// <summary>
        /// Indicates whether a package operation is currently running.
        /// </summary>
        public bool IsRunning { get; private set; }

        private readonly Queue<string> _queue = new();

        protected Request CurrentRequest;

        /// <summary>
        /// Starts processing the given package URLs sequentially.
        /// If an operation is already running, this method does nothing.
        /// </summary>
        /// <param name="packageUrls">The URLs of the packages to process.</param>
        public void Run(IEnumerable<string> packageUrls)
        {
            if (IsRunning)
                return;

            _queue.Clear();

            foreach (string url in packageUrls)
                _queue.Enqueue(url);

            if (_queue.Count == 0)
                return;

            IsRunning = true;
            ProcessNext();
        }

        /// <summary>
        /// Creates a package manager request for the given URL.
        /// </summary>
        /// <param name="url">The URL of the package to process.</param>
        /// <returns>A request object representing the package operation.</returns>
        protected abstract Request CreateRequest(string url);

        /// <summary>
        /// Gets the name of the package from the current request.
        /// </summary>
        /// <returns>The name of the package being processed.</returns>
        protected abstract string GetPackageName();

        private void ProcessNext()
        {
            if (_queue.Count == 0)
            {
                IsRunning = false;
                OnAllPackagesCompleted?.Invoke();
                return;
            }

            string url = _queue.Dequeue();

            OnPackageStarted?.Invoke(url);

            CurrentRequest = CreateRequest(url);

            EditorApplication.update += OnProgress;
        }

        private void OnProgress()
        {
            if (CurrentRequest is not { IsCompleted: true })
                return;

            EditorApplication.update -= OnProgress;

            if (CurrentRequest.Status == StatusCode.Failure)
            {
                IsRunning = false;

                OnPackageFailed?.Invoke(CurrentRequest.Error?.message ?? "Unknown error");
                return;
            }

            OnPackageCompleted?.Invoke(GetPackageName());

            ProcessNext();
        }
    }
}
#endif