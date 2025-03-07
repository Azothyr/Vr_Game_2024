using System.Collections.Generic;
using UnityEngine;
using ZPTools.Interface;

namespace ShipGame.ScriptObj
{
    [CreateAssetMenu (fileName = "CoreData", menuName = "Data/ManagerData/CoreData")]
    public class CoreData : ScriptableObject, IResetOnNewGame
    {
        [SerializeField] internal bool allowDebug;
        [SerializeField] private GameAction _playerInitializePositionAction;
        [SerializeField] private GameGlobals gameGlobals;
        [SerializeField] private LevelData levelData;
        [SerializeField] private ShipData ship;
        [SerializeField] private CannonData cannon;
        [SerializeField] private AmmoData ammo;
        [SerializeField] private EnemyData enemy;
        [SerializeField] private BossData boss;

        public GameAction playerInitializePositionAction
        {
            get => _playerInitializePositionAction;
            set => _playerInitializePositionAction = value;
        }

        private int currentLevel
        {
            get => levelData.currentLevel;
            set 
            {
                levelData.currentLevel = value;
                SetLevelData();
            }
        }
        
        public InstancerData shipInstancerData => ship.shipInstancerData;

        public int shipIndex
        {
            get => ship.selectionIndex;
            set
            {
                ship.selectionIndex = value;
                SetShipData();
            }
        }
        
        // Current cannon prefab offset based on cannon prefab and ship prefab if ordered correctly in cannon selection's offset array
        private Vector3Data cannonPrefabOffset => cannon.GetCannonOffset(shipIndex);
        public int cannonIndex
        {
            get => cannon.selectionIndex;
            set
            {
                cannon.selectionIndex = value;
                SetCannonData();
            }
        }
        
        public int ammoIndex
        {
            get => ammo.selectionIndex;
            set 
            {
                ammo.selectionIndex = value;
                SetAmmoData();
            }
        }

        public int enemyIndex
        {
            get => gameGlobals.FightingBoss() ? boss.selectionIndex : enemy.selectionIndex;
            set 
            {
                if (gameGlobals.FightingBoss())
                {
                    boss.selectionIndex = value;
                }
                else
                {
                    enemy.selectionIndex = value;
                }
                SetEnemyData(gameGlobals.FightingBoss());
            }
        }

        private void OnValidate()
        {
            if (!gameGlobals) Debug.LogError("[ERROR] Game Globals is null. Please assign a value.", this);
            if (!levelData) Debug.LogError("[ERROR] Level Data is null. Please assign a value.", this);
            if (!ship) Debug.LogError("[ERROR] Ship Selections is null. Please assign a value.", this);
            if (!cannon) Debug.LogError("[ERROR] Cannon Selections is null. Please assign a value.", this);
            if (!ammo) Debug.LogError("[ERROR] Ammo Selections is null. Please assign a value.", this);
            if (!enemy) Debug.LogError("[ERROR] Enemy Selections is null. Please assign a value.", this);
        }

        private void OnEnable()
        {
            levelData.LoadError += HandleLevelLoadError;
            ship.LoadError += HandleShipLoadError;
            cannon.LoadError += HandleCannonLoadError;
            ammo.LoadError += HandleAmmoLoadError;
            enemy.LoadError += HandleEnemyLoadError;
            boss.LoadError += HandleEnemyLoadError;
        }

        private void OnDisable()
        {
            levelData.LoadError -= HandleLevelLoadError;
            ship.LoadError -= HandleShipLoadError;
            cannon.LoadError -= HandleCannonLoadError;
            ammo.LoadError -= HandleAmmoLoadError;
            enemy.LoadError -= HandleEnemyLoadError;
            boss.LoadError -= HandleEnemyLoadError;
        }
        
        public void LevelCompleted()
        {
            currentLevel++;
            // SetEnemyData();
            
            PrintGameVariables("Called From LevelCompleted");
        }
        
