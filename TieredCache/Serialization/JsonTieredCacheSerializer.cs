// Author: Jacques Murray
using System.Text.Json;

namespace TieredCache.Serialization
{
    /// <summary>
    /// The default implementation of ITieredCacheSerializer using System.Text.Json.
    /// </summary>
    public class JsonTieredCacheSerializer
    {
        private readonly JsonSerializerOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonTieredCacheSerializer"/> class
        /// with default JsonSerializerOptions.
        /// </summary>
        public JsonTieredCacheSerializer()
        {
            // Default options can be configured here if needed
            _options = new JsonSerializerOptions();
        }

        /// <inheritdoc/>
        public byte[] Serialize<T>(T value)
        {
            return JsonSerializer.SerializeToUtf8Bytes(value, _options);
        }

        /// <inheritdoc/>
        public T? Deserialize<T>(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(bytes, _options);
        }
    }
}
