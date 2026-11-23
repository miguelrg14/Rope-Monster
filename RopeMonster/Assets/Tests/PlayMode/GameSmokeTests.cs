using System.Collections;
using System.Linq;
using NUnit.Framework;
using RopeMonster.Menu;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RopeMonster.Tests
{
    /// <summary>
    /// End-to-end checks that run the real scenes: the main menu boots, its buttons
    /// work, the audio mixer responds and every level loads and runs.
    /// </summary>
    public class GameSmokeTests
    {
        private const string MainMenu = "MainMenu";
        private const int FramesToRun = 90;

        private static IEnumerator LoadScene(string sceneName)
        {
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneName));
        }

        private static IEnumerator RunFrames(int frames)
        {
            for (int i = 0; i < frames; i++)
                yield return null;
        }

        private static T FindOne<T>() where T : Object
        {
            T found = Object.FindObjectsOfType<T>(true).FirstOrDefault();
            Assert.That(found, Is.Not.Null, $"{typeof(T).Name} not found in '{SceneManager.GetActiveScene().name}'");
            return found;
        }

        private static Button FindButton(string name)
        {
            Button button = Object.FindObjectsOfType<Button>(true).FirstOrDefault(b => b.name == name);
            Assert.That(button, Is.Not.Null, $"Button '{name}' not found");
            return button;
        }

        [TearDown]
        public void RestoreTime() => Time.timeScale = 1f;

        [UnityTest]
        public IEnumerator MainMenu_BootsWithOnlyTheMainPanelVisible()
        {
            yield return LoadScene(MainMenu);
            yield return RunFrames(5);

            Assert.That(FindButton("PlayButton").gameObject.activeInHierarchy, Is.True);
            Assert.That(FindOne<AudioOptionsManager>().gameObject.activeInHierarchy, Is.False);
        }

        [UnityTest]
        public IEnumerator MainMenu_OptionsButtonOpensOptionsAndBackReturns()
        {
            yield return LoadScene(MainMenu);
            yield return null;

            FindButton("Options_Button").onClick.Invoke();
            yield return null;

            GameObject options = FindOne<AudioOptionsManager>().gameObject;
            Assert.That(options.activeInHierarchy, Is.True);

            options.GetComponentsInChildren<Button>(true).First(b => b.name == "Back_Button").onClick.Invoke();
            yield return null;

            Assert.That(options.activeInHierarchy, Is.False);
            Assert.That(FindButton("PlayButton").gameObject.activeInHierarchy, Is.True);
        }

        [UnityTest]
        public IEnumerator MainMenu_ClickSoundsAreRoutedThroughTheUiGroup()
        {
            yield return LoadScene(MainMenu);
            yield return null;

            AudioSource source = FindOne<MenuAudio>().Source;
            Assert.That(source, Is.Not.Null);
            Assert.That(source.outputAudioMixerGroup, Is.Not.Null);
            Assert.That(source.outputAudioMixerGroup.name, Is.EqualTo("UI"));
        }

        [UnityTest]
        public IEnumerator Mixer_AppliesVolumeChangesInDecibels()
        {
            yield return LoadScene(MainMenu);
            yield return null;

            AudioMixer mixer = FindOne<MenuAudio>().Source.outputAudioMixerGroup.audioMixer;
            var store = new MemoryStore();
            var volume = new VolumeController(mixer, store);

            foreach (VolumeChannel channel in VolumeSettings.AllChannels)
            {
                Assert.That(volume.Apply(channel, 50f), Is.True, $"{channel} is not exposed");
                Assert.That(mixer.GetFloat(VolumeSettings.MixerParameter(channel), out float db), Is.True);
                Assert.That(db, Is.EqualTo(-6.02f).Within(0.05f));
            }

            foreach (VolumeChannel channel in VolumeSettings.AllChannels)
                mixer.ClearFloat(VolumeSettings.MixerParameter(channel));
        }

        [UnityTest]
        public IEnumerator PlayButton_OpensLevelSelect_AndLevelOneButtonLoadsLevelOne()
        {
            yield return LoadScene(MainMenu);
            yield return null;

            FindButton("PlayButton").onClick.Invoke();
            yield return null;

            Button levelOne = Object.FindObjectsOfType<Button>(true)
                .FirstOrDefault(b => b.name.StartsWith("Level_Button")
                    && b.GetComponentInChildren<TMPro.TMP_Text>(true).text.Trim() == "1");

            Assert.That(levelOne, Is.Not.Null, "Level 1 button not found");
            Assert.That(levelOne.gameObject.activeInHierarchy, Is.True, "Play should open the level select menu");
            Assert.That(FindButton("PlayButton").gameObject.activeInHierarchy, Is.False);

            levelOne.onClick.Invoke();
            yield return RunFrames(10);

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Lvl_1"));
        }

        [UnityTest]
        public IEnumerator LevelSelect_LoadsTheChosenLevel([Values(0, 3, 8)] int buttonIndex)
        {
            yield return LoadScene(MainMenu);
            yield return null;

            FindOne<LevelSelectLoader>().LoadLevel(buttonIndex);
            yield return RunFrames(10);

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo($"Lvl_{buttonIndex + 1}"));
        }

        [UnityTest]
        public IEnumerator Level_LoadsAndRuns([Values(1, 2, 3, 4, 5, 6, 7, 8, 9)] int level)
        {
            yield return LoadScene($"Lvl_{level}");
            yield return RunFrames(FramesToRun);

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo($"Lvl_{level}"));
            Assert.That(Object.FindObjectOfType<Camera>(), Is.Not.Null);
            Assert.That(FindOne<MenuAudio>().Source.outputAudioMixerGroup.name, Is.EqualTo("UI"));
        }

        private sealed class MemoryStore : IVolumeStore
        {
            private readonly System.Collections.Generic.Dictionary<VolumeChannel, float> values =
                new System.Collections.Generic.Dictionary<VolumeChannel, float>();

            public bool TryGet(VolumeChannel channel, out float percent) => values.TryGetValue(channel, out percent);

            public void Set(VolumeChannel channel, float percent) => values[channel] = percent;
        }
    }
}
