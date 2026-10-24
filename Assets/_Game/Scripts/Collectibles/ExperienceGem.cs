using KillThemAll.Core;
using KillThemAll.Player;
using UnityEngine;
using UnityEngine.Pool;

namespace KillThemAll.Collectibles
{
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class ExperienceGem : MonoBehaviour, IPoolable
    {
        [Header("Gem Stats")]
        [SerializeField] private int expValue = 10;
        [SerializeField] private float magnetSpeed = 12f;
        [SerializeField] private float collectionDistance = 0.5f;

        [Header("Idle Animation")]
        [SerializeField] private float bobHeight = 0.2f;
        [SerializeField] private float bobSpeed = 3f;
        [SerializeField] private float rotationSpeed = 90f;

        private IObjectPool<ExperienceGem> _pool;
        private Transform _targetPlayer;
        private PlayerLevelSystem _playerLevel;
        private Vector3 _basePosition;
        private bool _isMagnetized;
        private bool _isReturned;
        private float _currentSpeed;

        public int ExpValue => expValue;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        public void SetPool(IObjectPool<ExperienceGem> pool)
        {
            _pool = pool;
        }

        public void Spawn(Vector3 position, int value)
        {
            transform.position = position + Vector3.up * 0.3f;
            _basePosition = transform.position;
            expValue = Mathf.Max(1, value);
            _targetPlayer = null;
            _playerLevel = null;
            _isMagnetized = false;
            _isReturned = false;
            _currentSpeed = magnetSpeed;

            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (_isReturned) return;

            if (_isMagnetized && _targetPlayer != null)
            {
                // Oyuncuya doğru ivmelenerek uç
                _currentSpeed += 15f * Time.deltaTime;
                Vector3 targetPos = _targetPlayer.position + Vector3.up * 0.5f;
                transform.position = Vector3.MoveTowards(transform.position, targetPos, _currentSpeed * Time.deltaTime);

                if (Vector3.SqrMagnitude(transform.position - targetPos) < collectionDistance * collectionDistance)
                {
                    Collect();
                }
            }
            else
            {
                // Yerde tatlı bir dönüş ve süzülme animasyonu
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
                float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
                transform.position = new Vector3(_basePosition.x, _basePosition.y + yOffset, _basePosition.z);
            }
        }

        public void AttractTo(Transform player, PlayerLevelSystem levelSystem)
        {
            if (_isMagnetized || _isReturned) return;
            _targetPlayer = player;
            _playerLevel = levelSystem;
            _isMagnetized = true;
        }

        private void Collect()
        {
            if (_isReturned) return;
            _isReturned = true;

            if (_playerLevel != null)
            {
                _playerLevel.AddExp(expValue);
            }

            ReturnToPool();
        }

        public void OnSpawnFromPool()
        {
            _isReturned = false;
            _isMagnetized = false;
        }

        public void OnReturnToPool()
        {
            _isReturned = true;
            _targetPlayer = null;
            _playerLevel = null;
        }

        private void ReturnToPool()
        {
            if (_pool != null)
            {
                _pool.Release(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
