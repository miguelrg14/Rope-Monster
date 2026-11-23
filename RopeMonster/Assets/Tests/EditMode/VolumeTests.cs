using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.Audio;

namespace RopeMonster.Menu.Tests
{
    internal sealed class FakeVolumeStore : IVolumeStore
    {
        public readonly Dictionary<VolumeChannel, float> Values = new Dictionary<VolumeChannel, float>();

        public bool TryGet(VolumeChannel channel, out float percent) => Values.TryGetValue(channel, out percent);

        public void Set(VolumeChannel channel, float percent) => Values[channel] = percent;
    }

    public class VolumeSettingsTests
    {
        [TestCase(100f, 0f)]
        [TestCase(50f, -6.0206f)]
        [TestCase(10f, -20f)]
        [TestCase(1f, -40f)]
        [TestCase(0f, -80f)]
        public void PercentToDecibels_UsesLogarithmicCurve(float percent, float expectedDb)
        {
            Assert.That(VolumeSettings.PercentToDecibels(percent), Is.EqualTo(expectedDb).Within(0.001f));
        }

        [TestCase(-25f, -80f)]
        [TestCase(250f, 0f)]
        public void PercentToDecibels_ClampsOutOfRangeValues(float percent, float expectedDb)
        {
            Assert.That(VolumeSettings.PercentToDecibels(percent), Is.EqualTo(expectedDb).Within(0.001f));
        }

        [Test]
        public void ClampPercent_ReturnsDefaultForNaN()
        {
            Assert.That(VolumeSettings.ClampPercent(float.NaN), Is.EqualTo(VolumeSettings.DefaultPercent));
        }

        [TestCase(49.6f, "50")]
        [TestCase(0f, "0")]
        [TestCase(120f, "100")]
        public void FormatPercent_RoundsAndClamps(float percent, string expected)
        {
            Assert.That(VolumeSettings.FormatPercent(percent), Is.EqualTo(expected));
        }

        [Test]
        public void EveryChannelHasItsOwnParameterAndKey()
        {
            var parameters = new HashSet<string>();
            var keys = new HashSet<string>();

            foreach (VolumeChannel channel in VolumeSettings.AllChannels)
            {
                Assert.That(parameters.Add(VolumeSettings.MixerParameter(channel)), Is.True);
                Assert.That(keys.Add(VolumeSettings.PrefsKey(channel)), Is.True);
            }
        }
    }

    /// <summary>Store logic without a mixer (mixer behaviour is covered by the PlayMode tests).</summary>
    public class VolumeControllerTests
    {
        private FakeVolumeStore store;
        private VolumeController controller;

        [SetUp]
        public void SetUp()
        {
            store = new FakeVolumeStore();
            controller = new VolumeController(null, store);
        }

        [Test]
        public void GetPercent_ReturnsDefaultWhenNothingIsSaved()
        {
            Assert.That(controller.GetPercent(VolumeChannel.Music), Is.EqualTo(VolumeSettings.DefaultPercent));
        }

        [Test]
        public void GetPercent_ClampsCorruptedSavedValues()
        {
            store.Values[VolumeChannel.Music] = -30f;

            Assert.That(controller.GetPercent(VolumeChannel.Music), Is.EqualTo(0f));
        }

        [Test]
        public void SetPercent_ClampsBeforeStoring()
        {
            float applied = controller.SetPercent(VolumeChannel.Interface, 400f);

            Assert.That(applied, Is.EqualTo(100f));
            Assert.That(store.Values[VolumeChannel.Interface], Is.EqualTo(100f));
        }

        [Test]
        public void ResetToDefaults_RestoresEveryChannel()
        {
            controller.SetPercent(VolumeChannel.Music, 10f);
            controller.SetPercent(VolumeChannel.Effects, 20f);

            controller.ResetToDefaults();

            foreach (VolumeChannel channel in VolumeSettings.AllChannels)
                Assert.That(store.Values[channel], Is.EqualTo(VolumeSettings.DefaultPercent));
        }

        [Test]
        public void Apply_WithoutMixer_ReturnsFalse()
        {
            Assert.That(controller.Apply(VolumeChannel.Music, 50f), Is.False);
        }
    }

    public class MainAudioMixerAssetTests
    {
        private const string MixerPath = "Assets/Audio/MainAudioMixer.mixer";

        [Test]
        public void ExposesAParameterForEveryChannel()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            Assert.That(mixer, Is.Not.Null, "MainAudioMixer not found");

            var exposed = new HashSet<string>();
            SerializedProperty list = new SerializedObject(mixer).FindProperty("m_ExposedParameters");
            for (int i = 0; i < list.arraySize; i++)
                exposed.Add(list.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue);

            foreach (VolumeChannel channel in VolumeSettings.AllChannels)
                Assert.That(exposed, Does.Contain(VolumeSettings.MixerParameter(channel)));
        }

        [Test]
        public void HasMusicSfxAndUiGroups()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);

            Assert.That(mixer.FindMatchingGroups("Master/Music"), Has.Length.EqualTo(1));
            Assert.That(mixer.FindMatchingGroups("Master/SFX"), Has.Length.EqualTo(1));
            Assert.That(mixer.FindMatchingGroups("Master/UI"), Has.Length.EqualTo(1));
        }
    }
}
