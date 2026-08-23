using System;
using System.Collections.Generic;
using Base.PackageInstaller.Data;
using Base.PackageInstaller.Operations.Persistence;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Base.PackageInstaller.Operations
{
    /// <summary>
    /// Base class for sequential package operations.
    /// <para>
    /// Adding or removing a Git package that contains scripts triggers a recompile and therefore a
    /// domain reload, which wipes all non-serialized state. To survive this, progress is
    /// mirrored into <see cref="PackageOperationStore"/> after every step and the run is
    /// continued via <see cref="Resume"/> once the domain has reloaded.
    /// </para>
    /// </summary>
    internal abstract class PackageOperation
    {
        private const string UnknownError = "Unknown error";

        /// <summary>
        /// Invoked when a package operation starts.
        /// The string parameter is the friendly label of the package being processed.
        /// </summary>
        internal event Action<string> OnPackageStarted;

        /// <summary>
        /// Invoked when a package operation completes successfully.
        /// The parameter describes the resolved package and version.
        /// </summary>
        internal event Action<PackageResult> OnPackageCompleted;

        /// <summary>
        /// Invoked when a package operation fails.
        /// The run continues with the next package after this is raised.
        /// </summary>
        internal event Action<PackageResult> OnPackageFailed;

        /// <summary>
        /// Invoked when all package operations have finished.
        /// The summary reports how many packages succeeded, changed, or failed.
        /// </summary>
        internal event Action<OperationSummary> OnAllPackagesCompleted;

        /// <summary>
        /// Indicates whether a package operation is currently running.
        /// </summary>
        internal bool IsRunning { get; private set; }

        /// <summary>
        /// What this operation does to the packages it processes, stamped onto every result so the
        /// report can be worded without knowing which operation produced it.
        /// </summary>
        protected abstract EPackageAction Action { get; }

        /// <summary>
        /// The key under which this operation's progress is persisted.
        /// Each concrete operation type gets its own slot so independent runs never collide.
        /// </summary>
        private string PersistenceKey => GetType().Name;

        private readonly Queue<PackageRequest> _queue = new();
        private readonly List<PackageResult> _results = new();
        private readonly Dictionary<string, InstalledPackage> _installed = new();

        private Request _currentRequest;
        private PackageRequest _current;
        private ListRequest _listRequest;
        private ListRequest _verifyRequest;
        private PackageResult _pendingFailure;
        private bool _hasSnapshot;

        /// <summary>
        /// Creates a package manager request for the given identifier.
        /// </summary>
        /// <param name="id">The Git URL or package name to process.</param>
        /// <returns>A request object representing the package operation.</returns>
        protected abstract Request CreateRequest(string id);

        /// <summary>
        /// Asked whenever a request comes back as a failure, with the packages the project holds
        /// at that moment. True means the project is already in the state the request was meant to
        /// put it in, so the error describes work that is done rather than work that went wrong.
        /// </summary>
        /// <param name="request">The package whose request failed.</param>
        /// <param name="installedNames">The names of the packages currently in the project.</param>
        /// <returns>True to report the failure as a success instead.</returns>
        /// <remarks>
        /// The default is false, which reports every failure as one. An operation only overrides
        /// this when its requests are not repeatable: a run interrupted by a domain reload resumes
        /// from a queue whose head was still in flight, so that one request is issued a second
        /// time and an operation that cannot be asked twice for the same thing gets an error back.
        /// </remarks>
        protected virtual bool IsAlreadySettled(PackageRequest request, ISet<string> installedNames) => false;

        /// <summary>
        /// Starts processing the given packages sequentially.
        /// If an operation is already running, this method does nothing.
        /// </summary>
        /// <param name="requests">The packages to process, in the order they have to be processed in.</param>
        internal void Run(IEnumerable<PackageRequest> requests)
        {
            if (IsRunning)
                return;

            ResetState();

            foreach (PackageRequest request in requests)
                _queue.Enqueue(request);

            if (_queue.Count == 0)
                return;

            IsRunning = true;

            Persist();

            BeginSnapshot();
        }

        /// <summary>
        /// Resumes a run that was interrupted by a domain reload, if one is pending.
        /// Safe to call when nothing is running; it then does nothing.
        /// </summary>
        /// <remarks>
        /// Call this after the owner is re-created following a domain reload
        /// (for example from an editor window's <c>OnEnable</c>).
        /// </remarks>
        internal void Resume()
        {
            if (IsRunning)
                return;

            if (!PackageOperationStore.TryLoad(PersistenceKey, out PackageOperationState state))
                return;

            ResetState();

            foreach (PackageRequest request in state.RemainingRequests)
                _queue.Enqueue(request);

            _results.AddRange(state.GetResults());

            foreach (KeyValuePair<string, InstalledPackage> pair in state.GetSnapshot())
                _installed[pair.Key] = pair.Value;

            _hasSnapshot = state.HasSnapshot;

            IsRunning = true;

            if (_hasSnapshot)
                ProcessNext();
            else
                BeginSnapshot();
        }

        private static bool HasChanged(InstalledPackage previous, PackageInfo info)
        {
            if (!string.IsNullOrEmpty(previous.Hash) && info.git != null)
                return previous.Hash != info.git.hash;

            return previous.Version != info.version;
        }

        private void ResetState()
        {
            _queue.Clear();
            _results.Clear();
            _installed.Clear();

            _currentRequest = null;
            _current = default;
            _listRequest = null;
            _verifyRequest = null;
            _pendingFailure = default;
            _hasSnapshot = false;

            EditorApplication.update -= OnVerifyProgress;
        }

        private void BeginSnapshot()
        {
            _listRequest = Client.List(false, false);

            EditorApplication.update += OnSnapshotProgress;
        }

        private void OnSnapshotProgress()
        {
            if (_listRequest is not
                {
                    IsCompleted: true
                })
                return;

            EditorApplication.update -= OnSnapshotProgress;

            if (_listRequest.Status == StatusCode.Success && _listRequest.Result != null)
                foreach (PackageInfo info in _listRequest.Result)
                    _installed[info.name] = new InstalledPackage(info.version, info.git?.hash);

            _listRequest = null;
            _hasSnapshot = true;

            Persist();

            ProcessNext();
        }

        private void ProcessNext()
        {
            if (_queue.Count == 0)
            {
                Finish();
                return;
            }

            _current = _queue.Peek();

            OnPackageStarted?.Invoke(_current.Label);

            _currentRequest = CreateRequest(_current.Id);

            EditorApplication.update += OnProgress;
        }

        private void OnProgress()
        {
            if (_currentRequest is not
                {
                    IsCompleted: true
                })
                return;

            EditorApplication.update -= OnProgress;

            PackageResult result;

            try
            {
                result = BuildResult();
            }
            catch (Exception exception)
            {
                result = Failure(exception.Message);
            }

            _currentRequest = null;

            // A reported failure can be the report of work the project has already carried out,
            // so it is checked against the project before it is passed on as one.
            if (!result.Success)
            {
                BeginVerification(result);
                return;
            }

            Complete(result);
        }

        private void BeginVerification(PackageResult failure)
        {
            _pendingFailure = failure;
            _verifyRequest = Client.List(true, false);

            EditorApplication.update += OnVerifyProgress;
        }

        private void OnVerifyProgress()
        {
            if (_verifyRequest is not
                {
                    IsCompleted: true
                })
                return;

            EditorApplication.update -= OnVerifyProgress;

            HashSet<string> names = new();

            if (_verifyRequest.Status == StatusCode.Success && _verifyRequest.Result != null)
                foreach (PackageInfo info in _verifyRequest.Result)
                    names.Add(info.name);

            _verifyRequest = null;

            // The list itself failing tells us nothing, so the original failure stands.
            PackageResult result = IsAlreadySettled(_current, names)
                ? BareSuccess()
                : _pendingFailure;

            _pendingFailure = default;

            Complete(result);
        }

        private void Complete(PackageResult result)
        {
            _results.Add(result);
            _queue.Dequeue();

            Persist();

            if (result.Success)
                OnPackageCompleted?.Invoke(result);
            else
                OnPackageFailed?.Invoke(result);

            ProcessNext();
        }

        private PackageResult BuildResult()
        {
            if (_currentRequest.Status == StatusCode.Failure)
                return Failure(_currentRequest.Error?.message ?? UnknownError);

            // Only an AddRequest carries package info. A removal is identified by the package name
            // instead, so the version it took away comes from the snapshot taken before the run.
            if (_currentRequest is not AddRequest
                {
                    Result:
                    { } info
                })
                return BareSuccess();

            _installed.TryGetValue(info.name, out InstalledPackage previous);

            bool wasInstalled = previous.Version != null || previous.Hash != null;
            bool changed = !wasInstalled || HasChanged(previous, info);

            return new PackageResult(_current.Label, info.name, info.version, previous.Version ?? string.Empty,
                changed, true, null, Action);
        }

        // Nothing but the package itself is known, so the version it took away is looked up in the
        // snapshot taken before the run.
        private PackageResult BareSuccess()
        {
            _installed.TryGetValue(_current.Id, out InstalledPackage gone);

            return new PackageResult(_current.Label, _current.Label, string.Empty,
                gone.Version ?? string.Empty, true, true, null, Action);
        }

        private PackageResult Failure(string error) => new(_current.Label, _current.Label, string.Empty,
            string.Empty, false, false, error, Action);

        private void Finish()
        {
            EditorApplication.update -= OnProgress;
            EditorApplication.update -= OnVerifyProgress;

            _currentRequest = null;

            int success = 0;
            int failed = 0;
            int changed = 0;
            int unchanged = 0;

            foreach (PackageResult result in _results)
            {
                if (!result.Success)
                {
                    failed++;
                    continue;
                }

                success++;

                if (result.Changed)
                    changed++;
                else
                    unchanged++;
            }

            OperationSummary summary = new(_results.ToArray(), Action, success, failed, changed, unchanged);

            IsRunning = false;

            PackageOperationStore.Clear(PersistenceKey);

            OnAllPackagesCompleted?.Invoke(summary);
        }

        private void Persist()
        {
            PackageOperationState state = PackageOperationState.Create(_queue, _results, _installed, _hasSnapshot);
            PackageOperationStore.Save(PersistenceKey, state);
        }
    }
}