using System;
using System.Collections.Generic;
using Base.PackageInstaller.Data;
using UnityEngine;

namespace Base.PackageInstaller.Operations.Persistence
{
    /// <summary>
    /// Serializable snapshot of a running package operation.
    /// Used to persist progress across editor domain reloads so a run can resume
    /// after a package installation or removal triggers a recompile.
    /// </summary>
    /// <remarks>
    /// The immutable data structs are mirrored into serializable form here, so those structs stay
    /// decoupled from the persistence layer. <see cref="PackageRequest"/> is the exception: it is
    /// serializable to begin with, precisely because it exists to survive a reload.
    /// </remarks>
    [Serializable]
    internal sealed class PackageOperationState
    {
        [SerializeField] private PackageRequest[] remainingRequests;
        [SerializeField] private SerializableResult[] results;
        [SerializeField] private SerializableSnapshotEntry[] snapshot;
        [SerializeField] private bool hasSnapshot;

        /// <summary>The packages that still need to be processed, head first.</summary>
        internal PackageRequest[] RemainingRequests => remainingRequests ?? Array.Empty<PackageRequest>();

        /// <summary>True if the pre-operation installed snapshot has been captured.</summary>
        internal bool HasSnapshot => hasSnapshot;

        /// <summary>
        /// Builds a serializable state from the live operation data.
        /// </summary>
        /// <param name="remaining">The not-yet-completed packages, head first.</param>
        /// <param name="results">The results gathered so far.</param>
        /// <param name="snapshot">The pre-operation installed packages keyed by name.</param>
        /// <param name="hasSnapshot">Whether the snapshot has already been captured.</param>
        /// <returns>A state object ready to be serialized.</returns>
        internal static PackageOperationState Create(IReadOnlyCollection<PackageRequest> remaining,
            IReadOnlyList<PackageResult> results, IReadOnlyDictionary<string, InstalledPackage> snapshot,
            bool hasSnapshot)
        {
            PackageRequest[] pending = new PackageRequest[remaining.Count];
            int index = 0;

            foreach (PackageRequest request in remaining)
                pending[index++] = request;

            return new PackageOperationState
            {
                remainingRequests = pending,
                results = ToSerializable(results),
                snapshot = ToSerializable(snapshot),
                hasSnapshot = hasSnapshot
            };
        }

        /// <summary>
        /// Rebuilds the gathered results in their original order.
        /// </summary>
        /// <returns>The results restored from the serialized form.</returns>
        internal List<PackageResult> GetResults()
        {
            List<PackageResult> restored = new();

            if (results == null)
                return restored;

            foreach (SerializableResult result in results)
                restored.Add(result.ToResult());

            return restored;
        }

        /// <summary>
        /// Rebuilds the pre-operation installed snapshot keyed by package name.
        /// </summary>
        /// <returns>The snapshot restored from the serialized form.</returns>
        internal Dictionary<string, InstalledPackage> GetSnapshot()
        {
            Dictionary<string, InstalledPackage> restored = new();

            if (snapshot == null)
                return restored;

            foreach (SerializableSnapshotEntry entry in snapshot)
                restored[entry.Name] = entry.ToInstalledPackage();

            return restored;
        }

        private static SerializableResult[] ToSerializable(IReadOnlyList<PackageResult> source)
        {
            SerializableResult[] target = new SerializableResult[source.Count];

            for (int i = 0; i < source.Count; i++)
                target[i] = new SerializableResult(source[i]);

            return target;
        }

        private static SerializableSnapshotEntry[] ToSerializable(IReadOnlyDictionary<string, InstalledPackage> source)
        {
            SerializableSnapshotEntry[] target = new SerializableSnapshotEntry[source.Count];

            int index = 0;

            foreach (KeyValuePair<string, InstalledPackage> pair in source)
                target[index++] = new SerializableSnapshotEntry(pair.Key, pair.Value);

            return target;
        }
    }
}