using System.Linq;
using UnityEngine;

namespace LovelyBytes.AnimationTextures
{
    public class MultiClipInfo
    {
        public struct Entry
        {
            public AnimationClip Clip;
            public int StartFrame;
            public int FrameCount;
            public float FPS;
            public float NormalizedStartTime;
            public float NormalizedDuration;
        }

        private readonly Entry[] _entries;
        
        public MultiClipInfo(params AnimationClip[] clips)
        {
            _entries = new Entry[clips.Length];

            int startFrame = 0;
            for (int i = 0; i < _entries.Length; ++i)
            {
                ref Entry entry = ref _entries[i];
                AnimationClip clip = clips[i];
                
                entry.Clip = clip;
                entry.StartFrame = startFrame;
                entry.FrameCount = Mathf.CeilToInt(clip.length * clip.frameRate);
                startFrame += entry.FrameCount + 1;
                entry.FPS = clip.frameRate;
            }
            int totalFrameCount = _entries.Sum(entry => entry.FrameCount) + _entries.Length - 1;

            for (int i = 0; i < _entries.Length; ++i)
            {
                ref Entry entry = ref _entries[i];
                entry.NormalizedStartTime = entry.StartFrame / (float)totalFrameCount;
                entry.NormalizedDuration = entry.FrameCount / (float)totalFrameCount;
            }
        }

        public Entry? GetEntryForClip(AnimationClip clip)
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].Clip == clip)
                    return _entries[i];
            }
            return null;
        }
        public Entry GetEntry(int index) => _entries[index];
    }
}