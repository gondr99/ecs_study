using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    //ECS World의 GameScore를 읽어서 UI Toolkit Label에 보여준다.
    //PanelRenderer는 GameObject 컴포넌트라 SubScene이 아니라 일반 Scene에 둔다.
    public class ScoreView : MonoBehaviour
    {
        //6.6: Q<Label>("이름") 대신 Inspector에서 PanelRenderer 안의 element를 골라서 직렬화한다.
        //이름이 아니라 UXML의 authoring-id 경로로 저장되므로, name을 바꿔도 참조가 끊기지 않는다.
        [SerializeField] private VisualElementReference<Label> _scoreLabelRef;

        private Label _scoreLabel;
        private EntityQuery _scoreQuery;
        private int _shownScore = -1;

        private void OnEnable()
        {
            //이미 resolve된 상태에서 등록하면 콜백이 즉시 호출된다.
            _scoreLabelRef.RegisterReferenceResolvedCallback(OnScoreLabelResolved);
            _scoreLabelRef.RegisterReferenceUnloadedCallback(OnScoreLabelUnloaded);
        }

        private void OnDisable()
        {
            _scoreLabelRef.UnregisterReferenceResolvedCallback(OnScoreLabelResolved);
            _scoreLabelRef.UnregisterReferenceUnloadedCallback(OnScoreLabelUnloaded);
            _scoreLabel = null;
        }

        private void OnScoreLabelResolved(Label label)
        {
            _scoreLabel = label;
            _shownScore = -1;
        }

        //live reload 등으로 문서가 파괴되면 호출된다. 이후 다시 resolve되면 새 Label이 들어온다.
        private void OnScoreLabelUnloaded(Label label)
        {
            _scoreLabel = null;
        }

        private void Update()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            
            if (world == null || !world.IsCreated || _scoreLabel == null)
                return;

            if (_scoreQuery == default)
                _scoreQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<GameScore>());

            if (!_scoreQuery.TryGetSingleton(out GameScore score))
                return;

            //값이 바뀔 때만 text를 갱신한다. text를 쓰면 매번 layout/repaint가 다시 일어난다.
            if (score.KillCount == _shownScore)
                return;
            _shownScore = score.KillCount;
            _scoreLabel.text = $"Score: {_shownScore}";
        }
    }
}
