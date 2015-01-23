using System;
using System.Collections.Generic;
using System.Text;
using DiacloLib;
using System.Runtime.InteropServices;

namespace Diaclo
{
    /// <summary>
    /// Abstracted sound playback. Uses FMOD sound engine internally.
    /// </summary>
    public class SoundManager
    {
        private Dictionary<string, SoundEntry> sounds;
        private FMOD.System soundSystem = null;

        private class SoundEntry
        {
            public FMOD.Sound Sound;
            public FMOD.Channel LastChannel;
            public FMOD.CREATESOUNDEXINFO exinfo = new FMOD.CREATESOUNDEXINFO();
        }
        public SoundManager()
        {
            this.sounds = new Dictionary<string, SoundEntry>();
            Setup();
        }
        private void Setup()
        {
            /*
                Initialize sound system
            */
            CheckResult(FMOD.Factory.System_Create(ref soundSystem));
            CheckResult(soundSystem.init(Settings.SOUND_CHANNELS, FMOD.INITFLAGS.NORMAL, (IntPtr)null)); 
        }
        /// <summary>
        /// Load a sound from memory.
        /// </summary>
        /// <param name="audiodata">File data</param>
        /// <param name="name">Filename (key/index)</param>
        public void LoadSound(byte[] audiodata, string name)
        {
            SoundEntry se = new SoundEntry();
            se.exinfo.cbsize = Marshal.SizeOf(se.exinfo);
            se.exinfo.length = (uint)audiodata.Length;
                        
            FMOD.RESULT result = soundSystem.createSound(audiodata, (FMOD.MODE.HARDWARE | FMOD.MODE.OPENMEMORY), ref se.exinfo, ref se.Sound);
            if (result == FMOD.RESULT.OK)
            {
                this.sounds.Add(name, se);
            }
            else
            {
                GameConsole.Write("Failed to load sound: " + name);
                GameConsole.Write("   -> " + FMOD.Error.String(result));
            }
            
        }
        public void PlaySound(string name)
        {
            SoundEntry se = null;
            this.sounds.TryGetValue(name, out se);
            if (se != null)
            {
                CheckResult(soundSystem.playSound(FMOD.CHANNELINDEX.FREE, se.Sound, false, ref se.LastChannel));
            }
        }
        public bool SoundLoaded(string name)
        {
            return this.sounds.ContainsKey(name);
        }
        /// <summary>
        /// Release a sound resource
        /// </summary>
        /// <param name="name"></param>
        public void ReleaseSound(string name)
        {
            SoundEntry se = null;
            this.sounds.TryGetValue(name, out se);
            if (se != null)
            {
                CheckResult(se.Sound.release());
                this.sounds.Remove(name);
            } else {
                GameConsole.Write("SoundManager.ReleaseSound: Sound not found. (" + name + ")", ConsoleMessageTypes.Debug);
            }
            
        }
        private void CheckResult(FMOD.RESULT result)
        {
            if (result != FMOD.RESULT.OK)
            {
                GameConsole.Write("SoundManager error! " + result + " - " + FMOD.Error.String(result));
            }
        }
        /// <summary>
        /// Unload sounds in memory
        /// </summary>
        public void UnloadAll()
        {
            foreach (KeyValuePair<string, SoundEntry> kp in this.sounds)
                CheckResult(kp.Value.Sound.release());
        }
        /// <summary>
        /// Shut down soundsystem resources
        /// </summary>
        public void ShutDown()
        {
            /*
                Shut down
            */
            this.UnloadAll();

            if (this.soundSystem != null)
            {
                CheckResult(soundSystem.close());
                CheckResult(soundSystem.release());
            }
       
        }

        internal void Update()
        {
            this.soundSystem.update();
        }
    }
}
