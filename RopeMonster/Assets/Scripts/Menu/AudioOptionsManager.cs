using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace RopeMonster.Menu
{
    /// <summary>
    /// Options panel: three sliders (0-100 %) that control the Music, Effects and
    /// Interface groups of the main audio mixer. Values are saved in PlayerPrefs.
    /// </summary>
    public class AudioOptionsManager : MonoBehaviour
    {
        [SerializeField] private AudioMixer audioMixer;

        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider effectsVolumeSlider;

        [FormerlySerializedAs("uIVolumeSlider")]
        [SerializeField] private Slider interfaceVolumeSlider;

        private TextMeshProUGUI musicVolumeLabel;
        private TextMeshProUGUI effectsVolumeLabel;
        private TextMeshProUGUI interfaceVolumeLabel;

        private VolumeController volume;

        private void Awake() => Initialize(new PlayerPrefsVolumeStore());

        private void OnEnable() => RefreshSliders();

        // Called by the sliders' OnValueChanged events.
        public void SetMusicVolume(float percent) => SetVolume(VolumeChannel.Music, percent);

        public void SetSfxVolume(float percent) => SetVolume(VolumeChannel.Effects, percent);

        public void SetUiVolume(float percent) => SetVolume(VolumeChannel.Interface, percent);

        public void ResetAudioOptions()
        {
            volume.ResetToDefaults();
            RefreshSliders();
        }

        internal void Configure(AudioMixer mixer, Slider music, Slider effects, Slider ui)
        {
            audioMixer = mixer;
            musicVolumeSlider = music;
            effectsVolumeSlider = effects;
            interfaceVolumeSlider = ui;
        }

        internal void Initialize(IVolumeStore store)
        {
            volume = new VolumeController(audioMixer, store);

            musicVolumeLabel = FindLabel(musicVolumeSlider);
            effectsVolumeLabel = FindLabel(effectsVolumeSlider);
            interfaceVolumeLabel = FindLabel(interfaceVolumeSlider);

            RefreshSliders();
        }

        private void SetVolume(VolumeChannel channel, float percent)
        {
            if (volume == null)
                return;

            float applied = volume.SetPercent(channel, percent);
            SetLabel(LabelFor(channel), applied);
        }

        /// <summary>Moves the sliders to the saved values without firing their events.</summary>
        private void RefreshSliders()
        {
            if (volume == null)
                return;

            foreach (VolumeChannel channel in VolumeSettings.AllChannels)
            {
                float percent = volume.GetPercent(channel);
                Slider slider = SliderFor(channel);

                if (slider != null)
                    slider.SetValueWithoutNotify(percent);

                SetLabel(LabelFor(channel), percent);
            }
        }

        private Slider SliderFor(VolumeChannel channel)
        {
            switch (channel)
            {
                case VolumeChannel.Music: return musicVolumeSlider;
                case VolumeChannel.Effects: return effectsVolumeSlider;
                default: return interfaceVolumeSlider;
            }
        }

        private TextMeshProUGUI LabelFor(VolumeChannel channel)
        {
            switch (channel)
            {
                case VolumeChannel.Music: return musicVolumeLabel;
                case VolumeChannel.Effects: return effectsVolumeLabel;
                default: return interfaceVolumeLabel;
            }
        }

        private static TextMeshProUGUI FindLabel(Slider slider) =>
            slider != null ? slider.GetComponentInChildren<TextMeshProUGUI>(true) : null;

        private static void SetLabel(TextMeshProUGUI label, float percent)
        {
            if (label != null)
                label.text = VolumeSettings.FormatPercent(percent);
        }
    }
}
