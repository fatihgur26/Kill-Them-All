using KillThemAll.Core;
using KillThemAll.Enemies;
using KillThemAll.Weapons;
using UnityEngine;
using UnityEngine.Pool;

namespace KillThemAll.Player
{
    [RequireComponent(typeof(PlayerController))]
    [DisallowMultipleComponent]
    public class PlayerCombatController : MonoBehaviour
    {
        [Header("Combat Settings")]
        [SerializeField] private float attackRange = 8f;
        [SerializeField] private float fireRate = 1.2f; // Saniyede atış sayısı (1.2 = ~0.83sn cooldown)
        [SerializeField] private float bulletDamage = 25f;
        [SerializeField] private LayerMask enemyLayer;

        [Header("Projectile Prefab & Origin")]
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private Transform firePoint;

        [Header("Pool Settings")]
        [SerializeField] private int defaultPoolCapacity = 20;
        [SerializeField] private int maxPoolSize = 100;

        private IObjectPool<Projectile> _projectilePool;
        private readonly Collider[] _hitBuffer = new Collider[32]; // Zero-alloc OverlapSphere buffer
        private float _nextFireTime;
        private PlayerRangeVisualizer _rangeVisualizer;

        public float AttackRange => attackRange;

        private void Awake()
        {
            _rangeVisualizer = GetComponent<PlayerRangeVisualizer>();
            if (_rangeVisualizer != null)
            {
                attackRange = _rangeVisualizer.Range;
            }

            InitializePool();
        }

        private void InitializePool()
        {
            _projectilePool = new ObjectPool<Projectile>(
                createFunc: CreatePooledProjectile,
                actionOnGet: OnGetFromPool,
                actionOnRelease: OnReleaseToPool,
                actionOnDestroy: OnDestroyPooledObject,
                collectionCheck: true,
                defaultCapacity: defaultPoolCapacity,
                maxSize: maxPoolSize
            );
        }

        private Projectile CreatePooledProjectile()
        {
            if (projectilePrefab == null)
            {
                Debug.LogError("[PlayerCombatController] Projectile prefab atanmadı!");
                return null;
            }

            Projectile instance = Instantiate(projectilePrefab);
            instance.SetPool(_projectilePool);
            instance.gameObject.SetActive(false);
            return instance;
        }

        private void OnGetFromPool(Projectile proj)
        {
            proj.OnSpawnFromPool();
        }

        private void OnReleaseToPool(Projectile proj)
        {
            proj.OnReturnToPool();
            proj.gameObject.SetActive(false);
        }

        private void OnDestroyPooledObject(Projectile proj)
        {
            if (proj != null)
            {
                Destroy(proj.gameObject);
            }
        }

        private void Update()
        {
            if (Time.time < _nextFireTime) return;

            Transform nearestEnemy = FindNearestEnemy();
            if (nearestEnemy != null)
            {
                ShootAt(nearestEnemy);
                _nextFireTime = Time.time + (1f / fireRate);
            }
        }

        private Transform FindNearestEnemy()
        {
            Vector3 origin = transform.position;
            int count = Physics.OverlapSphereNonAlloc(origin, attackRange, _hitBuffer, enemyLayer);

            Transform nearest = null;
            float minSqrDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider col = _hitBuffer[i];
                if (col == null) continue;

                // Oyuncunun kendisini veya çocuk objelerini hedef alma
                if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

                // Sadece "Enemy" etiketli veya EnemyController bileşeni olan düşmanları hedef al
                if (!col.CompareTag("Enemy") && !col.TryGetComponent<EnemyController>(out _)) continue;

                // IDamageable olmalı ve hayatta olmalı
                if (!col.TryGetComponent<IDamageable>(out var damageable) || damageable.IsDead)
                {
                    continue;
                }

                float sqrDist = (col.transform.position - origin).sqrMagnitude;
                if (sqrDist < minSqrDistance)
                {
                    minSqrDistance = sqrDist;
                    nearest = col.transform;
                }
            }

            return nearest;
        }

        private void ShootAt(Transform target)
        {
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 0.5f;
            Vector3 targetPos = target.position + Vector3.up * 0.5f;
            Vector3 direction = (targetPos - spawnPos).normalized;

            Projectile proj = _projectilePool.Get();
            if (proj != null)
            {
                proj.Launch(spawnPos, direction, bulletDamage);
            }
        }

        public void SetFirePoint(Transform point)
        {
            firePoint = point;
        }

        public void SetProjectilePrefab(Projectile prefab)
        {
            projectilePrefab = prefab;
        }

        public void SetEnemyLayer(LayerMask mask)
        {
            enemyLayer = mask;
        }
    }
}
