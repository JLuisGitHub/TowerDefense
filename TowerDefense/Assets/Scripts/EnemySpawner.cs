using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using static UnityEngine.EventSystems.EventTrigger;

public class EnemySpawner : MonoBehaviour
{
    public static event EventHandler WinGame;

    [SerializeField] public GameObject basement;
    [SerializeField] public GameObject basement2;
    [SerializeField] public List <Path> paths;

    public WaveConfigs WaveConfigs;

    private GameObject enemyContainer;

    private int currentWaveIndex = 0;
    private int spawnedEnemyCount = 0;
    private int deadEnemyCount = 0;
    private int enemyWinInWaveCount = 0;

    private float waveStartTime = 0f;
    private float lastSpawnTryTime = 0f;
    private float nextSpawnDelay = 0f;

    private GameState gameState = GameState.PRE_WAVE;

    private void Start()
    {
        enemyContainer = GameObject.Find("Enemies");
    }

    void OnEnable()
    {
        Enemy.EnemyWin += OnEnemyWin;
        Enemy.EnemyDie += OnEnemyDie;
    }

    void OnDisable()
    {
        Enemy.EnemyWin -= OnEnemyWin;
        Enemy.EnemyDie -= OnEnemyDie;
    }

    void Update()
    {
        PerfomStateAction();
        GameState newState = PerformStateTransition();
        if (!newState.Equals(gameState)) OnStateChange(newState);

        if (Input.GetKeyDown(KeyCode.W))
        {
            WinGame.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnStateChange(GameState newState)
    {
        gameState = newState;
    }

    private void OnEnemyWin(object sender, EventArgs eventArgs)
    {
        enemyWinInWaveCount++;
    }

    private void OnEnemyDie(object sender, int e)
    {
        deadEnemyCount++;
    }
    private WaveConfig currentWave()
    {
        return WaveConfigs.waves[currentWaveIndex];
    }

    private void PerfomStateAction()
    {
        switch (gameState)
        {
            case GameState.PRE_WAVE:
                break;
            case GameState.WAVE_STARTED:
                TrySpawn();
                break;
            case GameState.SPAWN_OVER:
                // Do nothing
                break;
            case GameState.WAVE_OVER:
                StartNewWave();
                break;
            case GameState.GAME_OVER:
                bool enemiesDead = enemyContainer.transform.childCount == 0;
                if (enemiesDead)
                {
                    WinGame.Invoke(this, EventArgs.Empty);
                }
                break;
        }
    }

    private GameState PerformStateTransition()
    {
        switch (gameState)
        {
            case GameState.PRE_WAVE:
                bool preWaveDelayElapsed = currentWave().startDelay  < Time.time - waveStartTime;
                if (preWaveDelayElapsed) return GameState.WAVE_STARTED;
                break;

            case GameState.WAVE_STARTED:
                bool allEnemiesSpawned = spawnedEnemyCount >= currentWave().enemyCount;
                if (allEnemiesSpawned) return GameState.SPAWN_OVER;
                break;

            case GameState.SPAWN_OVER:
                bool enemiesAllGone = deadEnemyCount + enemyWinInWaveCount >= currentWave().enemyCount;
                bool noMoreWave = currentWaveIndex >= WaveConfigs.waves.Count - 1;
                if (enemiesAllGone  && noMoreWave) return GameState.GAME_OVER;
                if (enemiesAllGone  && !noMoreWave) return GameState.WAVE_OVER;
                break;

            case GameState.WAVE_OVER:
                return GameState.PRE_WAVE;

            // Terminal state
            case GameState.GAME_OVER:
                break;
        }

        return gameState;
    }

    private void StartNewWave()
    {
        currentWaveIndex += 1;
        spawnedEnemyCount = 0;
        deadEnemyCount = 0;
        waveStartTime = Time.time;
        lastSpawnTryTime = Time.time;
    }

    private void TrySpawn()
    {
        float spawnVariance = currentWave().spawnVariance;
        float meanSpawnInterval = currentWave().meanSpawnInterval;
        if (Time.time - lastSpawnTryTime  < nextSpawnDelay) return;
        SpawnNewEnemy();

        lastSpawnTryTime = Time.time;
        nextSpawnDelay = 2 * spawnVariance * meanSpawnInterval * UnityEngine.Random.Range(0f, 1f) + (1 - spawnVariance) * meanSpawnInterval;
    }


    private void SpawnNewEnemy()
    {
        Path selectedpath = ChooseRandom <Path>(paths);
        GameObject selectedEnemy = ChooseRandomEnemy();
        selectedEnemy.GetComponent <Enemy>().SetPath(selectedpath.waypoints);

        if (selectedpath.basement_number == 2)
        {
            selectedEnemy.GetComponent <Enemy>().SetBasement(basement2);
        }
        else
        {
            selectedEnemy.GetComponent <Enemy>().SetBasement(basement);
        }

        GameObject newObject = Instantiate(selectedEnemy, selectedpath.waypoints[0].position, Quaternion.identity);
        newObject.transform.parent = enemyContainer.transform;
        spawnedEnemyCount++;
    }

    public static T ChooseRandom <T>(List <T> list)
    {
        int idx = Mathf.FloorToInt(UnityEngine.Random.Range(0, list.Count));
        return list[idx];
    }


    public GameObject ChooseRandomEnemy()
    {
        float totalWeight = currentWave().enemies.Sum(item => item.weight);
        float randomValue = UnityEngine.Random.Range(0f, 1f) * totalWeight;

        float cumulatedWeight = 0f;
        foreach (var enemyConfig in currentWave().enemies)
        {
            cumulatedWeight += enemyConfig.weight;
            if (randomValue  < cumulatedWeight)
            {
                return enemyConfig.enemyPrefab;
            }
        }

        return null;
    }
}

enum GameState
{
    PRE_WAVE,
    WAVE_STARTED,
    SPAWN_OVER,
    WAVE_OVER,
    GAME_OVER
}
