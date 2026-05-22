using NovaSpark.Mods;
using NovaSpark.Powerups;
using NovaSpark.Run;
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
            InitializeRunStateMetaPool();
        }

        // Seed RunState.metaPool with every PowerupDefinition we have. For
        // Phase 1 this is "everything that exists"; meta unlocks in Phase 4 will
        // gate which definitions count as "unlocked" for the player.
        private static void InitializeRunStateMetaPool()
        {
            RunState.InitializeMetaPool(ContentRegistry.All<PowerupDefinition>());
            Debug.Log($"[NovaSpark] RunState meta pool initialized with {RunState.MetaPool.Count} powerups.");
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
