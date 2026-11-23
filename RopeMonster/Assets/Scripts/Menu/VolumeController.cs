using UnityEngine;
using UnityEngine.Audio;

namespace RopeMonster.Menu
{
    /// <summary>Persistence abstraction so the volume logic can be tested without PlayerPrefs.</summary>
    public interface IVolumeStore
    {
        bool TryGet(VolumeChannel channel, out float percent);
        void Set(VolumeChannel channel, float percent);
    }

    public sealed class PlayerPrefsVolumeStore : IVolumeStore
    {
        public bool TryGet(VolumeChannel channel, out float percent)
        {
            string key = VolumeSettings.PrefsKey(channel);

            if (!PlayerPrefs.HasKey(key))
            {
                percent = VolumeSettings.DefaultPercent;
                return false;
            }

            percent = PlayerPrefs.GetFloat(key);
            return true;
        }

        public void Set(VolumeChannel channel, float percent)
        {
            PlayerPrefs.SetFloat(VolumeSettings.PrefsKey(channel), percent);
            PlayerPrefs.Save();
        }
    }

    /// <summary>Reads, stores and applies the volume of every mixer channel.</summary>
    public sealed class VolumeController
    {
        private readonly AudioMixer mixer;
        private readonly IVolumeStore store;

        public VolumeController(AudioMixer mixer, IVolumeStore store)
        {
            this.mixer = mixer;
            this.store = store;
        }

        public float GetPercent(VolumeChannel channel) =>
            store.TryGet(channel, out float percent)
                ? VolumeSettings.ClampPercent(percent)
                : VolumeSettings.DefaultPercent;

        /// <summary>Stores the new value and applies it to the mixer.</summary>
        public float SetPercent(VolumeChannel channel, float percent)
        {
            percent = VolumeSettings.ClampPercent(percent);
            store.Set(channel, percent);
            Apply(channel, percent);
            return percent;
        }

        /// <summary>Returns false when the mixer does not expose the parameter.</summary>
        public bool Apply(VolumeChannel channel, float percent)
        {
            if (mixer == null)
                return false;

            return mixer.SetFloat(
                VolumeSettings.MixerParameter(channel),
                VolumeSettings.PercentToDecibels(percent));
        }

        /// <summary>Applies the saved (or default) value of every channel.</summary>
        public void ApplySaved()
        {
            foreach (VolumeChannel channel in VolumeSettings.AllChannels)
            {
                if (!Apply(channel, GetPercent(channel)))
                {
                    Debug.LogWarning(
                        $"[Audio] Mixer parameter '{VolumeSettings.MixerParameter(channel)}' is not exposed.");
                }
            }
        }

        public void ResetToDefaults()
        {
            foreach (VolumeChannel channel in VolumeSettings.AllChannels)
                SetPercent(channel, VolumeSettings.DefaultPercent);
        }
    }
}
