using KillThemAll.Core;
using KillThemAll.Player;
using KillThemAll.Spawner;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KillThemAll.UI
{
    [DisallowMultipleComponent]
    public class GameHUDView : MonoBehaviour
    {
        [Header("Main Menu UI")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private Button startButton;

        [Header("Gameplay HUD")]
        [SerializeField] private GameObject hudContainer;

        [Header("Health UI")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Color fullHealthColor = new Color(0.2f, 0.85f, 0.3f);
        [SerializeField] private Color lowHealthColor = new Color(0.9f, 0.2f, 0.2f);

        [Header("Level & EXP UI")]
        [SerializeField] private Slider expSlider;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text expText;

        [Header("Stats UI")]
        [SerializeField] private TMP_Text killCountText;
        [SerializeField] private TMP_Text timerText;

        [Header("Game Over UI")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TMP_Text finalKillsText;
        [SerializeField] private TMP_Text finalLevelText;
        [SerializeField] private TMP_Text finalTimeText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        [Header("References")]
        [SerializeField] private HealthComponent playerHealth;
        [SerializeField] private PlayerLevelSystem playerLevel;
        [SerializeField] private EnemySpawnManager spawnManager;

        private float _survivalTime;
        private bool _isPlaying;

        private void Start()
        {
            FindReferencesIfNull();
            BindEvents();

            if (startButton != null) startButton.onClick.AddListener(StartGame);
            if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ShowMainMenu);

            // Başlangıç durumu: Ana Menü aktif, oyun duraklatılmış
            if (mainMenuPanel != null)
            {
                ShowMainMenu();
            }
            else
            {
                StartGame();
            }
        }

        private void FindReferencesIfNull()
        {
            if (playerHealth == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    playerHealth = player.GetComponent<HealthComponent>();
                    playerLevel = player.GetComponent<PlayerLevelSystem>();
                }
            }

            if (spawnManager == null)
            {
                spawnManager = FindAnyObjectByType<EnemySpawnManager>();
            }
        }

        private void BindEvents()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateHealthBar;
                playerHealth.OnDied += HandlePlayerDeath;
                UpdateHealthBar(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }

            if (playerLevel != null)
            {
                playerLevel.OnExpChanged += UpdateExpBar;
                playerLevel.OnLevelUp += HandleLevelUp;
                UpdateExpBar(playerLevel.CurrentLevel, playerLevel.CurrentExp, playerLevel.ExpToNextLevel);
            }

            if (spawnManager != null)
            {
                spawnManager.OnKillCountChanged += UpdateKillCount;
                UpdateKillCount(spawnManager.TotalKills);
            }
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateHealthBar;
                playerHealth.OnDied -= HandlePlayerDeath;
            }

            if (playerLevel != null)
            {
                playerLevel.OnExpChanged -= UpdateExpBar;
                playerLevel.OnLevelUp -= HandleLevelUp;
            }

            if (spawnManager != null)
            {
                spawnManager.OnKillCountChanged -= UpdateKillCount;
            }

            if (startButton != null) startButton.onClick.RemoveListener(StartGame);
            if (restartButton != null) restartButton.onClick.RemoveListener(RestartGame);
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(ShowMainMenu);
        }

        private void Update()
        {
            if (!_isPlaying) return;

            _survivalTime += Time.deltaTime;
            int minutes = Mathf.FloorToInt(_survivalTime / 60f);
            int seconds = Mathf.FloorToInt(_survivalTime % 60f);
            if (timerText != null)
            {
                timerText.text = $"{minutes:00}:{seconds:00}";
            }
        }

        public void StartGame()
        {
            _isPlaying = true;
            _survivalTime = 0f;
            Time.timeScale = 1f;

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (hudContainer != null) hudContainer.SetActive(true);
            if (spawnManager != null) spawnManager.enabled = true;
        }

        public void ShowMainMenu()
        {
            _isPlaying = false;
            Time.timeScale = 0f;

            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (hudContainer != null) hudContainer.SetActive(false);
            if (spawnManager != null) spawnManager.enabled = false;
        }

        private void UpdateHealthBar(float current, float max)
        {
            float ratio = Mathf.Clamp01(current / max);

            if (healthSlider != null) healthSlider.value = ratio;
            if (healthText != null) healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            if (healthFillImage != null) healthFillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, ratio);
        }

        private void UpdateExpBar(int level, float currentExp, float maxExp)
        {
            float ratio = maxExp > 0f ? Mathf.Clamp01(currentExp / maxExp) : 0f;

            if (expSlider != null) expSlider.value = ratio;
            if (levelText != null) levelText.text = $"LVL {level}";
            if (expText != null) expText.text = $"{Mathf.CeilToInt(currentExp)} / {Mathf.CeilToInt(maxExp)}";
        }

        private void HandleLevelUp(int newLevel)
        {
            if (levelText != null) levelText.text = $"LVL {newLevel}";
        }

        private void UpdateKillCount(int count)
        {
            if (killCountText != null)
            {
                killCountText.text = $"KILLS: {count}";
            }
        }

        private void HandlePlayerDeath()
        {
            _isPlaying = false;

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            if (finalKillsText != null && spawnManager != null)
            {
                finalKillsText.text = $"Enemies Defeated: {spawnManager.TotalKills}";
            }

            if (finalLevelText != null && playerLevel != null)
            {
                finalLevelText.text = $"Level Reached: {playerLevel.CurrentLevel}";
            }

            if (finalTimeText != null)
            {
                int minutes = Mathf.FloorToInt(_survivalTime / 60f);
                int seconds = Mathf.FloorToInt(_survivalTime % 60f);
                finalTimeText.text = $"Time Survived: {minutes:00}:{seconds:00}";
            }
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }
    }
}
