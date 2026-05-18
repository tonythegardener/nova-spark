using System;
using System.Collections.Generic;
using System.Linq;

namespace NovaSpark.Mods
{
    public static class ContentRegistry
    {
        private static readonly Dictionary<Type, Dictionary<string, IContentDefinition>> _byType = new();

        public static int Count
        {
            get
            {
                int total = 0;
                foreach (var bucket in _byType.Values) total += bucket.Count;
                return total;
            }
        }

        public static void Register(IContentDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (string.IsNullOrWhiteSpace(definition.ContentId))
                throw new ArgumentException("ContentId must be non-empty", nameof(definition));

            var type = definition.GetType();
            if (!_byType.TryGetValue(type, out var bucket))
            {
                bucket = new Dictionary<string, IContentDefinition>(StringComparer.Ordinal);
                _byType[type] = bucket;
            }

            if (bucket.ContainsKey(definition.ContentId))
                throw new InvalidOperationException(
                    $"Duplicate content id '{definition.ContentId}' for type {type.Name}");

            bucket[definition.ContentId] = definition;
        }

        public static T Get<T>(string contentId) where T : class, IContentDefinition
        {
            if (_byType.TryGetValue(typeof(T), out var bucket)
                && bucket.TryGetValue(contentId, out var def))
            {
                return def as T;
            }
            return null;
        }

        public static bool TryGet<T>(string contentId, out T value) where T : class, IContentDefinition
        {
            value = Get<T>(contentId);
            return value != null;
        }

        public static IEnumerable<T> All<T>() where T : class, IContentDefinition
        {
            if (_byType.TryGetValue(typeof(T), out var bucket))
            {
                foreach (var def in bucket.Values) yield return (T)def;
            }
        }

        public static IEnumerable<string> AllIds()
        {
            foreach (var bucket in _byType.Values)
                foreach (var id in bucket.Keys)
                    yield return id;
        }

        public static IEnumerable<Type> RegisteredTypes() => _byType.Keys;

        public static void Clear() => _byType.Clear();
    }
}
