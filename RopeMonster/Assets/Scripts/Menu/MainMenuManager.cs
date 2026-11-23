using UnityEngine;
using UnityEngine.SceneManagement;

namespace RopeMonster.Menu
{
    /// <summary>
    /// Navigation of the main menu. Only one panel is visible at a time: the main
    /// menu or one of its sub-menus (options, wardrobe, level select, shop).
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        /// <summary>Build index of the first level (index 0 is the main menu).</summary>
        public const int FirstLevelBuildIndex = 1;

        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject optionsMenu;
        [SerializeField] private GameObject skinWardrobeMenu;
        [SerializeField] private GameObject levelsMenu;
        [SerializeField] private GameObject shopMenu;

        private void Start()
        {
            ShowMainMenu();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>Shows the main menu and hides every sub-menu.</summary>
        public void ShowMainMenu()
        {
            SetActive(mainMenu, true);
            SetActive(optionsMenu, false);
            SetActive(skinWardrobeMenu, false);
            SetActive(levelsMenu, false);
            SetActive(shopMenu, false);
        }

        // The buttons pass "false" to open a sub-menu and "true" to go back.
        public void ToggleOptionsMenu(bool showMainMenu) => Switch(optionsMenu, showMainMenu);

        public void ToggleWardrobeMenu(bool showMainMenu) => Switch(skinWardrobeMenu, showMainMenu);

        public void ToggleLevelsMenu(bool showMainMenu) => Switch(levelsMenu, showMainMenu);

        public void ToggleShopMenu(bool showMainMenu) => Switch(shopMenu, showMainMenu);

        public void Play() => SceneManager.LoadScene(FirstLevelBuildIndex);

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // No effect in WebGL builds: the browser tab owns the application.
            Application.Quit();
#endif
        }

        internal void Configure(GameObject main, GameObject options, GameObject wardrobe, GameObject levels, GameObject shop)
        {
            mainMenu = main;
            optionsMenu = options;
            skinWardrobeMenu = wardrobe;
            levelsMenu = levels;
            shopMenu = shop;
        }

        private void Switch(GameObject subMenu, bool showMainMenu)
        {
            SetActive(mainMenu, showMainMenu);
            SetActive(subMenu, !showMainMenu);
        }

        private static void SetActive(GameObject panel, bool active)
        {
            if (panel != null)
                panel.SetActive(active);
        }
    }
}
