using Unity.Entities;
using UnityEngine;

namespace Sounds
{
    //SubScene에 하나만 둔다. PlaySoundRequest / SoundClipElement buffer를 가진 싱글톤 엔티티가 된다.
    public class SoundAuthoring : MonoBehaviour
    {
        public AudioClip[] laserShotClips;
        public AudioClip[] explosionClips;
        [Range(0f, 1f)] public float laserShotVolume = 0.5f;
        [Range(0f, 1f)] public float explosionVolume = 0.8f;

        private class SoundAuthoringBaker : Baker<SoundAuthoring>
        {
            public override void Bake(SoundAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                AddBuffer<PlaySoundRequest>(entity);

                DynamicBuffer<SoundClipElement> clips = AddBuffer<SoundClipElement>(entity);
                AddClips(clips, SoundType.LaserShot, authoring.laserShotClips, authoring.laserShotVolume);
                AddClips(clips, SoundType.Explosion, authoring.explosionClips, authoring.explosionVolume);
            }

            private static void AddClips(DynamicBuffer<SoundClipElement> buffer, SoundType type, AudioClip[] clips, float volume)
            {
                if (clips == null)
                    return;

                foreach (AudioClip clip in clips)
                {
                    if (clip == null)
                        continue;
                    buffer.Add(new SoundClipElement { Type = type, Clip = clip, Volume = volume });
                }
            }
        }
    }
}
