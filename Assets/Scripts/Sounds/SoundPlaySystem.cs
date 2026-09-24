using Unity.Entities;
using UnityEngine;

namespace Sounds
{
    //AudioSource는 managed API라 Burst/Job에서 부를 수 없다. 그래서 SystemBase로 main thread에서 재생한다.
    //Simulation에서 쌓인 요청을 같은 프레임 Presentation에서 처리하고 비운다.
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class SoundPlaySystem : SystemBase
    {
        private AudioSource _audioSource;
        private readonly bool[] _playedThisFrame = new bool[2];

        protected override void OnCreate()
        {
            RequireForUpdate<PlaySoundRequest>();
            RequireForUpdate<SoundClipElement>();
        }

        protected override void OnDestroy()
        {
            if (_audioSource != null)
                Object.Destroy(_audioSource.gameObject);
        }

        protected override void OnUpdate()
        {
            DynamicBuffer<PlaySoundRequest> requests = SystemAPI.GetSingletonBuffer<PlaySoundRequest>();
            if (requests.IsEmpty)
                return;

            DynamicBuffer<SoundClipElement> clips = SystemAPI.GetSingletonBuffer<SoundClipElement>(true);
            AudioSource source = GetOrCreateAudioSource();

            System.Array.Clear(_playedThisFrame, 0, _playedThisFrame.Length);

            foreach (PlaySoundRequest request in requests)
            {
                //적 여러 마리가 한 프레임에 죽어도 같은 소리는 한 번만 낸다. 겹치면 소리가 뭉개지고 커진다.
                int typeIndex = (int)request.Type;
                if (_playedThisFrame[typeIndex])
                    continue;
                _playedThisFrame[typeIndex] = true;

                PlayRandom(source, clips, request.Type);
            }

            requests.Clear();
        }

        private static void PlayRandom(AudioSource source, DynamicBuffer<SoundClipElement> clips, SoundType type)
        {
            int count = 0;
            foreach (SoundClipElement element in clips)
            {
                if (element.Type == type)
                    count++;
            }
            if (count == 0)
                return;

            int pick = Random.Range(0, count);
            foreach (SoundClipElement element in clips)
            {
                if (element.Type != type)
                    continue;
                if (pick-- > 0)
                    continue;

                //UnityObjectRef.Value가 실제 AudioClip을 돌려준다(managed라 main thread에서만).
                AudioClip clip = element.Clip.Value;
                if (clip != null)
                    source.PlayOneShot(clip, element.Volume);
                return;
            }
        }

        //SubScene 안의 GameObject는 bake되면 사라지므로, 재생용 AudioSource는 런타임에 직접 만든다.
        private AudioSource GetOrCreateAudioSource()
        {
            if (_audioSource != null)
                return _audioSource;

            var go = new GameObject("ECS Sound Player");
            _audioSource = go.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f; //2D 게임이라 거리 감쇠 없이 재생한다.
            return _audioSource;
        }
    }
}
