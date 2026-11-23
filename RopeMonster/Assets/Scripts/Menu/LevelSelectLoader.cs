using UnityEngine;
using UnityEngine.SceneManagement;

namespace RopeMonster.Menu
{
    /// <summary>Loads the level chosen in the level-select menu.</summary>
    public class LevelSelectLoader : MonoBehaviour
    {
        /// <summary>Scene names in the build. The values are serialized as integers, keep their order.</summary>
        public enum GameScene
        {
            MainMenu,
            Lvl_1,
            Lvl_2,
            Lvl_3,
            Lvl_4,
            Lvl_5,
            Lvl_6,
            Lvl_7,
            Lvl_8,
            Lvl_9
        }

        [Tooltip("Level shown by each button, in order (button 1 = element 0).")]
        [SerializeField] private GameScene[] gameLevels;

        /// <summary>Called by the level buttons with a zero-based index.</summary>
        public void LoadLevel(int index)
        {
            if (!TryGetSceneName(gameLevels, index, out string sceneName))
            {
                Debug.LogWarning($"[LevelSelect] There is no level for button index {index}.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning($"[LevelSelect] Scene '{sceneName}' is not in the build settings.");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        internal void Configure(GameScene[] levels) => gameLevels = levels;

        internal static bool TryGetSceneName(GameScene[] levels, int index, out string sceneName)
        {
            sceneName = null;

            if (levels == null || index < 0 || index >= levels.Length)
                return false;

            sceneName = levels[index].ToString();
            return true;
        }
    }
}