        [HideInInspector] public bool failedLevel;
        public void LevelFailed() 
        {
            failedLevel = true;
            ResetToNewGameValues();
        }

        public void ResetToNewGameValues(int tier = 1)
        {
            if (tier < 1) return;
            currentLevel = 1;
            
            ship.selectionIndex = 0;
            
            cannon.selectionIndex = 0;
            cannon.upgradeIndex = 0;
            
            ammo.selectionIndex = 0;
            ammo.upgradeIndex = 0;
            
            boss.selectionIndex = 0;
            enemy.selectionIndex = 0;
            
            gameGlobals.ResetGameVariables();
            
        }
        
        private void SetPlayerData()
        {
            gameGlobals.SetPlayerSpeed();
        }
        
        private void SetShipData()
        {
            try
            {
                UpdatePlayerHealth();
            } 
            catch (System.IndexOutOfRangeException)
            {
                LoadShipData();
                UpdatePlayerHealth();
            }
            
            try
            {
                gameGlobals.SetEnemySpawnCount(levelData.spawnCount, ship.numberOfLanes);
            } 
            catch (System.IndexOutOfRangeException)
            {
                LoadLevelData();
                gameGlobals.SetEnemySpawnCount(levelData.spawnCount, ship.numberOfLanes);
            }
            
            ship.SetCannonPrefabData(cannon.prefab);
            ship.SetAmmoSpawnCount();
        }
        
        public void UpdatePlayerHealth()
        {
            float shipHealth = RetrieveWithRetry(
                getter: () => ship.health, 
                loader: LoadShipData, 
                maxAttempts: 25, 
                defaultValue: 0f
            );

            gameGlobals.SetShipHealth(shipHealth, failedLevel);
        }

        
        private void SetLevelData()
        {
            gameGlobals.SetSpawnLaneActiveLimit(levelData.laneActiveLimit);
            gameGlobals.SetEnemySpawnCount(levelData.spawnCount, ship.numberOfLanes);
            gameGlobals.SetSpawnRates(levelData.spawnRateMin, levelData.spawnRateMax);
            
            enemy.SetHealth(levelData.spawnBaseHealth + enemy.selectionHealth);
            enemy.SetDamage(levelData.spawnBaseDamage + enemy.selectionDamage);
            enemy.SetBounty(levelData.spawnBounty + enemy.selectionBounty);
            enemy.SetScore(levelData.spawnScore + enemy.selectionScore);
        }
        
        private void SetAmmoData()
        {
            UpdatePlayerDamage();
            gameGlobals.SetAmmoRespawnRate(ammo.respawnRate);
            
            // Pass the prefab list to the ship's ammo spawner
            ship.SetAmmoPrefabDataList(ammo.prefabList);
        }
        
        private void SetCannonData()
        {
            UpdatePlayerDamage();
                
            // Pass the prefab to the ship's cannon instancer with its correct offset
            ship.SetCannonPrefabData(cannon.prefab);
            ship.SetCannonPrefabOffset(cannonPrefabOffset);
        }
        
