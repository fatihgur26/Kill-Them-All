using UnityEngine;
using UnityEngine.Pool;

namespace KillThemAll.Collectibles
{
    [DisallowMultipleComponent]
    public class ExperienceGemPool : MonoBehaviour
    {
        public static ExperienceGemPool Instance { get; private set; }

        [Header("Prefab & Pool Settings")]
        [SerializeField] private ExperienceGem gemPrefab;
        [SerializeField] private int defaultCapacity = 40;
        [SerializeField] private int maxPoolSize = 150;

        private IObjectPool<ExperienceGem> _pool;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializePool();
        }

        private void InitializePool()
        {
            _pool = new ObjectPool<ExperienceGem>(
                createFunc: CreateGem,
                actionOnGet: gem => gem.OnSpawnFromPool(),
                actionOnRelease: gem =>
                {
                    gem.OnReturnToPool();
                    gem.gameObject.SetActive(false);
                },
                actionOnDestroy: gem =>
                {
                    if (gem != null) Destroy(gem.gameObject);
                },
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxPoolSize
            );
        }

        private ExperienceGem CreateGem()
        {
            if (gemPrefab == null)
            {
                Debug.LogError("[ExperienceGemPool] gemPrefab atanmadı!");
                return null;
            }

            ExperienceGem gem = Instantiate(gemPrefab, transform);
            gem.SetPool(_pool);
            gem.gameObject.SetActive(false);
            return gem;
        }

        public static void SpawnGem(Vector3 position, int value)
        {
            if (Instance == null || Instance._pool == null) return;

            ExperienceGem gem = Instance._pool.Get();
            if (gem != null)
            {
                gem.Spawn(position, value);
            }
        }

        public void SetGemPrefab(ExperienceGem prefab)
        {
            gemPrefab = prefab;
        }
    }
}
