using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LovelyBytes.AnimationTextures
{
    public class PlayAnimation : MonoBehaviour
    {
        [SerializeField]
        private AnimationClip _clip;
        
        [SerializeField]
        private Animator _animator;

        private PlayableGraph _playableGraph;
        
        private void Awake()
        {
            _playableGraph = PlayableGraph.Create();
            _playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            
            var playableOutput = AnimationPlayableOutput.Create(_playableGraph, "Animation", _animator);
            var clipPlayable = AnimationClipPlayable.Create(_playableGraph, _clip);
            playableOutput.SetSourcePlayable(clipPlayable);
            _playableGraph.Play();
        }

        private void OnDestroy()
        {
            _playableGraph.Destroy();
        }
    }
}