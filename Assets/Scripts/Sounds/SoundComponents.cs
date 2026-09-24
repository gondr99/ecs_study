using Unity.Entities;
using UnityEngine;

namespace Sounds
{
    public enum SoundType : byte
    {
        LaserShot,
        Explosion
    }

    //Burst 시스템이 "이 소리를 재생해줘"라고 쌓아두는 요청. 실제 재생은 SoundPlaySystem(managed)이 한다.
    public struct PlaySoundRequest : IBufferElementData
    {
        public SoundType Type;
    }

    //재생 가능한 clip 목록. 같은 Type이 여러 개면 그중 하나를 랜덤으로 고른다.
    //6.6부터 class IComponentData가 deprecated라서 AudioClip은 UnityObjectRef로 unmanaged하게 들고 있는다.
    public struct SoundClipElement : IBufferElementData
    {
        public SoundType Type;
        public UnityObjectRef<AudioClip> Clip;
        public float Volume;
    }
}
