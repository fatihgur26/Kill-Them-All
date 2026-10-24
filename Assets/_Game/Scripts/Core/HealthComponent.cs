using System;
using UnityEngine;

namespace KillThemAll.Core
{
    [DisallowMultipleComponent]
    public class HealthComponent : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private bool destroyOnDeath = false;

        private float _currentHealth;
        private bool _isDead;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => _isDead;

        public event Action<float, float> OnHealthChanged;
        public event Action OnDied;

        private void Awake()
        {
            ResetHealth(maxHealth);
        }

        public void Initialize(float newMaxHealth)
        {
            maxHealth = Mathf.Max(1f, newMaxHealth);
            ResetHealth(maxHealth);
        }

        public void ResetHealth(float initialHealth)
        {
            maxHealth = Mathf.Max(1f, initialHealth);
            _currentHealth = maxHealth;
            _isDead = false;
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        public void TakeDamage(float damage)
        {
            if (_isDead || damage <= 0f) return;

            _currentHealth = Mathf.Max(0f, _currentHealth - damage);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);

            if (_currentHealth <= 0f && !_isDead)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (_isDead || amount <= 0f) return;

            _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        private void Die()
        {
            _isDead = true;
            OnDied?.Invoke();

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }
    }
}
