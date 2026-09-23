using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CoreSystem
{
    public partial class PlayerInputSystem : SystemBase, Controls.IPlayerActions
    {
        private Controls _controls;

        private float2 _movement;
        private float2 _mousePosition;
        private bool _shoot;

        protected override void OnCreate()
        {
            if(!SystemAPI.TryGetSingleton(out InputComponent inputComponent))
                EntityManager.CreateEntity(typeof(InputComponent));

            _controls = new Controls();
            _controls.Player.SetCallbacks(this);
            Debug.Log("Create input system");
        }

        protected override void OnStartRunning()
        {
            _controls.Enable();
        }

        protected override void OnStopRunning()
        {
            _controls.Disable();
        }

        protected override void OnUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;

            //카메라에서 z=0 평면까지의 거리를 줘야 원근/직교 카메라 모두 올바른 월드 좌표가 나온다.
            Vector3 screenPos = new Vector3(_mousePosition.x, _mousePosition.y, -cam.transform.position.z);
            Vector3 aimWorld = cam.ScreenToWorldPoint(screenPos);

            //콜백으로 갱신된 값을 싱글톤에 반영한다.
            SystemAPI.SetSingleton(new InputComponent
            {
                MousePosition = _mousePosition,
                AimWorldPosition = new float2(aimWorld.x, aimWorld.y),
                Movement = _movement,
                Shoot = _shoot
            });
        }

        protected override void OnDestroy()
        {
            _controls.Player.RemoveCallbacks(this);
            _controls.Dispose();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            _movement = context.ReadValue<Vector2>();
        }

        public void OnShoot(InputAction.CallbackContext context)
        {
            _shoot = context.ReadValueAsButton();
        }

        public void OnAim(InputAction.CallbackContext context)
        {
            _mousePosition = context.ReadValue<Vector2>();
        }
    }
}
