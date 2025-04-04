using System;
using System.Linq;
using UnityEngine;

namespace LovelyBytes.AnimationTextures
{
    [CreateAssetMenu(menuName = "LovelyBytes/AnimationTextures/MultiClipInfoReference")]
    public class MultiClipInfoReference : ScriptableObject
    {
        public MultiClipInfo Value;

        [ContextMenu(nameof(RecalculateFromClips))]
        public void RecalculateFromClips()
        {
            if (Value.Entries.Length < 1)
            {
                Debug.LogError("You must add at least one entry to calculate animation data from clips");
                return;
            }
            
            var infos = new AnimationClip[Value.Entries.Length];
            
            for (int i = 0; i < Value.Entries.Length; ++i)
                infos[i] = Value.Entries[i].Clip;
            
            Value = new MultiClipInfo(infos);
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }
    }    
    
    [Serializable]
    public class ClipInfo
    {
        public AnimationClip Clip;
        public int StartFrame;
        public int FrameCount;
        public float FPS;
        public float NormalizedStartTime;
        public float NormalizedDuration;
        
        public int EndFrame => StartFrame + FrameCount - 1;
        public float NormalizedEndTime => NormalizedStartTime + NormalizedDuration;
    }
    
    [Serializable]
    public class MultiClipInfo
    {
        public readonly int TotalFrameCount;
        public ClipInfo[] Entries;
        private const string _constructorError = "At least one clip must be provided";
        
        [Obsolete(_constructorError, error: true)]
        public MultiClipInfo() {}
        
        public MultiClipInfo(params AnimationClip[] clips)
        {
            if (clips is not { Length: > 0 })
                throw new ArgumentException(_constructorError);
            
            Entries = new ClipInfo[clips.Length];

            int startFrame = 0;
            for (int i = 0; i < Entries.Length; ++i)
            {
                ClipInfo clipInfo = new();
                Entries[i] = clipInfo;
                AnimationClip clip = clips[i];
                
                clipInfo.Clip = clip;
                clipInfo.StartFrame = startFrame;
                clipInfo.FrameCount = Mathf.CeilToInt(clip.length * clip.frameRate);
                startFrame += clipInfo.FrameCount;
                clipInfo.FPS = clip.frameRate;
            }
            TotalFrameCount = startFrame;

            foreach (ClipInfo clipInfo in Entries)
            {
                clipInfo.NormalizedStartTime = (clipInfo.StartFrame + 0.5f) / TotalFrameCount;
                clipInfo.NormalizedDuration = (clipInfo.FrameCount - 1f) / TotalFrameCount;
            }
        }

        public ClipInfo GetEntryForClip(AnimationClip clip)
        {
            foreach (ClipInfo entry in Entries)
            {
                if (entry.Clip == clip)
                    return entry;
            }
            return null;
        }
    }
}