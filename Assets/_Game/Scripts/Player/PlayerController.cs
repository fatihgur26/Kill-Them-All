using KillThemAll.Core;
using KillThemAll.Input;
using UnityEngine;

namespace KillThemAll.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(HealthComponent))]
    [DisallowMultipleComponent]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float rotationSpeed = 15f;
        [SerializeField] private float gravity = -20f;

        [Header("Input References")]
        [SerializeField] private MobileInputProvider mobileInput;
        [SerializeField] private StandaloneInputProvider standaloneInput;

        private CharacterController _characterController;
        private HealthComponent _health;
        private Vector3 _velocity;

        public HealthComponent Health => _health;
        public Vector3 Velocity => _velocity;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _health = GetComponent<HealthComponent>();

            if (mobileInput == null) mobileInput = GetComponent<MobileInputProvider>();
            if (standaloneInput == null) standaloneInput = GetComponent<StandaloneInputProvider>();
        }

        private void Update()
        {
            if (_health != null && _health.IsDead) return;

            Vector2 input = GetCombinedInput();
            HandleMovement(input);
        }

        private Vector2 GetCombinedInput()
        {
            // Mobil joystick girdisi varsa önceliklendir, yoksa klavye girdisini kullan
            if (mobileInput != null && mobileInput.HasInput())
            {
                return mobileInput.GetMovementDirection();
            }

            if (standaloneInput != null && standaloneInput.HasInput())
            {
                return standaloneInput.GetMovementDirection();
            }

            return Vector2.zero;
        }

        private void HandleMovement(Vector2 input)
        {
            // Kamera izometrik/üstten baktığı için X -> World X, Y -> World Z
            Vector3 moveDirection = new Vector3(input.x, 0f, input.y);

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                // Karakteri hareket yönüne yumuşakça döndür
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // Yatay hareket
            Vector3 motion = moveDirection * (moveSpeed * Time.deltaTime);

            // Yerçekimi kontrolü
            if (_characterController.isGrounded && _velocity.y < 0f)
            {
                _velocity.y = -2f;
            }
            else
            {
                _velocity.y += gravity * Time.deltaTime;
            }

            motion.y = _velocity.y * Time.deltaTime;

            _characterController.Move(motion);
        }

        public void SetInputProviders(MobileInputProvider mobile, StandaloneInputProvider standalone)
        {
            mobileInput = mobile;
            standaloneInput = standalone;
        }
    }
}
