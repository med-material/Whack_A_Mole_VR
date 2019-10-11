using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Base class of the game. Launches and stops the game. Contains the different game's parameters.
*/

public class GameDirector : MonoBehaviour
{
    [SerializeField]
    private WallManager wallManager;
    
    //temporarily serialized field for game test
    [SerializeField]
    private float gameDuration;
    //temporarily serialized field for game test
    [SerializeField]
    private string gameDifficulty;

    [SerializeField]
    private float gameWarmUpTime = 3f;

    private Dictionary<string, float> difficultySettings;
    private Coroutine spawnTimer;

    private Dictionary<string, Dictionary<string, float>> difficulties = new Dictionary<string, Dictionary<string, float>>(){
        {"easy", new Dictionary<string, float>(){
            {"spawnRate", 3.5f},
            {"spawnVariance", .1f},
            {"lifeTime", 5f},
            {"fakeCoeff", .1f},
        }},
        {"medium", new Dictionary<string, float>(){
            {"spawnRate", 2.25f},
            {"spawnVariance", .3f},
            {"lifeTime", 4f},
            {"fakeCoeff", .2f},
        }},
        {"hard", new Dictionary<string, float>(){
            {"spawnRate", 1f},
            {"spawnVariance", .5f},
            {"lifeTime", 3f},
            {"fakeCoeff", .3f},
        }}
    };

    private bool active = false;

    void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        active = true;
        wallManager.Enable();
        LoadDifficulty();
        StartMoleTimer(gameWarmUpTime);
        StartCoroutine(WaitEndGame(gameDuration));
    }

    public void StopGame()
    {
        StopAllCoroutines();
        FinishGame();
    }

    private void FinishGame()
    {
        active = false;
        wallManager.Disable();
    }

    public void LoadDifficulty()
    {
        difficulties.TryGetValue(gameDifficulty, out difficultySettings);
    }

    private void SpawnMole(float lifeTime, bool fakeCoeff)
    {
        wallManager.ActivateMole(lifeTime, fakeCoeff);
    }

    private void StartMoleTimer(float setTime = -1)
    {
        if (setTime == -1)
        {
            float variance = Random.Range(-difficultySettings["spawnVariance"], difficultySettings["spawnVariance"]);
            spawnTimer = StartCoroutine(WaitSpawnMole(difficultySettings["spawnRate"] + variance));
        }
        else
        {
            spawnTimer = StartCoroutine(WaitSpawnMole(setTime));
        }
    }

    // Waits a given time before activating a new Mole
    private IEnumerator WaitSpawnMole(float duration)
    {
        yield return new WaitForSeconds(duration);
        OnSpawnMoleTimeout();
    }

    private void OnSpawnMoleTimeout()
    {
        SpawnMole(difficultySettings["lifeTime"], Random.Range(0f, 1f) <= difficultySettings["fakeCoeff"]);
        StartMoleTimer();
    }

    // Waits a given time before stopping the game
    private IEnumerator WaitEndGame(float duration)
    {
        yield return new WaitForSeconds(duration);
        OnGameEndTimeout();
    }

    private void OnGameEndTimeout()
    {
        FinishGame();
    }
}
