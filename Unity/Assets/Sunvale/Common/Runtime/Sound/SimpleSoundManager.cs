using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.Common.Sound
{
    public static class SimpleSoundManager
    {
        
        public static float GlobalVolume = 0.5f;

        private static AudioSource[] voices;
        private static int voiceIndex;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            
            GameObject go = new GameObject("[GUI Pack Sound Manager Audio Source]");
            go.hideFlags = HideFlags.HideInHierarchy;
            Object.DontDestroyOnLoad(go);

            
            voices = new AudioSource[5];
            for (int i = 0; i < 5; i++)
            {
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f; 
                voices[i] = source;
            }
        }

        public static void Play(UISoundConfig config)
        {
            if (config == null || !config.playSound || config.soundClip == null || voices == null)
                return;

            AudioSource source = GetVoice();

            
            float pitch = config.basePitch;
            if (config.randomizePitch)
            {
                pitch *= Random.Range(0.92f, 1.08f);
            }

            source.pitch = pitch;
            source.PlayOneShot(config.soundClip, config.baseVolume * GlobalVolume);
        }

        private static AudioSource GetVoice()
        {
            for (int i = 0; i < voices.Length; i++)
            {
                int index = (voiceIndex + i) % voices.Length;

                if (!voices[index].isPlaying)
                {
                    voiceIndex = (index + 1) % voices.Length;
                    return voices[index];
                }
            }

            
            AudioSource stolen = voices[voiceIndex];
            stolen.Stop();
            
            voiceIndex = (voiceIndex + 1) % voices.Length;
            return stolen;
        }
    }
}
