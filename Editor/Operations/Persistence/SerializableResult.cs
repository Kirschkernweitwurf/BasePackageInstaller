using System;
using Base.PackageInstaller.Data;
using UnityEngine;

namespace Base.PackageInstaller.Operations.Persistence
{
    /// <summary>
    /// Serializable mirror of <see cref="PackageResult"/>.
    /// </summary>
    [Serializable]
    internal struct SerializableResult
    {
        [SerializeField] private string label;
        [SerializeField] private string name;
        [SerializeField] private string version;
        [SerializeField] private string previousVersion;
        [SerializeField] private bool changed;
        [SerializeField] private bool success;
        [SerializeField] private string error;
        [SerializeField] private EPackageAction action;

        /// <summary>Creates the serializable mirror of a single result.</summary>
        /// <param name="result">The result to mirror.</param>
        internal SerializableResult(PackageResult result)
        {
            label = result.Label;
            name = result.Name;
            version = result.Version;
            previousVersion = result.PreviousVersion;
            changed = result.Changed;
            success = result.Success;
            error = result.Error;
            action = result.Action;
        }

        /// <summary>Rebuilds the immutable result this entry was created from.</summary>
        /// <returns>The restored result.</returns>
        internal PackageResult ToResult()
            => new(label, name, version, previousVersion, changed, success, error, action);
    }
}