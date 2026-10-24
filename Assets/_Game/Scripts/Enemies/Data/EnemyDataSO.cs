using UnityEngine;

namespace KillThemAll.Enemies.Data
{
    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "KillThemAll/Enemy Data", order = 0)]
    public class EnemyDataSO : ScriptableObject
    {
        [Header("General Info")]
        [SerializeField] private string enemyName = "Monster";
        [SerializeField] private Color modelColor = Color.red;

        [Header("Combat Stats")]
        [SerializeField] private float maxHealth = 50f;
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float damage = 10f;
        [SerializeField] private float attackCooldown = 1.0f;
        [SerializeField] private float attackRange = 1.2f;
        [SerializeField] private int expDropAmount = 10;

        public string EnemyName => enemyName;
        public Color ModelColor => modelColor;
        public float MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float Damage => damage;
        public float AttackCooldown => attackCooldown;
        public float AttackRange => attackRange;
        public int ExpDropAmount => expDropAmount;
    }
}
