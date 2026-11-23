using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace RopeMonster.Menu
{
    /// <summary>
    /// Lives on an always-active UI root. On start it applies the saved volumes to the
    /// mixer and adds a click sound to every <see cref="Button"/> below it, routed
    /// through the mixer's Interface group.
    /// </summary>
    public class MenuAudio : MonoBehaviour
    {
        [Serializable]
        public class ButtonSound
        {
            public Button button;
            public AudioClip clip;
        }

        [Serializable]
        public class ContainerSound
        {
            [Tooltip("Every button below this transform plays the clip.")]
            public Transform container;
            public AudioClip clip;
        }

        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioMixerGroup interfaceGroup;
        [SerializeField] private AudioClip defaultClickSound;

        [Tooltip("Buttons that play a specific sound instead of the default click.")]
        [SerializeField] private ButtonSound[] buttonSounds = Array.Empty<ButtonSound>();

        [Tooltip("Groups of buttons (e.g. the wardrobe) that share a sound.")]
        [SerializeField] private ContainerSound[] containerSounds = Array.Empty<ContainerSound>();

        private AudioSource source;

        internal AudioSource Source => source;

        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = interfaceGroup;

            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                Button captured = button;
                captured.onClick.AddListener(() => Play(ResolveClip(captured, buttonSounds, containerSounds, defaultClickSound)));
            }
        }

        /// <summary>Logged once a scene's UI is running; the browser smoke test waits for it.</summary>
        public const string SceneReadyPrefix = "[RopeMonster] Scene ready: ";

        private void Start()
        {
            // AudioMixer.SetFloat is ignored during Awake, so the saved volumes are applied here.
            new VolumeController(audioMixer, new PlayerPrefsVolumeStore()).ApplySaved();

            Debug.Log(SceneReadyPrefix + gameObject.scene.name);
        }

        public void Play(AudioClip clip)
        {
            if (clip != null && source != null)
                source.PlayOneShot(clip);
        }

        /// <summary>Specific button sound, then container sound, then the default click.</summary>
        internal static AudioClip ResolveClip(
            Button button, ButtonSound[] buttons, ContainerSound[] containers, AudioClip fallback)
        {
            if (button == null)
                return fallback;

            if (buttons != null)
            {
                foreach (ButtonSound entry in buttons)
                {
                    if (entry != null && entry.button == button && entry.clip != null)
                        return entry.clip;
                }
            }

            if (containers != null)
            {
                foreach (ContainerSound entry in containers)
                {
                    if (entry != null && entry.container != null && entry.clip != null
                        && button.transform.IsChildOf(entry.container))
                        return entry.clip;
                }
            }

            return fallback;
        }
    }
}
