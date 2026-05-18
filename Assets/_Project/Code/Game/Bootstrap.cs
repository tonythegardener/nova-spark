using NovaSpark.Mods;
using NovaSpark.Powerups;
using UnityEngine;

namespace NovaSpark.Game
{
    // Phase 0a content-loading choice: ScriptableObject instances live under
    // Assets/_Project/Content/Resources/<Type>/ so Resources.LoadAll<T>("") finds them
    // in builds without an Addressables dependency. Will be revisited if/when
    // Addressables or runtime JSON mod loading becomes necessary.
    [DefaultExecutionOrder(-1000)]
    public sealed class Bootstrap : MonoBehaviour
    {
        private void Awake()
        {
            ContentRegistry.Clear();
            LoadAllPowerups();
            LogRegisteredContent();
        }

        private static void LoadAllPowerups()
        {
            var defs = Resources.LoadAll<PowerupDefinition>(string.Empty);
            foreach (var def in defs)
            {
                ContentRegistry.Register(def);
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
