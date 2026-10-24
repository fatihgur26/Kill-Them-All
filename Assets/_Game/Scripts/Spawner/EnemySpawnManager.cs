using System;
using System.Collections.Generic;
using KillThemAll.Enemies;
using KillThemAll.Enemies.Data;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

namespace KillThemAll.Spawner
{
    [System.Serializable]
    public class EnemySpawnConfig
    {
        public string configName = "Enemy Type";
        public EnemyController prefab;
        public EnemyDataSO data;
        [Range(1, 100)] public int spawnWeight = 50;
    }

    [DisallowMultipleComponent]
    public class EnemySpawnManager : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform playerTransform;

        [Header("Spawn Configuration")]
        [SerializeField] private List<EnemySpawnConfig> enemyConfigs = new List<EnemySpawnConfig>();
        [SerializeField] private float spawnInterval = 1.2f;
        [SerializeField] private int maxSimultaneousEnemies = 70;

        [Header("Arena Bounds & Distance")]
        [SerializeField] private bool useArenaBounds = true;
        [SerializeField] private float arenaHalfSize = 15f; // 30x30m arena için [-15, 15]
        [SerializeField] private float minSpawnDistance = 6f; // Oyuncuya minimum uzaklık
        [SerializeField] private float maxSpawnDistance = 16f;
        [SerializeField] private float navMeshSampleRange = 4f;

        [Header("Pool Limits")]
        [SerializeField] private int defaultPoolCapacity = 30;
        [SerializeField] private int maxPoolSize = 100;

        private readonly Dictionary<EnemyController, IObjectPool<EnemyController>> _poolDictionary 
            = new Dictionary<EnemyController, IObjectPool<EnemyController>>();
        
        private readonly List<EnemyController> _activeEnemies = new List<EnemyController>();
        private float _nextSpawnTime;
        private int _totalKills;

        public int TotalKills => _totalKills;
        public int ActiveEnemyCount => _activeEnemies.Count;

        public event Action<int> OnKillCountChanged;

        private void Awake()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }

            InitializePools();
        }

        public void SetConfigs(List<EnemySpawnConfig> configs)
        {
            enemyConfigs = configs;
            InitializePools();
        }

        public void SetArenaSettings(float halfSize, float minDistance)
        {
            arenaHalfSize = halfSize;
            minSpawnDistance = minDistance;
            useArenaBounds = true;
        }

        private void InitializePools()
        {
            _poolDictionary.Clear();
            if (enemyConfigs == null) return;

            foreach (var config in enemyConfigs)
            {
                if (config == null || config.prefab == null || _poolDictionary.ContainsKey(config.prefab)) continue;

                var prefab = config.prefab;
                var pool = new ObjectPool<EnemyController>(
                    createFunc: () => CreateEnemyInstance(prefab),
                    actionOnGet: enemy =>
                    {
                        enemy.OnSpawnFromPool();
                    },
                    actionOnRelease: enemy =>
                    {
                        enemy.OnReturnToPool();
                        enemy.gameObject.SetActive(false);
                    },
                    actionOnDestroy: enemy =>
                    {
                        if (enemy != null) Destroy(enemy.gameObject);
                    },
                    collectionCheck: true,
                    defaultCapacity: defaultPoolCapacity,
                    maxSize: maxPoolSize
                );

                _poolDictionary.Add(prefab, pool);
            }
        }

        private EnemyController CreateEnemyInstance(EnemyController prefab)
        {
            EnemyController instance = Instantiate(prefab);
            instance.SetPool(_poolDictionary[prefab]);
            instance.gameObject.SetActive(false);
            return instance;
        }

        private void Update()
        {
            if (playerTransform == null) return;

            if (Time.time >= _nextSpawnTime && _activeEnemies.Count < maxSimultaneousEnemies)
            {
                SpawnRandomEnemy();
                _nextSpawnTime = Time.time + spawnInterval;
            }
        }

        private void SpawnRandomEnemy()
        {
            if (enemyConfigs == null || enemyConfigs.Count == 0) return;

            EnemySpawnConfig selectedConfig = SelectRandomConfig();
            if (selectedConfig == null || selectedConfig.prefab == null) return;

            if (TryGetSpawnPosition(out Vector3 spawnPos))
            {
                if (_poolDictionary.TryGetValue(selectedConfig.prefab, out var pool))
                {
                    EnemyController enemy = pool.Get();
                    enemy.Initialize(selectedConfig.data, playerTransform, spawnPos);
                    enemy.OnEnemyDied += HandleEnemyDied;
                    _activeEnemies.Add(enemy);
                }
            }
        }

        private EnemySpawnConfig SelectRandomConfig()
        {
            int totalWeight = 0;
            for (int i = 0; i < enemyConfigs.Count; i++)
            {
                if (enemyConfigs[i] != null) totalWeight += enemyConfigs[i].spawnWeight;
            }

            if (totalWeight <= 0) return enemyConfigs[0];

            int roll = UnityEngine.Random.Range(0, totalWeight);
            int cursor = 0;

            for (int i = 0; i < enemyConfigs.Count; i++)
            {
                if (enemyConfigs[i] == null) continue;
                cursor += enemyConfigs[i].spawnWeight;
                if (roll < cursor)
                {
                    return enemyConfigs[i];
                }
            }

            return enemyConfigs[0];
        }

        private bool TryGetSpawnPosition(out Vector3 result)
        {
            result = Vector3.zero;
            Vector3 center = playerTransform.position;
            float minSqr = minSpawnDistance * minSpawnDistance;

            for (int i = 0; i < 10; i++)
            {
                Vector3 candidate;
                if (useArenaBounds)
                {
                    // Arena içi rastgele nokta seç (duvarların 1.5m içi)
                    float margin = 2f;
                    float rx = UnityEngine.Random.Range(-arenaHalfSize + margin, arenaHalfSize - margin);
                    float rz = UnityEngine.Random.Range(-arenaHalfSize + margin, arenaHalfSize - margin);
                    candidate = new Vector3(rx, 0f, rz);

                    // Oyuncunun çok yakınında ise pas geç
                    if ((candidate - center).sqrMagnitude < minSqr)
                    {
                        continue;
                    }
                }
                else
                {
                    float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    float distance = UnityEngine.Random.Range(minSpawnDistance, maxSpawnDistance);
                    candidate = center + new Vector3(Mathf.Sin(angle) * distance, 0f, Mathf.Cos(angle) * distance);
                }

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleRange, NavMesh.AllAreas))
                {
                    result = hit.position;
                    return true;
                }
            }

            return false;
        }

        private void HandleEnemyDied(EnemyController enemy)
        {
            enemy.OnEnemyDied -= HandleEnemyDied;
            _activeEnemies.Remove(enemy);
            _totalKills++;
            OnKillCountChanged?.Invoke(_totalKills);
        }

        public void SetPlayer(Transform player)
        {
            playerTransform = player;
        }
    }
}
