using KillThemAll.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace KillThemAll.Weapons
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class Projectile : MonoBehaviour, IPoolable
    {
        [Header("Projectile Specs")]
        [SerializeField] private float speed = 18f;
        [SerializeField] private float damage = 25f;
        [SerializeField] private float maxLifetime = 3f;

        private IObjectPool<Projectile> _originPool;
        private Rigidbody _rigidbody;
        private float _currentLifetime;
        private bool _isReturned;

        public float Damage => damage;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody != null)
            {
                _rigidbody.useGravity = false;
                _rigidbody.isKinematic = true; // Hareketimizi Update içinde kontrollü yapıyoruz
            }

            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        public void SetPool(IObjectPool<Projectile> pool)
        {
            _originPool = pool;
        }

        public void Launch(Vector3 spawnPosition, Vector3 direction, float customDamage = -1f)
        {
            transform.position = spawnPosition;
            direction.y = 0f; // Düz yatay düzlemde uçsun
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }

            if (customDamage > 0f)
            {
                damage = customDamage;
            }

            _currentLifetime = 0f;
            _isReturned = false;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (_isReturned) return;

            // İleri yönde hareket
            transform.position += transform.forward * (speed * Time.deltaTime);

            _currentLifetime += Time.deltaTime;
            if (_currentLifetime >= maxLifetime)
            {
                ReturnToPool();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isReturned) return;

            // Oyuncuya kendi mermisi çarpmasın
            if (other.CompareTag("Player")) return;

            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                if (!damageable.IsDead)
                {
                    damageable.TakeDamage(damage);
                    ReturnToPool();
                }
            }
            else if (other.CompareTag("Obstacle") || other.CompareTag("Environment"))
            {
                ReturnToPool();
            }
        }

        public void OnSpawnFromPool()
        {
            _currentLifetime = 0f;
            _isReturned = false;
        }

        public void OnReturnToPool()
        {
            _isReturned = true;
        }

        private void ReturnToPool()
        {
            if (_isReturned) return;
            _isReturned = true;

            if (_originPool != null)
            {
                _originPool.Release(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
