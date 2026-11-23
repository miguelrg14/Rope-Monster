using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RopeMonster.Menu.Tests
{
    /// <summary>Checks how the main menu scene and the build settings are wired.</summary>
    public class MainMenuSceneTests
    {
        private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";

        private Scene scene;

        [OneTimeSetUp]
        public void OpenScene() => scene = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);

        [OneTimeTearDown]
        public void CloseScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        private T[] FindInScene<T>() where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        [Test]
        public void BuildSettings_ContainMainMenuAndNineLevelsInOrder()
        {
            string[] expected = new[] { "MainMenu" }
                .Concat(Enumerable.Range(1, 9).Select(i => $"Lvl_{i}"))
                .ToArray();

            string[] actual = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path))
                .ToArray();

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Scene_HasOneOfEachMenuComponent()
        {
            Assert.That(FindInScene<MainMenuManager>(), Has.Length.EqualTo(1));
            Assert.That(FindInScene<LevelSelectLoader>(), Has.Length.EqualTo(1));
            Assert.That(FindInScene<AudioOptionsManager>(), Has.Length.EqualTo(1));
            Assert.That(FindInScene<MenuAudio>(), Has.Length.EqualTo(1));
        }

        [Test]
        public void EveryLevelButton_LoadsTheLevelWrittenOnIt()
        {
            Button[] levelButtons = FindInScene<Button>()
                .Where(b => b.name.StartsWith("Level_Button"))
                .ToArray();

            Assert.That(levelButtons, Has.Length.EqualTo(9));

            foreach (Button button in levelButtons)
            {
                var onClick = new SerializedObject(button).FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                Assert.That(onClick.arraySize, Is.GreaterThan(0), button.name);

                SerializedProperty call = onClick.GetArrayElementAtIndex(0);
                Assert.That(call.FindPropertyRelative("m_MethodName").stringValue, Is.EqualTo(nameof(LevelSelectLoader.LoadLevel)));

                var loader = call.FindPropertyRelative("m_Target").objectReferenceValue as LevelSelectLoader;
                Assert.That(loader, Is.Not.Null, button.name);

                int index = call.FindPropertyRelative("m_Arguments.m_IntArgument").intValue;
                string label = button.GetComponentInChildren<TMP_Text>(true).text.Trim();

                Assert.That(LevelSelectLoader.TryGetSceneName(GetLevels(loader), index, out string sceneName), Is.True,
                    $"Button '{label}' passes index {index}, which is out of range");
                Assert.That(sceneName, Is.EqualTo($"Lvl_{label}"), $"Button '{label}' loads the wrong level");
            }
        }

        [Test]
        public void VolumeSliders_UseAPercentageRange()
        {
            foreach (Slider slider in FindInScene<AudioOptionsManager>()[0].GetComponentsInChildren<Slider>(true))
            {
                Assert.That(slider.minValue, Is.EqualTo(0f), slider.name);
                Assert.That(slider.maxValue, Is.EqualTo(100f), slider.name);
            }
        }

        private static LevelSelectLoader.GameScene[] GetLevels(LevelSelectLoader loader)
        {
            SerializedProperty levels = new SerializedObject(loader).FindProperty("gameLevels");
            return Enumerable.Range(0, levels.arraySize)
                .Select(i => (LevelSelectLoader.GameScene)levels.GetArrayElementAtIndex(i).enumValueIndex)
                .ToArray();
        }
    }
}
