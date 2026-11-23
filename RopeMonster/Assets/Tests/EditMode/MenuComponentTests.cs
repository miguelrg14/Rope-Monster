using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RopeMonster.Menu.Tests
{
    /// <summary>Destroys every GameObject created by a test.</summary>
    public abstract class GameObjectTestBase
    {
        private readonly List<GameObject> created = new List<GameObject>();

        protected GameObject Create(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null)
                go.transform.SetParent(parent);
            created.Add(go);
            return go;
        }

        [TearDown]
        public void DestroyCreated()
        {
            foreach (GameObject go in created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            created.Clear();
        }
    }

    public class MainMenuManagerTests : GameObjectTestBase
    {
        private MainMenuManager menu;
        private GameObject main, options, wardrobe, levels, shop;

        [SetUp]
        public void SetUp()
        {
            main = Create("Main");
            options = Create("Options");
            wardrobe = Create("Wardrobe");
            levels = Create("Levels");
            shop = Create("Shop");

            menu = Create("Menu").AddComponent<MainMenuManager>();
            menu.Configure(main, options, wardrobe, levels, shop);
        }

        [Test]
        public void ShowMainMenu_HidesEverySubMenu()
        {
            options.SetActive(true);
            shop.SetActive(true);

            menu.ShowMainMenu();

            Assert.That(main.activeSelf, Is.True);
            Assert.That(options.activeSelf || wardrobe.activeSelf || levels.activeSelf || shop.activeSelf, Is.False);
        }

        [Test]
        public void ToggleOptionsMenu_False_OpensOptionsAndHidesMain()
        {
            menu.ShowMainMenu();

            menu.ToggleOptionsMenu(false);

            Assert.That(options.activeSelf, Is.True);
            Assert.That(main.activeSelf, Is.False);
        }

        [Test]
        public void ToggleLevelsMenu_True_GoesBackToMain()
        {
            menu.ToggleLevelsMenu(false);

            menu.ToggleLevelsMenu(true);

            Assert.That(levels.activeSelf, Is.False);
            Assert.That(main.activeSelf, Is.True);
        }

        [Test]
        public void ToggleShopAndWardrobe_OnlyAffectTheirOwnPanel()
        {
            menu.ShowMainMenu();

            menu.ToggleShopMenu(false);
            Assert.That(shop.activeSelf, Is.True);
            Assert.That(wardrobe.activeSelf, Is.False);

            menu.ToggleShopMenu(true);
            menu.ToggleWardrobeMenu(false);
            Assert.That(wardrobe.activeSelf, Is.True);
            Assert.That(shop.activeSelf, Is.False);
        }

        [Test]
        public void MissingPanels_DoNotThrow()
        {
            menu.Configure(main, null, null, null, null);

            Assert.DoesNotThrow(() =>
            {
                menu.ShowMainMenu();
                menu.ToggleOptionsMenu(false);
            });
        }
    }

    public class LevelSelectLoaderTests
    {
        private static readonly LevelSelectLoader.GameScene[] Levels =
        {
            LevelSelectLoader.GameScene.Lvl_1,
            LevelSelectLoader.GameScene.Lvl_2,
            LevelSelectLoader.GameScene.Lvl_3
        };

        [TestCase(0, "Lvl_1")]
        [TestCase(2, "Lvl_3")]
        public void TryGetSceneName_ReturnsTheSceneForValidIndices(int index, string expected)
        {
            Assert.That(LevelSelectLoader.TryGetSceneName(Levels, index, out string name), Is.True);
            Assert.That(name, Is.EqualTo(expected));
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void TryGetSceneName_RejectsOutOfRangeIndices(int index)
        {
            Assert.That(LevelSelectLoader.TryGetSceneName(Levels, index, out string name), Is.False);
            Assert.That(name, Is.Null);
        }

        [Test]
        public void TryGetSceneName_HandlesNullArray()
        {
            Assert.That(LevelSelectLoader.TryGetSceneName(null, 0, out _), Is.False);
        }
    }

    public class AudioOptionsManagerTests : GameObjectTestBase
    {
        private FakeVolumeStore store;
        private AudioOptionsManager options;
        private Slider music, effects, ui;

        private Slider CreateSlider(string name)
        {
            GameObject go = Create(name);
            var slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = true;
            Create(name + "Label", go.transform).AddComponent<TextMeshProUGUI>();
            return slider;
        }

        private static string LabelOf(Slider slider) => slider.GetComponentInChildren<TextMeshProUGUI>(true).text;

        [SetUp]
        public void SetUp()
        {
            store = new FakeVolumeStore();
            music = CreateSlider("Music");
            effects = CreateSlider("Effects");
            ui = CreateSlider("UI");

            options = Create("Options").AddComponent<AudioOptionsManager>();
            options.Configure(null, music, effects, ui);
        }

        [Test]
        public void Initialize_MovesSlidersToSavedValues()
        {
            store.Values[VolumeChannel.Music] = 30f;

            options.Initialize(store);

            Assert.That(music.value, Is.EqualTo(30f));
            Assert.That(effects.value, Is.EqualTo(VolumeSettings.DefaultPercent));
            Assert.That(LabelOf(music), Is.EqualTo("30"));
        }

        [Test]
        public void SetMusicVolume_SavesAndUpdatesLabel()
        {
            options.Initialize(store);

            options.SetMusicVolume(50f);

            Assert.That(store.Values[VolumeChannel.Music], Is.EqualTo(50f));
            Assert.That(LabelOf(music), Is.EqualTo("50"));
        }

        [Test]
        public void SetSfxAndUiVolume_UseTheirOwnChannels()
        {
            options.Initialize(store);

            options.SetSfxVolume(20f);
            options.SetUiVolume(70f);

            Assert.That(store.Values[VolumeChannel.Effects], Is.EqualTo(20f));
            Assert.That(store.Values[VolumeChannel.Interface], Is.EqualTo(70f));
            Assert.That(store.ContainsMusic(), Is.False);
        }

        [Test]
        public void ResetAudioOptions_RestoresDefaultsOnSliders()
        {
            options.Initialize(store);
            options.SetMusicVolume(10f);
            music.SetValueWithoutNotify(10f);

            options.ResetAudioOptions();

            Assert.That(music.value, Is.EqualTo(VolumeSettings.DefaultPercent));
            Assert.That(LabelOf(music), Is.EqualTo("100"));
        }
    }

    internal static class FakeVolumeStoreExtensions
    {
        public static bool ContainsMusic(this FakeVolumeStore store) => store.Values.ContainsKey(VolumeChannel.Music);
    }

    public class MenuAudioTests : GameObjectTestBase
    {
        private AudioClip click, buy, equip;

        [SetUp]
        public void SetUp()
        {
            click = AudioClip.Create("click", 10, 1, 44100, false);
            buy = AudioClip.Create("buy", 10, 1, 44100, false);
            equip = AudioClip.Create("equip", 10, 1, 44100, false);
        }

        [TearDown]
        public void DestroyClips()
        {
            Object.DestroyImmediate(click);
            Object.DestroyImmediate(buy);
            Object.DestroyImmediate(equip);
        }

        [Test]
        public void ResolveClip_PrefersSpecificButtonThenContainerThenDefault()
        {
            Transform wardrobe = Create("Wardrobe").transform;
            Button buyButton = Create("Buy").AddComponent<Button>();
            Button skinButton = Create("Skin", wardrobe).AddComponent<Button>();
            Button playButton = Create("Play").AddComponent<Button>();

            var buttons = new[] { new MenuAudio.ButtonSound { button = buyButton, clip = buy } };
            var containers = new[] { new MenuAudio.ContainerSound { container = wardrobe, clip = equip } };

            Assert.That(MenuAudio.ResolveClip(buyButton, buttons, containers, click), Is.SameAs(buy));
            Assert.That(MenuAudio.ResolveClip(skinButton, buttons, containers, click), Is.SameAs(equip));
            Assert.That(MenuAudio.ResolveClip(playButton, buttons, containers, click), Is.SameAs(click));
        }

        [Test]
        public void ResolveClip_IgnoresEntriesWithoutClip()
        {
            Button button = Create("Button").AddComponent<Button>();
            var buttons = new[] { new MenuAudio.ButtonSound { button = button, clip = null } };

            Assert.That(MenuAudio.ResolveClip(button, buttons, null, click), Is.SameAs(click));
        }
    }
}
