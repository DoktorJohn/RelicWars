using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.Common.Sound
{
    [Serializable]
    public class UISoundConfig
    {
        public bool playSound = false;
        public AudioClip soundClip;
        public bool randomizePitch = true;
        
        [Range(0f, 1f)] public float baseVolume = 1f;
        [Range(0.1f, 3f)] public float basePitch = 1f;
    }
}
