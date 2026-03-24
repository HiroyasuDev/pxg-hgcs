using UnityEngine;
using System.Collections.Generic;

namespace PXG.DataProviders
{
    /// <summary>
    /// Interface for modular volumetric data providers injecting into the Discrete Grid.
    /// </summary>
    public interface IDataProvider
    {
        string ProviderName { get; }
        List<Vector3> FetchNodes();
        void InitializeGridInjection();
    }
}
