#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using KillThemAll.CameraSystem;
using KillThemAll.Collectibles;
using KillThemAll.Core;
using KillThemAll.Enemies;
using KillThemAll.Enemies.Data;
using KillThemAll.Input;
using KillThemAll.Player;
using KillThemAll.Spawner;
using KillThemAll.UI;
using KillThemAll.Weapons;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KillThemAll.Editor
{
    public static class PrototypeSceneSetup
    {
        private const string RootFolder = "Assets/_Game";
        private const string DataFolder = RootFolder + "/ScriptableObjects";
        private const string PrefabsFolder = RootFolder + "/Prefabs";
        private const string MaterialsFolder = RootFolder + "/Materials";
        private const string ScenesFolder = RootFolder + "/Scenes";

        [MenuItem("Tools/Kill Them All/Setup Complete Prototype Scene", priority = 0)]
        public static void SetupCompletePrototype()
        {
            EnsureDirectories();
            EnsureTag("Enemy");
            EnsureTag("Obstacle");

            // 1. Materyaller
            Material groundMat = CreateOrGetMaterial("M_Ground", new Color(0.18f, 0.20f, 0.22f));
            Material wallMat = CreateOrGetMaterial("M_Wall", new Color(0.25f, 0.28f, 0.32f));
            Material obstacleMat = CreateOrGetMaterial("M_Obstacle", new Color(0.32f, 0.35f, 0.38f));
            Material playerMat = CreateOrGetMaterial("M_Player", new Color(0.15f, 0.65f, 1.0f));
            Material fastEnemyMat = CreateOrGetMaterial("M_Enemy_Fast", new Color(1.0f, 0.65f, 0.1f));
            Material heavyEnemyMat = CreateOrGetMaterial("M_Enemy_Heavy", new Color(0.85f, 0.15f, 0.35f));
            Material projectileMat = CreateOrGetMaterial("M_Projectile", new Color(1.0f, 0.95f, 0.2f));
            Material gemMat = CreateOrGetMaterial("M_Gem", new Color(0.15f, 0.95f, 0.65f));

            // 2. ScriptableObject Verileri (Flyweight)
            EnemyDataSO fastEnemyData = CreateOrGetEnemyData(
                "EnemyData_FastSwarm",
                "Fast Swarm",
                new Color(1.0f, 0.65f, 0.1f),
                hp: 40f,
                speed: 4.8f,
                damage: 8f,
                cooldown: 0.8f,
                attackRange: 1.2f,
                exp: 10
            );

            EnemyDataSO heavyEnemyData = CreateOrGetEnemyData(
                "EnemyData_HeavyBrute",
                "Heavy Brute",
                new Color(0.85f, 0.15f, 0.35f),
                hp: 120f,
                speed: 2.2f,
                damage: 25f,
                cooldown: 1.5f,
                attackRange: 1.6f,
                exp: 35
            );

            // 3. Prefablar
            Projectile projectilePrefab = CreateProjectilePrefab(projectileMat);
            ExperienceGem gemPrefab = CreateGemPrefab(gemMat);
            EnemyController fastEnemyPrefab = CreateEnemyPrefab("Enemy_FastSwarm", fastEnemyMat, fastEnemyData, new Vector3(0.8f, 0.8f, 0.8f));
            EnemyController heavyEnemyPrefab = CreateEnemyPrefab("Enemy_HeavyBrute", heavyEnemyMat, heavyEnemyData, new Vector3(1.3f, 1.3f, 1.3f));

            // 4. Yeni Sahne Oluştur
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Directional Light
            Light sun = Object.FindAnyObjectByType<Light>();
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                sun.color = new Color(1f, 0.96f, 0.88f);
                sun.intensity = 1.2f;
            }

            // 5. Genişletilmiş Kapalı Arena (60x60m)
            GameObject arenaParent = new GameObject("Arena");
            arenaParent.transform.position = Vector3.zero;

            GameObject groundObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundObj.name = "Ground";
            groundObj.transform.SetParent(arenaParent.transform);
            groundObj.transform.position = Vector3.zero;
            groundObj.transform.localScale = new Vector3(6f, 1f, 6f); // 60x60m oyun alanı
            groundObj.GetComponent<Renderer>().sharedMaterial = groundMat;
            groundObj.layer = LayerMask.NameToLayer("Default");

            // Çevre Sınır Duvarları (60x60m)
            GameObject wallsParent = new GameObject("Boundary_Walls");
            wallsParent.transform.SetParent(arenaParent.transform);

            CreateBoundaryWall(wallsParent, wallMat, new Vector3(0f, 2.5f, 30f), new Vector3(61.5f, 5f, 1.5f));  // Kuzey
            CreateBoundaryWall(wallsParent, wallMat, new Vector3(0f, 2.5f, -30f), new Vector3(61.5f, 5f, 1.5f)); // Güney
            CreateBoundaryWall(wallsParent, wallMat, new Vector3(30f, 2.5f, 0f), new Vector3(1.5f, 5f, 61.5f));  // Doğu
            CreateBoundaryWall(wallsParent, wallMat, new Vector3(-30f, 2.5f, 0f), new Vector3(1.5f, 5f, 61.5f)); // Batı

            // Taktiksel İç Engeller (8 Sütun & Bariyer)
            GameObject obstaclesParent = new GameObject("Obstacles");
            obstaclesParent.transform.SetParent(arenaParent.transform);

            CreateObstacle(obstaclesParent, obstacleMat, new Vector3(14f, 2f, 14f), new Vector3(3.5f, 4f, 3.5f));
            CreateObstacle(obstaclesParent, obstacleMat, new Vector3(-14f, 2f, 14f), new Vector3(3.5f, 4f, 3.5f));
            CreateObstacle(obstaclesParent, obstacleMat, new Vector3(14f, 2f, -14f), new Vector3(3.5f, 4f, 3.5f));
            CreateObstacle(obstaclesParent, obstacleMat, new Vector3(-14f, 2f, -14f), new Vector3(3.5f, 4f, 3.5f));

            CreateObstacle(obstaclesParent, obstacleMat, new Vector3(15f, 2f, 0f), new Vector3(3f, 4f, 1.5f));
            CreateObstacle(obstaclesParent, obstacleMat, new Vector3(-15f, 2f, 0f), new Vector3(3f, 4f, 1.5f));
            CreateObstacle(obstaclesParent, obstacleMat, new Vector3(0f, 2f, 15f), new Vector3(1.5f, 4f, 3f));
            CreateObstacle(obstaclesParent, obstacleMat, new Vector3(0f, 2f, -15f), new Vector3(1.5f, 4f, 3f));

            // NavMeshSurface Bake
            NavMeshSurface navSurface = groundObj.AddComponent<NavMeshSurface>();
            navSurface.collectObjects = CollectObjects.All;
            navSurface.BuildNavMesh();

            // 6. EXP Gem Havuz Yöneticisi
            GameObject gemPoolObj = new GameObject("ExperienceGemPool");
            var gemPool = gemPoolObj.AddComponent<ExperienceGemPool>();
            gemPool.SetGemPrefab(gemPrefab);

            // 7. Oyuncu Kurulumu
            GameObject playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObj.name = "Player";
            playerObj.tag = "Player";
            playerObj.transform.position = new Vector3(0f, 1f, 0f);
            playerObj.GetComponent<Renderer>().sharedMaterial = playerMat;

            Object.DestroyImmediate(playerObj.GetComponent<CapsuleCollider>());
            var charController = playerObj.AddComponent<CharacterController>();
            charController.center = Vector3.zero;
            charController.height = 2f;
            charController.radius = 0.5f;

            var playerHealth = playerObj.AddComponent<HealthComponent>();
            playerHealth.Initialize(100f);

            var playerLevel = playerObj.AddComponent<PlayerLevelSystem>();
            playerLevel.SetMagnetRadius(3.8f);

            var mobileInput = playerObj.AddComponent<MobileInputProvider>();
            var standaloneInput = playerObj.AddComponent<StandaloneInputProvider>();
            var playerController = playerObj.AddComponent<PlayerController>();
            playerController.SetInputProviders(mobileInput, standaloneInput);

            var rangeVisualizer = playerObj.AddComponent<PlayerRangeVisualizer>();
            rangeVisualizer.SetRange(8.5f);

            var combatController = playerObj.AddComponent<PlayerCombatController>();
            combatController.SetProjectilePrefab(projectilePrefab);
            combatController.SetEnemyLayer(~0);

            // 8. Kamera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var smoothCam = mainCam.gameObject.AddComponent<SmoothFollowCamera>();
                smoothCam.SetTarget(playerObj.transform);
                smoothCam.SnapToTarget();
            }

            // 9. Spawner Kurulumu
            GameObject spawnerObj = new GameObject("EnemySpawnManager");
            var spawner = spawnerObj.AddComponent<EnemySpawnManager>();
            spawner.SetPlayer(playerObj.transform);

            var so = new SerializedObject(spawner);
            var configsProp = so.FindProperty("enemyConfigs");
            configsProp.ClearArray();

            configsProp.InsertArrayElementAtIndex(0);
            var e0 = configsProp.GetArrayElementAtIndex(0);
            e0.FindPropertyRelative("configName").stringValue = "Fast Swarm";
            e0.FindPropertyRelative("prefab").objectReferenceValue = fastEnemyPrefab;
            e0.FindPropertyRelative("data").objectReferenceValue = fastEnemyData;
            e0.FindPropertyRelative("spawnWeight").intValue = 65;

            configsProp.InsertArrayElementAtIndex(1);
            var e1 = configsProp.GetArrayElementAtIndex(1);
            e1.FindPropertyRelative("configName").stringValue = "Heavy Brute";
            e1.FindPropertyRelative("prefab").objectReferenceValue = heavyEnemyPrefab;
            e1.FindPropertyRelative("data").objectReferenceValue = heavyEnemyData;
            e1.FindPropertyRelative("spawnWeight").intValue = 35;

            so.FindProperty("useArenaBounds").boolValue = true;
            so.FindProperty("arenaHalfSize").floatValue = 27f;
            so.FindProperty("minSpawnDistance").floatValue = 7f;
            so.FindProperty("maxSpawnDistance").floatValue = 25f;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(spawner);

            // 10. UI & Responsive Canvas (Main Menu, Level Bar, HUD, Game Over)
            SetupUI(playerHealth, playerLevel, spawner, mobileInput);

            // Sahneyi Kaydet
            string scenePath = ScenesFolder + "/PrototypeScene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00ff88><b>[KillThemAll]</b> Genişletilmiş arena, Level/EXP sistemi, Ana Menü ve Game Over ile kuruldu: {scenePath}</color>");
        }

        private static void CreateObstacle(GameObject parent, Material mat, Vector3 pos, Vector3 scale)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Obstacle_Pillar";
            cube.transform.SetParent(parent.transform);
            cube.transform.position = pos;
            cube.transform.localScale = scale;
            cube.tag = "Obstacle";
            cube.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void CreateBoundaryWall(GameObject parent, Material mat, Vector3 pos, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "BoundaryWall";
            wall.transform.SetParent(parent.transform);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.tag = "Obstacle";
            wall.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void SetupUI(HealthComponent playerHealth, PlayerLevelSystem playerLevel, EnemySpawnManager spawner, MobileInputProvider mobileInput)
        {
            // EventSystem
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            // Canvas
            GameObject canvasObj = new GameObject("UI_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Safe Area
            GameObject safeAreaObj = new GameObject("SafeArea", typeof(RectTransform));
            safeAreaObj.transform.SetParent(canvasObj.transform, false);
            var safeAreaRect = safeAreaObj.GetComponent<RectTransform>();
            safeAreaRect.anchorMin = Vector2.zero;
            safeAreaRect.anchorMax = Vector2.one;
            safeAreaRect.sizeDelta = Vector2.zero;
            safeAreaObj.AddComponent<ResponsiveSafeArea>();

            GameHUDView hudView = canvasObj.AddComponent<GameHUDView>();

            // ==========================================
            // 1. GAMEPLAY HUD CONTAINER
            // ==========================================
            GameObject hudContainer = new GameObject("HUD_Container", typeof(RectTransform));
            hudContainer.transform.SetParent(safeAreaObj.transform, false);
            var hudContainerRect = hudContainer.GetComponent<RectTransform>();
            hudContainerRect.anchorMin = Vector2.zero;
            hudContainerRect.anchorMax = Vector2.one;
            hudContainerRect.sizeDelta = Vector2.zero;

            // A) TOP LEVEL BAR (Vampire Survivors tarzı üst bant)
            GameObject levelBarObj = new GameObject("LevelBar_Slider", typeof(RectTransform));
            levelBarObj.transform.SetParent(hudContainer.transform, false);
            var levelBarRect = levelBarObj.GetComponent<RectTransform>();
            levelBarRect.anchorMin = new Vector2(0.05f, 1f);
            levelBarRect.anchorMax = new Vector2(0.95f, 1f);
            levelBarRect.pivot = new Vector2(0.5f, 1f);
            levelBarRect.anchoredPosition = new Vector2(0f, -12f);
            levelBarRect.sizeDelta = new Vector2(0f, 22f);

            Image levelBg = levelBarObj.AddComponent<Image>();
            levelBg.color = new Color(0.1f, 0.12f, 0.16f, 0.85f);

            GameObject levelFillArea = new GameObject("FillArea", typeof(RectTransform));
            levelFillArea.transform.SetParent(levelBarObj.transform, false);
            var levelFillAreaRect = levelFillArea.GetComponent<RectTransform>();
            levelFillAreaRect.anchorMin = Vector2.zero;
            levelFillAreaRect.anchorMax = Vector2.one;
            levelFillAreaRect.sizeDelta = new Vector2(-4f, -4f);

            GameObject levelFillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            levelFillObj.transform.SetParent(levelFillArea.transform, false);
            var levelFillRect = levelFillObj.GetComponent<RectTransform>();
            levelFillRect.anchorMin = Vector2.zero;
            levelFillRect.anchorMax = Vector2.one;
            levelFillRect.sizeDelta = Vector2.zero;
            Image levelFillImg = levelFillObj.GetComponent<Image>();
            levelFillImg.color = new Color(0.15f, 0.8f, 1f, 0.95f);

            Slider levelSlider = levelBarObj.AddComponent<Slider>();
            levelSlider.fillRect = levelFillRect;
            levelSlider.minValue = 0f;
            levelSlider.maxValue = 1f;
            levelSlider.value = 0f;

            // Level Text (Badge - Sol)
            GameObject lvlBadgeObj = new GameObject("LevelBadgeText", typeof(RectTransform), typeof(TextMeshProUGUI));
            lvlBadgeObj.transform.SetParent(levelBarObj.transform, false);
            var lvlBadgeRect = lvlBadgeObj.GetComponent<RectTransform>();
            lvlBadgeRect.anchorMin = new Vector2(0f, 0.5f);
            lvlBadgeRect.anchorMax = new Vector2(0f, 0.5f);
            lvlBadgeRect.pivot = new Vector2(0f, 0.5f);
            lvlBadgeRect.anchoredPosition = new Vector2(10f, 0f);
            lvlBadgeRect.sizeDelta = new Vector2(120f, 22f);
            var tmpLevel = lvlBadgeObj.GetComponent<TextMeshProUGUI>();
            tmpLevel.text = "LVL 1";
            tmpLevel.fontSize = 18;
            tmpLevel.fontStyle = FontStyles.Bold;
            tmpLevel.color = new Color(1f, 0.88f, 0.2f);

            // Exp Ratio Text (Sağ)
            GameObject expRatioObj = new GameObject("ExpRatioText", typeof(RectTransform), typeof(TextMeshProUGUI));
            expRatioObj.transform.SetParent(levelBarObj.transform, false);
            var expRatioRect = expRatioObj.GetComponent<RectTransform>();
            expRatioRect.anchorMin = new Vector2(1f, 0.5f);
            expRatioRect.anchorMax = new Vector2(1f, 0.5f);
            expRatioRect.pivot = new Vector2(1f, 0.5f);
            expRatioRect.anchoredPosition = new Vector2(-10f, 0f);
            expRatioRect.sizeDelta = new Vector2(160f, 22f);
            var tmpExpRatio = expRatioObj.GetComponent<TextMeshProUGUI>();
            tmpExpRatio.text = "0 / 30";
            tmpExpRatio.alignment = TextAlignmentOptions.Right;
            tmpExpRatio.fontSize = 15;
            tmpExpRatio.color = Color.white;

            // B) HEALTH BAR (Level barın altında)
            GameObject healthBarObj = new GameObject("HealthBar_Slider", typeof(RectTransform));
            healthBarObj.transform.SetParent(hudContainer.transform, false);
            var healthBarRect = healthBarObj.GetComponent<RectTransform>();
            healthBarRect.anchorMin = new Vector2(0.5f, 1f);
            healthBarRect.anchorMax = new Vector2(0.5f, 1f);
            healthBarRect.pivot = new Vector2(0.5f, 1f);
            healthBarRect.anchoredPosition = new Vector2(0f, -44f);
            healthBarRect.sizeDelta = new Vector2(360f, 28f);

            Image bgImage = healthBarObj.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            GameObject fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(healthBarObj.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = new Vector2(-4f, -4f);

            GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(fillArea.transform, false);
            var fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            Image fillImage = fillObj.GetComponent<Image>();
            fillImage.color = new Color(0.2f, 0.85f, 0.3f);

            Slider healthSlider = healthBarObj.AddComponent<Slider>();
            healthSlider.fillRect = fillRect;
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;

            GameObject healthTextObj = new GameObject("HealthText", typeof(RectTransform), typeof(TextMeshProUGUI));
            healthTextObj.transform.SetParent(healthBarObj.transform, false);
            var healthTextRect = healthTextObj.GetComponent<RectTransform>();
            healthTextRect.anchorMin = Vector2.zero;
            healthTextRect.anchorMax = Vector2.one;
            healthTextRect.sizeDelta = Vector2.zero;
            var tmpHealth = healthTextObj.GetComponent<TextMeshProUGUI>();
            tmpHealth.text = "100 / 100";
            tmpHealth.alignment = TextAlignmentOptions.Center;
            tmpHealth.fontSize = 18;
            tmpHealth.color = Color.white;

            // C) KILLS (Sol Üst)
            GameObject killObj = new GameObject("KillCountText", typeof(RectTransform), typeof(TextMeshProUGUI));
            killObj.transform.SetParent(hudContainer.transform, false);
            var killRect = killObj.GetComponent<RectTransform>();
            killRect.anchorMin = new Vector2(0f, 1f);
            killRect.anchorMax = new Vector2(0f, 1f);
            killRect.pivot = new Vector2(0f, 1f);
            killRect.anchoredPosition = new Vector2(30f, -80f);
            killRect.sizeDelta = new Vector2(200f, 40f);
            var tmpKill = killObj.GetComponent<TextMeshProUGUI>();
            tmpKill.text = "KILLS: 0";
            tmpKill.fontSize = 26;
            tmpKill.fontStyle = FontStyles.Bold;
            tmpKill.color = new Color(1f, 0.85f, 0.2f);

            // D) TIMER (Sağ Üst)
            GameObject timerObj = new GameObject("TimerText", typeof(RectTransform), typeof(TextMeshProUGUI));
            timerObj.transform.SetParent(hudContainer.transform, false);
            var timerRect = timerObj.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(1f, 1f);
            timerRect.anchorMax = new Vector2(1f, 1f);
            timerRect.pivot = new Vector2(1f, 1f);
            timerRect.anchoredPosition = new Vector2(-30f, -80f);
            timerRect.sizeDelta = new Vector2(160f, 40f);
            var tmpTimer = timerObj.GetComponent<TextMeshProUGUI>();
            tmpTimer.text = "00:00";
            tmpTimer.alignment = TextAlignmentOptions.Right;
            tmpTimer.fontSize = 26;
            tmpTimer.fontStyle = FontStyles.Bold;
            tmpTimer.color = Color.white;

            // E) DYNAMIC JOYSTICK ZONE
            GameObject joystickAreaObj = new GameObject("JoystickTouchZone", typeof(RectTransform), typeof(Image));
            joystickAreaObj.transform.SetParent(canvasObj.transform, false);
            var joystickAreaRect = joystickAreaObj.GetComponent<RectTransform>();
            joystickAreaRect.anchorMin = Vector2.zero;
            joystickAreaRect.anchorMax = new Vector2(1f, 0.6f);
            joystickAreaRect.sizeDelta = Vector2.zero;
            Image areaImage = joystickAreaObj.GetComponent<Image>();
            areaImage.color = new Color(0, 0, 0, 0);
            areaImage.raycastTarget = true;

            GameObject joyBgObj = new GameObject("JoystickBackground", typeof(RectTransform), typeof(Image));
            joyBgObj.transform.SetParent(joystickAreaObj.transform, false);
            var joyBgRect = joyBgObj.GetComponent<RectTransform>();
            joyBgRect.sizeDelta = new Vector2(180f, 180f);
            Image joyBgImg = joyBgObj.GetComponent<Image>();
            joyBgImg.color = new Color(1f, 1f, 1f, 0.35f);

            GameObject joyHandleObj = new GameObject("JoystickHandle", typeof(RectTransform), typeof(Image));
            joyHandleObj.transform.SetParent(joyBgObj.transform, false);
            var joyHandleRect = joyHandleObj.GetComponent<RectTransform>();
            joyHandleRect.sizeDelta = new Vector2(80f, 80f);
            Image joyHandleImg = joyHandleObj.GetComponent<Image>();
            joyHandleImg.color = new Color(0.15f, 0.7f, 1f, 0.85f);

            DynamicJoystick dynamicJoy = joystickAreaObj.AddComponent<DynamicJoystick>();
            var joySerialized = new SerializedObject(dynamicJoy);
            joySerialized.FindProperty("background").objectReferenceValue = joyBgRect;
            joySerialized.FindProperty("handle").objectReferenceValue = joyHandleRect;
            joySerialized.FindProperty("hideOnRelease").boolValue = true;
            joySerialized.ApplyModifiedProperties();

            joyBgObj.SetActive(false);
            hudContainer.SetActive(false);

            mobileInput.SetJoystick(dynamicJoy);

            // ==========================================
            // 2. MAIN MENU PANEL
            // ==========================================
            GameObject menuPanelObj = new GameObject("MainMenu_Panel", typeof(RectTransform), typeof(Image));
            menuPanelObj.transform.SetParent(safeAreaObj.transform, false);
            var menuPanelRect = menuPanelObj.GetComponent<RectTransform>();
            menuPanelRect.anchorMin = Vector2.zero;
            menuPanelRect.anchorMax = Vector2.one;
            menuPanelRect.sizeDelta = Vector2.zero;
            menuPanelObj.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.1f, 0.94f);

            // Title
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(menuPanelObj.transform, false);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.65f);
            titleRect.anchorMax = new Vector2(0.5f, 0.65f);
            titleRect.sizeDelta = new Vector2(600f, 80f);
            var tmpTitle = titleObj.GetComponent<TextMeshProUGUI>();
            tmpTitle.text = "KILL THEM ALL";
            tmpTitle.alignment = TextAlignmentOptions.Center;
            tmpTitle.fontSize = 52;
            tmpTitle.fontStyle = FontStyles.Bold;
            tmpTitle.color = new Color(1f, 0.3f, 0.2f);

            // Subtitle
            GameObject subTitleObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            subTitleObj.transform.SetParent(menuPanelObj.transform, false);
            var subTitleRect = subTitleObj.GetComponent<RectTransform>();
            subTitleRect.anchorMin = new Vector2(0.5f, 0.58f);
            subTitleRect.anchorMax = new Vector2(0.5f, 0.58f);
            subTitleRect.sizeDelta = new Vector2(500f, 40f);
            var tmpSubtitle = subTitleObj.GetComponent<TextMeshProUGUI>();
            tmpSubtitle.text = "3D Vampire Survivors Mobile Prototype";
            tmpSubtitle.alignment = TextAlignmentOptions.Center;
            tmpSubtitle.fontSize = 20;
            tmpSubtitle.color = new Color(0.7f, 0.75f, 0.8f);

            // Start Button
            GameObject startBtnObj = new GameObject("StartButton", typeof(RectTransform), typeof(Image), typeof(Button));
            startBtnObj.transform.SetParent(menuPanelObj.transform, false);
            var startBtnRect = startBtnObj.GetComponent<RectTransform>();
            startBtnRect.anchorMin = new Vector2(0.5f, 0.40f);
            startBtnRect.anchorMax = new Vector2(0.5f, 0.40f);
            startBtnRect.sizeDelta = new Vector2(300f, 75f);
            startBtnObj.GetComponent<Image>().color = new Color(0.15f, 0.75f, 0.35f);
            Button startBtn = startBtnObj.GetComponent<Button>();

            GameObject startBtnTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            startBtnTextObj.transform.SetParent(startBtnObj.transform, false);
            var startBtnTextRect = startBtnTextObj.GetComponent<RectTransform>();
            startBtnTextRect.anchorMin = Vector2.zero;
            startBtnTextRect.anchorMax = Vector2.one;
            startBtnTextRect.sizeDelta = Vector2.zero;
            var tmpStartBtn = startBtnTextObj.GetComponent<TextMeshProUGUI>();
            tmpStartBtn.text = "START GAME";
            tmpStartBtn.alignment = TextAlignmentOptions.Center;
            tmpStartBtn.fontSize = 26;
            tmpStartBtn.fontStyle = FontStyles.Bold;
            tmpStartBtn.color = Color.white;

            // ==========================================
            // 3. GAME OVER PANEL
            // ==========================================
            GameObject gameOverObj = new GameObject("GameOver_Panel", typeof(RectTransform), typeof(Image));
            gameOverObj.transform.SetParent(safeAreaObj.transform, false);
            var gameOverRect = gameOverObj.GetComponent<RectTransform>();
            gameOverRect.anchorMin = Vector2.zero;
            gameOverRect.anchorMax = Vector2.one;
            gameOverRect.sizeDelta = Vector2.zero;
            gameOverObj.GetComponent<Image>().color = new Color(0.08f, 0.02f, 0.04f, 0.95f);

            // Game Over Title
            GameObject goTitleObj = new GameObject("GameOverTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            goTitleObj.transform.SetParent(gameOverObj.transform, false);
            var goTitleRect = goTitleObj.GetComponent<RectTransform>();
            goTitleRect.anchorMin = new Vector2(0.5f, 0.75f);
            goTitleRect.anchorMax = new Vector2(0.5f, 0.75f);
            goTitleRect.sizeDelta = new Vector2(500f, 80f);
            var tmpGoTitle = goTitleObj.GetComponent<TextMeshProUGUI>();
            tmpGoTitle.text = "GAME OVER";
            tmpGoTitle.alignment = TextAlignmentOptions.Center;
            tmpGoTitle.fontSize = 56;
            tmpGoTitle.fontStyle = FontStyles.Bold;
            tmpGoTitle.color = new Color(0.95f, 0.15f, 0.25f);

            // Stats: Kills
            GameObject finalKillsObj = new GameObject("FinalKillsText", typeof(RectTransform), typeof(TextMeshProUGUI));
            finalKillsObj.transform.SetParent(gameOverObj.transform, false);
            var finalKillsRect = finalKillsObj.GetComponent<RectTransform>();
            finalKillsRect.anchorMin = new Vector2(0.5f, 0.62f);
            finalKillsRect.anchorMax = new Vector2(0.5f, 0.62f);
            finalKillsRect.sizeDelta = new Vector2(400f, 35f);
            var tmpFinalKills = finalKillsObj.GetComponent<TextMeshProUGUI>();
            tmpFinalKills.text = "Enemies Defeated: 0";
            tmpFinalKills.alignment = TextAlignmentOptions.Center;
            tmpFinalKills.fontSize = 22;
            tmpFinalKills.color = Color.white;

            // Stats: Level
            GameObject finalLevelObj = new GameObject("FinalLevelText", typeof(RectTransform), typeof(TextMeshProUGUI));
            finalLevelObj.transform.SetParent(gameOverObj.transform, false);
            var finalLevelRect = finalLevelObj.GetComponent<RectTransform>();
            finalLevelRect.anchorMin = new Vector2(0.5f, 0.56f);
            finalLevelRect.anchorMax = new Vector2(0.5f, 0.56f);
            finalLevelRect.sizeDelta = new Vector2(400f, 35f);
            var tmpFinalLevel = finalLevelObj.GetComponent<TextMeshProUGUI>();
            tmpFinalLevel.text = "Level Reached: 1";
            tmpFinalLevel.alignment = TextAlignmentOptions.Center;
            tmpFinalLevel.fontSize = 22;
            tmpFinalLevel.color = new Color(0.2f, 0.85f, 1f);

            // Stats: Time
            GameObject finalTimeObj = new GameObject("FinalTimeText", typeof(RectTransform), typeof(TextMeshProUGUI));
            finalTimeObj.transform.SetParent(gameOverObj.transform, false);
            var finalTimeRect = finalTimeObj.GetComponent<RectTransform>();
            finalTimeRect.anchorMin = new Vector2(0.5f, 0.50f);
            finalTimeRect.anchorMax = new Vector2(0.5f, 0.50f);
            finalTimeRect.sizeDelta = new Vector2(400f, 35f);
            var tmpFinalTime = finalTimeObj.GetComponent<TextMeshProUGUI>();
            tmpFinalTime.text = "Time Survived: 00:00";
            tmpFinalTime.alignment = TextAlignmentOptions.Center;
            tmpFinalTime.fontSize = 22;
            tmpFinalTime.color = new Color(1f, 0.85f, 0.2f);

            // Play Again Button
            GameObject restartBtnObj = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
            restartBtnObj.transform.SetParent(gameOverObj.transform, false);
            var restartBtnRect = restartBtnObj.GetComponent<RectTransform>();
            restartBtnRect.anchorMin = new Vector2(0.5f, 0.36f);
            restartBtnRect.anchorMax = new Vector2(0.5f, 0.36f);
            restartBtnRect.sizeDelta = new Vector2(280f, 65f);
            restartBtnObj.GetComponent<Image>().color = new Color(0.9f, 0.25f, 0.2f);
            Button restartBtn = restartBtnObj.GetComponent<Button>();

            GameObject restartBtnTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            restartBtnTextObj.transform.SetParent(restartBtnObj.transform, false);
            var restartBtnTextRect = restartBtnTextObj.GetComponent<RectTransform>();
            restartBtnTextRect.anchorMin = Vector2.zero;
            restartBtnTextRect.anchorMax = Vector2.one;
            restartBtnTextRect.sizeDelta = Vector2.zero;
            var tmpRestartBtn = restartBtnTextObj.GetComponent<TextMeshProUGUI>();
            tmpRestartBtn.text = "PLAY AGAIN";
            tmpRestartBtn.alignment = TextAlignmentOptions.Center;
            tmpRestartBtn.fontSize = 24;
            tmpRestartBtn.fontStyle = FontStyles.Bold;
            tmpRestartBtn.color = Color.white;

            // Main Menu Button
            GameObject menuBtnObj = new GameObject("MainMenuButton", typeof(RectTransform), typeof(Image), typeof(Button));
            menuBtnObj.transform.SetParent(gameOverObj.transform, false);
            var menuBtnRect = menuBtnObj.GetComponent<RectTransform>();
            menuBtnRect.anchorMin = new Vector2(0.5f, 0.26f);
            menuBtnRect.anchorMax = new Vector2(0.5f, 0.26f);
            menuBtnRect.sizeDelta = new Vector2(280f, 55f);
            menuBtnObj.GetComponent<Image>().color = new Color(0.25f, 0.28f, 0.35f);
            Button menuBtn = menuBtnObj.GetComponent<Button>();

            GameObject menuBtnTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            menuBtnTextObj.transform.SetParent(menuBtnObj.transform, false);
            var menuBtnTextRect = menuBtnTextObj.GetComponent<RectTransform>();
            menuBtnTextRect.anchorMin = Vector2.zero;
            menuBtnTextRect.anchorMax = Vector2.one;
            menuBtnTextRect.sizeDelta = Vector2.zero;
            var tmpMenuBtn = menuBtnTextObj.GetComponent<TextMeshProUGUI>();
            tmpMenuBtn.text = "MAIN MENU";
            tmpMenuBtn.alignment = TextAlignmentOptions.Center;
            tmpMenuBtn.fontSize = 20;
            tmpMenuBtn.fontStyle = FontStyles.Bold;
            tmpMenuBtn.color = Color.white;

            // Başlangıçta panelleri gizle
            gameOverObj.SetActive(false);

            // ==========================================
            // SERIALIZE REFERENCES TO HUDVIEW
            // ==========================================
            var hudSo = new SerializedObject(hudView);
            hudSo.FindProperty("mainMenuPanel").objectReferenceValue = menuPanelObj;
            hudSo.FindProperty("startButton").objectReferenceValue = startBtn;
            hudSo.FindProperty("hudContainer").objectReferenceValue = hudContainer;
            hudSo.FindProperty("healthSlider").objectReferenceValue = healthSlider;
            hudSo.FindProperty("healthText").objectReferenceValue = tmpHealth;
            hudSo.FindProperty("healthFillImage").objectReferenceValue = fillImage;
            hudSo.FindProperty("expSlider").objectReferenceValue = levelSlider;
            hudSo.FindProperty("levelText").objectReferenceValue = tmpLevel;
            hudSo.FindProperty("expText").objectReferenceValue = tmpExpRatio;
            hudSo.FindProperty("killCountText").objectReferenceValue = tmpKill;
            hudSo.FindProperty("timerText").objectReferenceValue = tmpTimer;
            hudSo.FindProperty("gameOverPanel").objectReferenceValue = gameOverObj;
            hudSo.FindProperty("finalKillsText").objectReferenceValue = tmpFinalKills;
            hudSo.FindProperty("finalLevelText").objectReferenceValue = tmpFinalLevel;
            hudSo.FindProperty("finalTimeText").objectReferenceValue = tmpFinalTime;
            hudSo.FindProperty("restartButton").objectReferenceValue = restartBtn;
            hudSo.FindProperty("mainMenuButton").objectReferenceValue = menuBtn;
            hudSo.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            hudSo.FindProperty("playerLevel").objectReferenceValue = playerLevel;
            hudSo.FindProperty("spawnManager").objectReferenceValue = spawner;
            hudSo.ApplyModifiedProperties();
        }

        private static ExperienceGem CreateGemPrefab(Material mat)
        {
            string path = PrefabsFolder + "/ExperienceGem.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<ExperienceGem>(path);
            if (existing != null) return existing;

            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = "ExperienceGem";
            obj.transform.localScale = new Vector3(0.35f, 0.45f, 0.35f);
            obj.GetComponent<Renderer>().sharedMaterial = mat;

            var col = obj.GetComponent<SphereCollider>();
            col.isTrigger = true;

            var gem = obj.AddComponent<ExperienceGem>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
            Object.DestroyImmediate(obj);

            return prefab.GetComponent<ExperienceGem>();
        }

        private static Projectile CreateProjectilePrefab(Material mat)
        {
            string path = PrefabsFolder + "/Projectile.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<Projectile>(path);
            if (existing != null) return existing;

            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = "Projectile";
            obj.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            obj.GetComponent<Renderer>().sharedMaterial = mat;

            var col = obj.GetComponent<SphereCollider>();
            col.isTrigger = true;

            var rb = obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var proj = obj.AddComponent<Projectile>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
            Object.DestroyImmediate(obj);

            return prefab.GetComponent<Projectile>();
        }

        private static EnemyController CreateEnemyPrefab(string name, Material mat, EnemyDataSO data, Vector3 scale)
        {
            string path = $"{PrefabsFolder}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<EnemyController>(path);
            if (existing != null) return existing;

            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            obj.name = name;
            obj.tag = "Enemy";
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = mat;

            var col = obj.GetComponent<CapsuleCollider>();
            col.isTrigger = true;

            var agent = obj.AddComponent<NavMeshAgent>();
            agent.speed = data.MoveSpeed;
            agent.stoppingDistance = data.AttackRange * 0.8f;
            agent.acceleration = 16f;
            agent.angularSpeed = 360f;

            var health = obj.AddComponent<HealthComponent>();
            health.Initialize(data.MaxHealth);

            var attack = obj.AddComponent<EnemyAttack>();
            attack.Initialize(data);

            var controller = obj.AddComponent<EnemyController>();

            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("defaultData").objectReferenceValue = data;
            serializedController.FindProperty("modelRenderer").objectReferenceValue = obj.GetComponent<Renderer>();
            serializedController.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
            Object.DestroyImmediate(obj);

            return prefab.GetComponent<EnemyController>();
        }

        private static Material CreateOrGetMaterial(string name, Color color)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static EnemyDataSO CreateOrGetEnemyData(string assetName, string displayName, Color color, float hp, float speed, float damage, float cooldown, float attackRange, int exp)
        {
            string path = $"{DataFolder}/{assetName}.asset";
            EnemyDataSO data = AssetDatabase.LoadAssetAtPath<EnemyDataSO>(path);
            if (data != null)
            {
                var s = new SerializedObject(data);
                s.FindProperty("expDropAmount").intValue = exp;
                s.ApplyModifiedProperties();
                return data;
            }

            data = ScriptableObject.CreateInstance<EnemyDataSO>();
            var serialized = new SerializedObject(data);
            serialized.FindProperty("enemyName").stringValue = displayName;
            serialized.FindProperty("modelColor").colorValue = color;
            serialized.FindProperty("maxHealth").floatValue = hp;
            serialized.FindProperty("moveSpeed").floatValue = speed;
            serialized.FindProperty("damage").floatValue = damage;
            serialized.FindProperty("attackCooldown").floatValue = cooldown;
            serialized.FindProperty("attackRange").floatValue = attackRange;
            serialized.FindProperty("expDropAmount").intValue = exp;
            serialized.ApplyModifiedProperties();

            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        private static void EnsureDirectories()
        {
            string[] dirs = { RootFolder, DataFolder, PrefabsFolder, MaterialsFolder, ScenesFolder };
            foreach (string dir in dirs)
            {
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    string parent = Path.GetDirectoryName(dir).Replace('\\', '/');
                    string leaf = Path.GetFileName(dir);
                    AssetDatabase.CreateFolder(parent, leaf);
                }
            }
        }

        private static void EnsureTag(string tag)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;

            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(tag)) return;
            }

            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
            newTag.stringValue = tag;
            tagManager.ApplyModifiedProperties();
        }
    }
}
#endif
