using System;
using KillThemAll.Collectibles;
using UnityEngine;

namespace KillThemAll.Player
{
    [DisallowMultipleComponent]
    public class PlayerLevelSystem : MonoBehaviour
    {
        [Header("Level Configuration")]
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private float currentExp = 0f;
        [SerializeField] private float baseExpToNextLevel = 30f;
        [SerializeField] private float expGrowthMultiplier = 1.3f;

        [Header("Magnet / Collection Range")]
        [SerializeField] private float magnetRadius = 3.5f;
        [SerializeField] private LayerMask collectibleLayer = ~0;

        private float _expToNextLevel;
        private readonly Collider[] _gemBuffer = new Collider[32]; // Zero-alloc magnet buffer
        private float _nextScanTime;

        public int CurrentLevel => currentLevel;
        public float CurrentExp => currentExp;
        public float ExpToNextLevel => _expToNextLevel;
        public float ExpPercentage => Mathf.Clamp01(currentExp / _expToNextLevel);

        public event Action<int, float, float> OnExpChanged; // level, currentExp, maxExp
        public event Action<int> OnLevelUp; // newLevel

        private void Awake()
        {
            CalculateExpRequirement();
        }

        private void Start()
        {
            OnExpChanged?.Invoke(currentLevel, currentExp, _expToNextLevel);
        }

        private void Update()
        {
            // Saniyede 10 kez etraftaki EXP taşlarını mıknatıs gibi çekmek için tara
            if (Time.time >= _nextScanTime)
            {
                ScanForGems();
                _nextScanTime = Time.time + 0.1f;
            }
        }

        private void ScanForGems()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, magnetRadius, _gemBuffer, collectibleLayer);
            for (int i = 0; i < count; i++)
            {
                Collider col = _gemBuffer[i];
                if (col == null) continue;

                if (col.TryGetComponent<ExperienceGem>(out var gem))
                {
                    gem.AttractTo(transform, this);
                }
            }
        }

        public void AddExp(int amount)
        {
            if (amount <= 0) return;

            currentExp += amount;

            while (currentExp >= _expToNextLevel)
            {
                currentExp -= _expToNextLevel;
                LevelUp();
            }

            OnExpChanged?.Invoke(currentLevel, currentExp, _expToNextLevel);
        }

        private void LevelUp()
        {
            currentLevel++;
            CalculateExpRequirement();
            OnLevelUp?.Invoke(currentLevel);
            Debug.Log($"<color=#38bdf8><b>[LEVEL UP]</b> Tebrikler! Seviye: {currentLevel}</color>");
        }

        private void CalculateExpRequirement()
        {
            _expToNextLevel = Mathf.Round(baseExpToNextLevel * Mathf.Pow(expGrowthMultiplier, currentLevel - 1));
        }

        public void SetMagnetRadius(float newRadius)
        {
            magnetRadius = Mathf.Max(1f, newRadius);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, magnetRadius);
        }
    }
}
