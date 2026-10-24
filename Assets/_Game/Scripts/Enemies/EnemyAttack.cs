using KillThemAll.Core;
using KillThemAll.Enemies.Data;
using UnityEngine;

namespace KillThemAll.Enemies
{
    [DisallowMultipleComponent]
    public class EnemyAttack : MonoBehaviour
    {
        private EnemyDataSO _data;
        private float _nextAttackTime;
        private IDamageable _currentTarget;

        public void Initialize(EnemyDataSO data)
        {
            _data = data;
            _nextAttackTime = 0f;
            _currentTarget = null;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && other.TryGetComponent<IDamageable>(out var target))
            {
                _currentTarget = target;
                TryDealDamage();
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Player") && _currentTarget != null)
            {
                TryDealDamage();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _currentTarget = null;
            }
        }

        private void TryDealDamage()
        {
            if (_data == null || _currentTarget == null) return;
            if (_currentTarget.IsDead) return;

            if (Time.time >= _nextAttackTime)
            {
                _currentTarget.TakeDamage(_data.Damage);
                _nextAttackTime = Time.time + _data.AttackCooldown;
            }
        }
    }
}
