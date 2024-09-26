using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static ZpTools.UtilityFunctions;
using Random = UnityEngine.Random;


public class SpawnManager : MonoBehaviour, INeedButton
{
    private bool _destroying;
    [SerializeField] private bool allowDebug;

    public UnityEvent onSpawn, onSpawningComplete, onFinalSpawnDefeated;

    public SpawnerData spawnerData;
    public bool usePriority, randomizeSpawnRate;

    public float spawnDelay = 1.0f, spawnRateMin = 1.0f, spawnRateMax = 1.0f;
    public int numToSpawn = 10;
    [HideInInspector] public int waitingCount;

    private int _poolSize;

    private int poolSize
    {
        get
        {
            var totalPoolSize = 0;
            foreach (var spawner in spawnerData.spawners)
            {
                totalPoolSize += spawnerData.GetSpawnerActiveLimit(spawner);
            }

            return totalPoolSize;
        }
    }

    private List<GameObject> _pooledObjects;
    private readonly List<WaitForSeconds> _spawnRates = new();
    private float spawnRate
    {
        get
        {
            var value = Random.Range(spawnRateMin, spawnRateMax);
            return value;
        }
    }

    private WaitForSeconds _waitForSpawnRate, _waitForSpawnDelay, _waitLoadBuffer;
    private WaitForFixedUpdate _wffu;
    private Coroutine _lateStartRoutine, _delaySpawnRoutine,_spawnRoutine,_poolCreationRoutine, _spawnWaitingRoutine;

    private PrefabDataList _prefabSet;
    private GameObject _parentObject;

    private int spawnedCount
    {
        get => spawnerData.activeCount.value;
        set => spawnerData.activeCount.Set(value);
    }

    private void Awake()
    {
        spawnerData.ResetSpawnerData();
        _parentObject = new GameObject($"SpawnedObjects_{name}");
        
        _wffu = new WaitForFixedUpdate();
        _waitLoadBuffer = new WaitForSeconds(1.0f);

        _poolSize = poolSize;
        spawnDelay = ToleranceCheck(spawnDelay, spawnDelay);

        _waitForSpawnDelay = new WaitForSeconds(spawnDelay);

        SetSpawnRate();

        if (!spawnerData)
        {
            Debug.LogError("SpawnerData not found in " + name);
            return;
        }
        
        _prefabSet = spawnerData.prefabList;

        _poolCreationRoutine ??= StartCoroutine(DelayPoolCreation());
    }

    private IEnumerator DelayPoolCreation()
    {
        yield return _waitLoadBuffer;
        _parentObject.transform.SetParent(transform);
        yield return _wffu;
        ProcessPool();
        yield return _wffu;
        _poolCreationRoutine = null;
    }

    private void ProcessPool()
    {
        _pooledObjects ??= new List<GameObject>();
        int iterationCount = _poolSize - _pooledObjects.Count;
        if (iterationCount <= 0) return;
        
        int totalPriority = _prefabSet.GetPriority();

        for (int i = 0; i < iterationCount; i++)
        {
            int randomNumber = Random.Range(0, totalPriority);
            int sum = 0;
            foreach (var _ in _prefabSet.prefabDataList)
            {
                var objData = _prefabSet.GetRandomPrefabData();
                sum += objData.priority;
                if (randomNumber >= sum && usePriority) continue;
                var obj = Instantiate(objData.prefab);
                AddToPool(obj);
                break;
            }
        }
    }

    private void AddToPool(GameObject obj)
    {
        var spawnBehavior = obj.GetComponent<PooledObjectBehavior>();
        if (spawnBehavior == null) obj.AddComponent<PooledObjectBehavior>();

        _pooledObjects.Add(obj);
        obj.transform.SetParent(_parentObject.transform);
        obj.SetActive(false);
    }

    public void SetSpawnDelay(float newDelay)
    {
        newDelay = ToleranceCheck(spawnDelay, newDelay);
        if (newDelay < 0) return;
        spawnDelay = newDelay;
        _waitForSpawnDelay = new WaitForSeconds(spawnDelay);
    }
    
    private void SetSpawnRate()
    {
        if (!randomizeSpawnRate)
        {
            _waitForSpawnRate = new WaitForSeconds(spawnRate);
            return;
        }
        if (numToSpawn < _spawnRates.Count) return;
        
        var count = numToSpawn - _spawnRates.Count;
        for (var i = 0; i < count; i++)
        {
            _spawnRates.Add(new WaitForSeconds(spawnRate));
        }
    }
    
    public WaitForSeconds GetWaitSpawnRate()
    {
        return randomizeSpawnRate ? _spawnRates[Random.Range(0, _spawnRates.Count)] : _waitForSpawnRate;
    }

    public void StartSpawn(int amount)
    {
        if (_spawnRoutine != null || waitingCount > 0) return;
        numToSpawn = (amount > 0) ? amount : numToSpawn;
        StartSpawn();
    }

    public void StartSpawn()
    {
        if (_spawnRoutine != null) return;
        numToSpawn = numToSpawn > 0 ? numToSpawn : 1;
        if (spawnedCount > 0) spawnedCount = 0;
        _delaySpawnRoutine ??= StartCoroutine(DelaySpawn());
    }
    
    public void StopSpawn()
    {
        if (_spawnRoutine == null) return;
        StopCoroutine(_spawnRoutine);
        _spawnRoutine = null;
    }

