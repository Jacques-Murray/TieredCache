// Author: Jacques Murray
namespace TieredCache.Serialization
{
    /// <summary>
    /// Defines a simple contract for serializing and deserializing objects
    /// for storage in the L2 (distributed) cache.
    /// </summary>
    public interface ITieredCacheSerializer
    {
        /// <summary>
        /// Serializes the provided object into a byte array.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <returns>A byte array representing the object.</returns>
        byte[] Serialize<T>(T value);

        /// <summary>
        /// Deserializes the provided byte array into an object of type T.
        /// </summary>
        /// <param name="bytes">The byte array to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        T? Deserialize<T>(byte[] bytes);
    }
}
