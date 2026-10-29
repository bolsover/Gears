using System.Collections.Generic;


namespace Bolsover.Gears.Model
{
    public class ToothProfile
    {
        /// <summary>
        /// A collection of geometries, identified by a key (id or type).
        /// </summary>
        public Dictionary<string, Feature> Features { get; private set; }

        /// <summary>
        /// Constructor to initialize the collection.
        /// </summary>
        public ToothProfile()
        {
            Features = new Dictionary<string, Feature>();
        }

        /// <summary>
        /// Adds a feature to the profile using a string key.
        /// If a feature with the same key already exists, it will NOT be added again.
        /// </summary>
        /// <param id="key">Id or description of the feature.</param>
        /// <param id="feature">The feature to add.</param>
        public void AddFeature(string key, Feature feature)
        {
            if (key == null || feature == null) return;
            if (Features.ContainsKey(key)) return; // prevent duplicate keys
            Features[key] = feature;
        }

        /// <summary>
        /// Adds a feature to the profile using an enum key.
        /// Duplicate keys are not added.
        /// </summary>
        public void AddFeature(ToothFeatureKey key, Feature feature)
        {
            AddFeature(key.ToString(), feature);
        }

        /// <summary>
        /// Removes a geometry from the profile by string key.
        /// </summary>
        /// <param id="key">The key of the geometry to remove.</param>
        public void RemoveFeature(string key)
        {
            Features.Remove(key);
        }

        /// <summary>
        /// Removes a geometry from the profile by enum key.
        /// </summary>
        public void RemoveFeature(ToothFeatureKey key)
        {
            RemoveFeature(key.ToString());
        }

        /// <summary>
        /// Retrieves a geometry by its string key.
        /// </summary>
        /// <param id="key">The key of the geometry.</param>
        /// <returns>The geometry if found, otherwise null.</returns>
        public Feature GetFeature(string key)
        {
            return Features.TryGetValue(key, out var feature) ? feature : null;
        }

        /// <summary>
        /// Retrieves a geometry by its enum key.
        /// </summary>
        public Feature GetFeature(ToothFeatureKey key)
        {
            return GetFeature(key.ToString());
        }
    }
}