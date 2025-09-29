using System;
using System.Collections.Generic;

namespace Asmo.Scenes
{
    /// <summary>
    /// Lightweight service container exposed to scenes for sharing cross-cutting dependencies
    /// (input devices, audio mixers, resource caches, etc.).
    /// </summary>
    public class SceneServices
    {
        private readonly Dictionary<Type, object> _services = new();

        /// <summary>
        /// Registers or replaces a service instance accessible to scenes.
        /// </summary>
        public void Register<TService>(TService service) where TService : class
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            _services[typeof(TService)] = service;
        }

        /// <summary>
        /// Attempts to retrieve a registered service instance.
        /// </summary>
        public bool TryGet<TService>(out TService? service) where TService : class
        {
            if (_services.TryGetValue(typeof(TService), out var value))
            {
                service = (TService)value;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Retrieves a registered service or throws if it has not been provided.
        /// </summary>
        public TService GetRequired<TService>() where TService : class
        {
            if (TryGet<TService>(out var service) && service != null)
                return service;

            throw new InvalidOperationException($"No service of type {typeof(TService).Name} is registered.");
        }
    }
}
