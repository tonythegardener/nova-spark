using NovaSpark.Mods;
using UnityEngine;

namespace NovaSpark.Game
{
    // Phase 0 content-loading choice: ScriptableObject instances live under
    // Assets/_Project/Content/Resources/<Type>/ so Resources.LoadAll<T> finds them
    // in player builds without an Addressables dependency. Will be revisited if/when
    // Addressables or runtime JSON mod loading becomes necessary.
    [DefaultExecutionOrder(-1000)]
    public sealed class Bootstrap : MonoBehaviour
    {
        private void Awake()
        {
            ContentRegistry.Clear();
            LoadAllContent();
            LogRegisteredContent();
        }

        private static void LoadAllContent()
        {
            var all = Resources.LoadAll<ScriptableObject>(string.Empty);
            foreach (var so in all)
            {
                if (so is IContentDefinition def)
                {
                    ContentRegistry.Register(def);
                }
            }
        }

        private static void LogRegisteredContent()
        {
            int count = ContentRegistry.Count;
            Debug.Log($"[NovaSpark] ContentRegistry initialized with {count} entries.");
            foreach (var id in ContentRegistry.AllIds())
            {
                Debug.Log($"[NovaSpark]   - {id}");
            }
        }
    }
}
