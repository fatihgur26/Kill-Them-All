using System;

namespace KillThemAll.Core
{
    public interface IDamageable
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        bool IsDead { get; }
        
        event Action<float, float> OnHealthChanged; // currentHealth, maxHealth
        event Action OnDied;
        
        void TakeDamage(float damage);
        void Heal(float amount);
    }
}
