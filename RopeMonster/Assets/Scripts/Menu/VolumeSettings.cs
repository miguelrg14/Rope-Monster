using System;
using UnityEngine;

namespace RopeMonster.Menu
{
    /// <summary>Audio channels exposed by the main audio mixer.</summary>
    public enum VolumeChannel
    {
        Music,
        Effects,
        Interface
    }

    /// <summary>
    /// Pure helpers that translate the 0-100 % values shown in the options menu
    /// into the decibel values expected by the <see cref="UnityEngine.Audio.AudioMixer"/>.
    /// </summary>
    public static class VolumeSettings
    {
        public const float MinPercent = 0f;
        public const float MaxPercent = 100f;
        public const float DefaultPercent = 100f;

        /// <summary>Lowest attenuation Unity's mixer accepts (silence).</summary>
        public const float MinDecibels = -80f;

        public static readonly VolumeChannel[] AllChannels =
        {
            VolumeChannel.Music,
            VolumeChannel.Effects,
            VolumeChannel.Interface
        };

        /// <summary>Name of the exposed parameter in MainAudioMixer for a channel.</summary>
        public static string MixerParameter(VolumeChannel channel)
        {
            switch (channel)
            {
                case VolumeChannel.Music: return "musicVolume";
                case VolumeChannel.Effects: return "sfxVolume";
                case VolumeChannel.Interface: return "uiVolume";
                default: throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
            }
        }

        /// <summary>PlayerPrefs key used to persist a channel's volume.</summary>
        public static string PrefsKey(VolumeChannel channel) =>
            "settings.volume." + channel.ToString().ToLowerInvariant();

        public static float ClampPercent(float percent)
        {
            if (float.IsNaN(percent))
                return DefaultPercent;

            return Mathf.Clamp(percent, MinPercent, MaxPercent);
        }

        /// <summary>
        /// Converts a perceptual percentage into decibels (20·log10). 100 % is 0 dB,
        /// 50 % is about -6 dB and 0 % is silence (-80 dB).
        /// </summary>
        public static float PercentToDecibels(float percent)
        {
            percent = ClampPercent(percent);

            if (percent <= 0f)
                return MinDecibels;

            return Mathf.Max(MinDecibels, 20f * Mathf.Log10(percent / MaxPercent));
        }

        /// <summary>Text shown next to a slider (the "%" sign is a separate label).</summary>
        public static string FormatPercent(float percent) =>
            Mathf.RoundToInt(ClampPercent(percent)).ToString();
    }
}
