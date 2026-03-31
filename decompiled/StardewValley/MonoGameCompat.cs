// MonoGame → FNA Compatibility Layer
// Bridges API differences between MonoGame and FNA for Stardew Valley

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Audio
{
    /// <summary>CueDefinition shim for FNA (MonoGame-specific class)</summary>
    public class CueDefinition
    {
        public string name;
        public SoundEffect[] sounds;
        public int instanceLimit = 8;
        public event Action OnModified;

        public CueDefinition() { }

        public void SetSound(SoundEffect sound, int categoryID, bool loop, string name)
        {
            sounds = new SoundEffect[] { sound };
        }

        public void SetSound(SoundEffect sound, int categoryID, bool loop = false)
        {
            sounds = new SoundEffect[] { sound };
        }
    }

    /// <summary>Stub for OggStreamSoundEffect (MonoGame-specific)</summary>
    public class OggStreamSoundEffect
    {
        // Minimal stub - FNA handles audio differently
        // Cannot inherit SoundEffect in FNA (sealed)
        public TimeSpan Duration { get; set; }
        public string Name { get; set; }
        
        public static OggStreamSoundEffect FromStream(Stream stream)
        {
            return new OggStreamSoundEffect();
        }
    }
}

namespace Microsoft.Xna.Framework
{
    /// <summary>TextInputEventArgs shim for FNA</summary>
    public class TextInputEventArgs : EventArgs
    {
        public char Character { get; private set; }
        public Input.Keys Key { get; private set; }

        public TextInputEventArgs(char character, Input.Keys key = Input.Keys.None)
        {
            Character = character;
            Key = key;
        }
    }
}

namespace MonoGameCompat
{
    /// <summary>Extension methods to bridge MonoGame→FNA SpriteBatch API differences</summary>
    public static class SpriteBatchExtensions
    {
        /// <summary>MonoGame's 3-arg Begin overload: Begin(sortMode, blendState, samplerState)</summary>
        public static void Begin(this SpriteBatch spriteBatch, SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState)
        {
            spriteBatch.Begin(sortMode, blendState, samplerState, null, null, null, Matrix.Identity);
        }
    }

    /// <summary>Extension methods for MonoGame-specific Texture2D properties</summary>
    public static class Texture2DExtensions
    {
        public static int ActualWidth(this Texture2D texture) => texture.Width;
        public static int ActualHeight(this Texture2D texture) => texture.Height;
        public static void SetImageSize(this Texture2D texture, int width, int height) { /* no-op in FNA */ }
    }

    /// <summary>Extension methods for MonoGame-specific GameWindow properties</summary>
    public static class GameWindowExtensions
    {
        public static int GetDisplayIndex(this GameWindow window) => 0;
        public static Rectangle GetDisplayBounds(this GameWindow window)
        {
            return new Rectangle(0, 0, 1920, 1080);
        }
        public static void CenterOnDisplay(this GameWindow window, int display = 0) { }
    }

    /// <summary>Extension methods for MonoGame-specific GraphicsDeviceManager properties</summary>
    public static class GraphicsDeviceManagerExtensions
    {
        private static bool _hardwareModeSwitch = true;
        public static bool GetHardwareModeSwitch(this GraphicsDeviceManager gdm) => _hardwareModeSwitch;
        public static void SetHardwareModeSwitch(this GraphicsDeviceManager gdm, bool value) { _hardwareModeSwitch = value; }
    }

    /// <summary>Extension methods for MonoGame-specific Cue properties</summary>
    public static class CueExtensions
    {
        public static float GetPitch(this Cue cue) => 0f;
        public static void SetPitch(this Cue cue, float value) { }
        public static float GetVolume(this Cue cue) => 1f;
        public static void SetVolume(this Cue cue, float value) { }
        public static bool GetIsPitchBeingControlledByRPC(this Cue cue) => false;
    }

    /// <summary>Extension methods for MonoGame-specific AudioEngine methods</summary>
    public static class AudioEngineExtensions
    {
        public static float[] GetReverbSettings(this AudioEngine engine) => new float[0];
        public static int GetCategoryIndex(this AudioEngine engine, string name) => 0;
    }

    /// <summary>Extension methods for MonoGame-specific SoundBank methods</summary>
    public static class SoundBankExtensions
    {
        public static void AddCue(this SoundBank bank, CueDefinition definition) { }
        public static bool Exists(this SoundBank bank, string name) => true;
        public static CueDefinition GetCueDefinition(this SoundBank bank, string name)
        {
            return new CueDefinition { name = name };
        }
    }

    /// <summary>Extension for SoundEffect.FromStream 2-arg overload</summary>
    public static class SoundEffectExtensions
    {
        public static SoundEffect FromStream(Stream stream, bool dummy)
        {
            return SoundEffect.FromStream(stream);
        }
    }

    /// <summary>0.001f shim</summary>
    public static class SpriteBatchConstants
    {
        public static float TextureTuckAmount => 0.001f;
    }
}