        public void UpdatePlayerDamage()
        {
            float cannonDamage = RetrieveWithRetry(
                getter: () => cannon.damage, 
                loader: LoadCannonData, 
                maxAttempts: 25, 
                defaultValue: 0f
            );

            float ammoDamage = RetrieveWithRetry(
                getter: () => ammo.damage, 
                loader: LoadAmmoData, 
                maxAttempts: 25, 
                defaultValue: 0f
            );

            gameGlobals.SetPlayerDamage(cannonDamage, ammoDamage);
        }

        
        private void SetEnemyData(bool isBoss)
        {
            if (isBoss)
            {
                // Pass the prefab list to the ship's enemy spawner
                ship.SetEnemyPrefabDataList(boss.prefabList);

                boss.SetHealth(levelData.spawnBaseHealth + boss.selectionHealth);
                boss.SetDamage(levelData.spawnBaseDamage + boss.selectionDamage);
                boss.SetSpeed(levelData.spawnBaseDamage + boss.selectionSpeed);
                boss.SetBounty(levelData.spawnBounty + boss.selectionBounty);
                boss.SetScore(levelData.spawnScore + boss.selectionScore);
            }
            else
            {
                // Pass the prefab list to the ship's enemy spawner
                ship.SetEnemyPrefabDataList(enemy.prefabList);

                enemy.SetHealth(levelData.spawnBaseHealth + enemy.selectionHealth);
                enemy.SetDamage(levelData.spawnBaseDamage + enemy.selectionDamage);
                enemy.SetSpeed(enemy.selectionSpeed);
                enemy.SetBounty(levelData.spawnBounty + enemy.selectionBounty);
                enemy.SetScore(levelData.spawnScore + enemy.selectionScore);
            }
        }

        public void HandleEnemyDefeated()
        {
            if (!gameGlobals)
            {
                Debug.LogError("[ERROR] GameGlobals is null. Please assign a value.", this);
                return;
            }
            var enemyIsBoss = gameGlobals.FightingBoss();
            
            gameGlobals.playerCoins += enemyIsBoss ? boss.bounty: enemy.bounty;
            gameGlobals.playerScore += enemyIsBoss ? boss.score: enemy.score;
            
            gameGlobals.UpdateCoinVisual();
            gameGlobals.UpdateEnemyCountVisual();
        }
        
        private T RetrieveWithRetry<T>(System.Func<T> getter, System.Action loader, int maxAttempts = 10, T defaultValue = default)
        {
            int attempt = 0;

            while (attempt < maxAttempts)
            {
                try
                {
                    var value = getter();
                    if (!EqualityComparer<T>.Default.Equals(value, defaultValue)) // Check if value is valid
                    {
                        // Debug.Log($"[DEBUG] Successfully retrieved value: {value} after {attempt + 1} attempts.");
                        return value;
                    }
                }
                catch (System.IndexOutOfRangeException)
                {
                    // Debug.LogWarning($"[WARNING] Attempt {attempt + 1} failed to retrieve value. Reloading data and retrying.");
                    loader?.Invoke(); // Attempt to load the data
                }

                attempt++;
            }

            // Debug.LogWarning($"[WARNING] Unable to retrieve value after {maxAttempts} attempts.");
            return defaultValue;
        }

        private void HandleLevelLoadError()
        {
            if (allowDebug) 
                Debug.LogError("[WARNING] Level data is not loaded. Attempting load.", this);
            LoadLevelData();
        }
        private void LoadLevelData() => levelData.LoadOnStartup();

        private void HandleShipLoadError()
        {
            if (allowDebug) 
                Debug.LogError("[WARNING] Ship data is not loaded. Attempting load.", this);
            LoadShipData();
        }
        private void LoadShipData() => ship.LoadOnStartup();

        private void HandleAmmoLoadError()
        {
            if (allowDebug) 
                Debug.LogError("[WARNING] Ammo data is not loaded. Attempting load.", this);
            LoadAmmoData();
        }
        private void LoadAmmoData() => ammo.LoadOnStartup();

        private void HandleCannonLoadError()
        {
            if (allowDebug) 
                Debug.LogError("[WARNING] Cannon data is not loaded. Attempting load.", this);
            LoadCannonData();
        }
        private void LoadCannonData() => cannon.LoadOnStartup();
        
        private void HandleEnemyLoadError()
        {
            if (allowDebug) 
                Debug.LogError("[WARNING] Enemy data is not loaded. Attempting load.", this);
            LoadEnemyData();
        }
        private void LoadEnemyData()
        {
            if(gameGlobals.FightingBoss())
            {
                boss.LoadOnStartup();
                return;
            }
            enemy.LoadOnStartup();
        }
        private void LoadAllData()
        {
            LoadLevelData();
            LoadShipData();
            LoadAmmoData();
            LoadCannonData();
            LoadEnemyData();
        }

