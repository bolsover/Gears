using System.Collections.Generic;


namespace Bolsover.Gears.Model
{
    public class ToothProfile
    {
        /// <summary>
        /// A collection of geometries, identified by a key (name or type).
        /// </summary>
        public Dictionary<string, Geometry> Geometries { get; private set; }

        /// <summary>
        /// Constructor to initialize the collection.
        /// </summary>
        public ToothProfile()
        {
            Geometries = new Dictionary<string, Geometry>();
        }

        /// <summary>
        /// Adds a geometry to the profile.
        /// If a geometry with the same key already exists, it will overwrite it.
        /// </summary>
        /// <param name="key">Name or description of the geometry.</param>
        /// <param name="geometry">The geometry to add.</param>
        public void AddGeometry(string key, Geometry geometry)
        {
            Geometries[key] = geometry;
        }

        /// <summary>
        /// Removes a geometry from the profile by key.
        /// </summary>
        /// <param name="key">The key of the geometry to remove.</param>
        public void RemoveGeometry(string key)
        {
            if (Geometries.ContainsKey(key))
            {
                Geometries.Remove(key);
            }
        }

        /// <summary>
        /// Retrieves a geometry by its key.
        /// </summary>
        /// <param name="key">The key of the geometry.</param>
        /// <returns>The geometry if found, otherwise null.</returns>
        public Geometry GetGeometry(string key)
        {
            return Geometries.ContainsKey(key) ? Geometries[key] : null;
        }
    }
}