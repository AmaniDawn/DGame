using System;
using UnityEngine;

namespace DGame
{
    [Serializable]
    public class AnimationWrapper
    {
        public int Layer;
        public WrapMode WrapMode;
        public AnimationClip Clip;
        public float FadeDuration = 0.25f;
    }
}