        private void Setup(bool attemptingReload)
        {
            if (attemptingReload)
            {
                LoadAllData();
                return;
            }
            SetPlayerData();
            SetShipData();
            SetLevelData();
            SetAmmoData();
            SetCannonData();
            SetEnemyData(gameGlobals.FightingBoss());
        }
        
        public void SetupOnlyPlayerData()
        {
            SetPlayerData();
            SetShipData();
            SetAmmoData();
            SetCannonData();
        }

        [HideInInspector] public bool setupComplete;
        private bool _isSettingUp;
        public void Setup()
        {
            if (_isSettingUp) return;
            _isSettingUp = true;
            setupComplete = false;
            try
            {
                Setup(false);
            }
            
            catch (System.IndexOutOfRangeException e)
            {
                if (allowDebug) 
                    Debug.LogWarning(
                    "[WARNING] Attempted to load game data and got an index out of range exception. Attempting to initialize data and reload. Error: " +
                    e.Message);
                
                Setup(true);
            }
            
            catch (System.NullReferenceException e)
            {
                if (allowDebug) 
                    Debug.LogWarning(
                    "[WARNING] Attempted to load game data and got a null reference exception. Attempting to initialize data and reload. Error: " +
                    e.Message);
                
                Setup(true);
            }
            
            catch (System.Exception e)
            {
                if (allowDebug) 
                    Debug.LogWarning(
                    "[WARNING] Attempted to load game data and got an unexpected exception type. Attempting to initialize data and reload. Error: " +
                    e.Message);
                
                Setup(true);
            }
            if (allowDebug) 
                Debug.Log("[DEBUG] Successfully setup game data.", this);
            PrintGameVariables("Called From Setup");
            
            setupComplete = true;
            _isSettingUp = false;
        }

        private void PrintGameVariables(string header = "")
        {
            if (allowDebug)
            {
                var fightingBoss = gameGlobals.FightingBoss();
                
                Debug.Log(
                    "[DEBUG] -----Game Variables-----\n" +
                    $"{(header == "" ? "" : $" ---{header}--- \n")}" +
                    $"Boss level: {(fightingBoss ? "True" : "False")}\n" +
                    $"\n___ Level Index [{currentLevel}] ___\n" +
                    $"Player Speed: {gameGlobals.playerSpeed}\n" +
                    $"Player Score: {gameGlobals.playerScore}\n" +
                    $"\n___ Ship Index [{shipIndex}] ___\n" +
                    $"Ship Health: {gameGlobals.shipHealth}\n" +
                    $"\n___ Ammo Index: [{ammoIndex}] ___\n" +
                    $"Ammo Damage: {gameGlobals.playerDamage}\n" +
                    $"Ammo Respawn Time: {gameGlobals.ammoRespawnRate}\n" +
                    $"\n___ Cannon Index: [{cannonIndex}] ___\n" +
                    $"\n___ {(fightingBoss ? "Boss" : "Enemy")} Index: [{enemyIndex}] ___\n" +
                    $"Lane Active Limit: {gameGlobals.enemyLaneActiveLimit}\n" +
                    $"Spawn Rate MIN: {gameGlobals.spawnRateMin}\n" +
                    $"Spawn Rate MAX: {gameGlobals.spawnRateMax}\n" +
                    $"Enemy Spawn Count: {gameGlobals.enemySpawnCount}\n" +
                    $"Enemy Health: {enemy.health}\n" +
                    $"Enemy Damage: {enemy.damage}\n" +
                    $"Enemy Speed: {enemy.speed}\n" +
                    $"Enemy Bounty: {enemy.bounty}\n" +
                    $"Enemy Score: {enemy.score}\n" +
                    "\n"
                    , this
                );
            }
        }
    }
}