    private IEnumerator DelaySpawn()
    {
        SetSpawnRate();
        while(_poolCreationRoutine != null)
        {
            yield return _wffu;
        }
        yield return _wffu;
        yield return _waitForSpawnDelay;
        _spawnRoutine ??= StartCoroutine(Spawn());
        yield return _wffu;
        _delaySpawnRoutine = null;
    }
    
    private IEnumerator Spawn()
    {
        yield return _waitLoadBuffer;
        while (spawnedCount < numToSpawn)
        {
            var waitTime = GetWaitSpawnRate();
            SpawnerData.Spawner spawner = spawnerData.GetInactiveSpawner();
            if (allowDebug)
            {
                Debug.Log($"Spawning Info...\nTotal Spawns Currently Active Count: {spawnedCount}\nTotal To Spawn: {numToSpawn}\nNum Left: {numToSpawn-spawnedCount}\nPoolSize: {_poolSize}\nPooledObjects: {_pooledObjects.Count}\nspawners: {spawnerData.spawners.Count}\nspawnRate: {waitTime}");
                Debug.Log((spawner == null) ? "No Spawners Available" : $"Spawner: {spawner.spawnerID}");
            }
            
            yield return _wffu;
            
            if (spawner == null)
            {
                waitingCount = numToSpawn - spawnedCount;
                spawnedCount = 0;
                if (allowDebug) Debug.Log($"All Spawners Active... Killing Process, {waitingCount} spawns waiting for next spawn cycle.");
                yield break;
            }

            GameObject spawnObj = FetchFromPool();
            _poolSize = _pooledObjects.Count;
            
            if (!spawnObj)
            {
                _poolSize++;
                ProcessPool();
                continue;
            }
            
            var navBehavior = spawner.pathingTarget ? spawnObj.GetComponent<NavAgentBehavior>(): null;
            if (spawner.pathingTarget && navBehavior == null) Debug.LogError($"No NavAgentBehavior found on {spawnObj} though a pathingTarget was found in ProcessSpawnedObject Method");

            Transform objTransform = spawnObj.transform;
            PooledObjectBehavior spawnBehavior = spawnObj.GetComponent<PooledObjectBehavior>();
                    
            if (spawnBehavior == null) Debug.LogError($"No SpawnObjectBehavior found on {spawnObj} in ProcessSpawnedObject Method");
            var rb = spawnObj.GetComponent<Rigidbody>();
            
            spawnBehavior.Setup(this, ref spawner, ref allowDebug);
            
            if (rb)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            if (allowDebug) Debug.Log($"Retrieved Spawner: {spawner.spawnerID} with... Count: {spawner.GetAliveCount()} Limit: {spawnerData.GetSpawnerActiveLimit(spawner)}");
            
            objTransform.position = spawner.spawnLocation.position;
            if (spawner.pathingTarget)
            {
                objTransform.rotation = Quaternion.LookRotation(spawner.pathingTarget.position - spawner.spawnLocation.position);
            }
            
            if (navBehavior) navBehavior.destination = spawner.pathingTarget.position;
            spawnObj.SetActive(true);
            if (navBehavior) navBehavior.Setup(spawner.pathingTarget.position);
            onSpawn.Invoke();
            spawner.IncrementCount();
            spawnedCount++;
            yield return waitTime;
        }
        onSpawningComplete.Invoke();
        _spawnRoutine = null;
    }

    private GameObject FetchFromPool() { return FetchFromList(_pooledObjects, obj => !obj.activeInHierarchy); }
    
    private IEnumerator ProcessWaitingSpawns()
    {
        yield return _wffu;
        if (waitingCount <= 0) yield break;
        StartSpawn(waitingCount);
        waitingCount = 0;
        _spawnWaitingRoutine = null;
    }
    
    public void NotifyPoolObjectDisabled(ref SpawnerData.Spawner spawnerID)
    {
        if (_destroying) return;
        spawnerData.HandleSpawnRemoval(ref spawnerID);
        if (allowDebug) Debug.Log($"Notified of Death: passed {spawnerID} as spawnerID\nTotal active: {spawnerData.activeCount}");
        
        if (spawnerData.activeCount <= 0 && numToSpawn - spawnedCount <= 0 && waitingCount <= 0)
        {
            if (allowDebug) Debug.Log($"NOTIFIED: {spawnerID} WAS THE FINAL SPAWN");
            onFinalSpawnDefeated.Invoke();
        }
        else
        {
            if (waitingCount <= 0 || _destroying) return;
            _spawnWaitingRoutine ??= StartCoroutine(ProcessWaitingSpawns());
        }
    }

    private void OnDisable()
    {
        _destroying = true;
    }

    private void OnDestroy()
    {
        _destroying = true;
    }


    public List<(System.Action, string)> GetButtonActions()
    {
        return new List<(System.Action, string)> { (() => StartSpawn(numToSpawn), "Spawn") };
    }

}

namespace ZpTools
{
    public static class UtilityFunctions
    {
        public static float ToleranceCheck(float value, float newValue, float tolerance = 0.1f)
        {
            return System.Math.Abs(value - newValue) < tolerance ? value : newValue;
        }
        
        public static T FetchFromList<T>(List<T> listToProcess, System.Func<T, bool> condition)
        {
            if (listToProcess == null || listToProcess.Count == 0) return default;
            foreach (var obj in listToProcess)
            {
                if (condition(obj)) return obj;
            }
            return default;
        }
    }
}