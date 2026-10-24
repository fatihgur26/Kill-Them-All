using System;
using KillThemAll.Core;
using KillThemAll.Enemies.Data;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

namespace KillThemAll.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(EnemyAttack))]
    [DisallowMultipleComponent]
    public class EnemyController : MonoBehaviour, IPoolable
    {
        [Header("Data Reference")]
        [SerializeField] private EnemyDataSO defaultData;
        [SerializeField] private Renderer modelRenderer;

        private NavMeshAgent _agent;
        private HealthComponent _health;
        private EnemyAttack _attack;
        private EnemyDataSO _currentData;
        private Transform _playerTransform;
        private IObjectPool<EnemyController> _pool;
        private float _nextPathUpdateTime;
        private bool _isReturned;

        public HealthComponent Health => _health;
        public EnemyDataSO CurrentData => _currentData;
        public event Action<EnemyController> OnEnemyDied;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<HealthComponent>();
            _attack = GetComponent<EnemyAttack>();

            if (modelRenderer == null)
            {
                modelRenderer = GetComponentInChildren<Renderer>();
            }

            _health.OnDied += HandleDeath;
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnDied -= HandleDeath;
            }
        }

        public void SetPool(IObjectPool<EnemyController> pool)
        {
            _pool = pool;
        }

        public void Initialize(EnemyDataSO data, Transform player, Vector3 spawnPosition)
        {
            _currentData = data != null ? data : defaultData;
            _playerTransform = player;
            _isReturned = false;

            // NavMeshAgent pozisyonuna ışınla
            if (_agent != null)
            {
                _agent.enabled = true;
                _agent.Warp(spawnPosition);
                _agent.speed = _currentData.MoveSpeed;
                _agent.stoppingDistance = _currentData.AttackRange * 0.8f;
                _agent.isStopped = false;
            }

            // Canı sıfırla
            if (_health != null)
            {
                _health.Initialize(_currentData.MaxHealth);
            }

            // Saldırıyı başlat
            if (_attack != null)
            {
                _attack.Initialize(_currentData);
            }

            // Rengi uygula
            ApplyVisualColor(_currentData.ModelColor);

            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (_isReturned || _health.IsDead || _playerTransform == null) return;

            // Mobil performans için hedef güncelleme sıklığını seyreltiyoruz (Throttle/Staggering)
            if (Time.time >= _nextPathUpdateTime)
            {
                if (_agent.isOnNavMesh && _agent.enabled)
                {
                    _agent.SetDestination(_playerTransform.position);
                }
                _nextPathUpdateTime = Time.time + 0.15f + UnityEngine.Random.Range(0f, 0.05f);
            }
        }

        private void ApplyVisualColor(Color color)
        {
            if (modelRenderer == null) return;

            // MaterialPropertyBlock kullanarak yeni materyal kopyası oluşturmadan renklendiriyoruz (Zero-alloc)
            var propBlock = new MaterialPropertyBlock();
            modelRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", color); // URP için
            propBlock.SetColor("_Color", color);     // Standart/Built-in fallback
            modelRenderer.SetPropertyBlock(propBlock);
        }

        private void HandleDeath()
        {
            if (_isReturned) return;

            // EXP Gem düşür
            int expAmount = _currentData != null ? _currentData.ExpDropAmount : 10;
            KillThemAll.Collectibles.ExperienceGemPool.SpawnGem(transform.position, expAmount);

            OnEnemyDied?.Invoke(this);
            ReturnToPool();
        }

        public void OnSpawnFromPool()
        {
            _isReturned = false;
        }

        public void OnReturnToPool()
        {
            _isReturned = true;
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.enabled = false;
            }
        }

        public void ReturnToPool()
        {
            if (_isReturned) return;
            _isReturned = true;

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
