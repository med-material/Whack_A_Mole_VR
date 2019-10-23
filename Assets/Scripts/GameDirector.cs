using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class TimeUpdateEvent : UnityEvent<float> {}

[System.Serializable]
public class StateUpdateEvent : UnityEvent<GameDirector.GameState> {}

/*
Base class of the game. Launches and stops the game. Contains the different game's parameters.
*/

public class GameDirector : MonoBehaviour
{
    public enum GameState {Paused, Playing, Stopped}

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

    [SerializeField]
    public TimeUpdateEvent timeUpdate;

    [SerializeField]
    public StateUpdateEvent stateUpdate;

    private Dictionary<string, float> difficultySettings;
    private Coroutine spawnTimer;
    private float currentGameTimeLeft;
    private float currentMoleTimeLeft;
    private GameState gameState = GameState.Stopped;

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

    // Starts the game.
    public void StartGame()
    {
        if (gameState == GameState.Playing) return;
        UpdateState(GameState.Playing);
        wallManager.Enable();
        LoadDifficulty();
        StartMoleTimer(gameWarmUpTime);
        StartCoroutine(WaitEndGame(gameDuration));
    }

    // Stops the game.
    public void StopGame()
    {
        if (gameState == GameState.Stopped) return;
        StopAllCoroutines();
        FinishGame();
    }

    // Pauses/unpauses the game.
    public void PauseUnpauseGame()
    {
        if (gameState == GameState.Stopped) return;

        if(gameState == GameState.Playing)
        {
            UpdateState(GameState.Paused);
        }
        else if(gameState == GameState.Paused)
        {
            UpdateState(GameState.Playing);
        }
    }

    // Sets the game duration.
    public void SetGameDuration(float duration)
    {
        if (gameState == GameState.Playing) return;
        gameDuration = duration;
    }

    // Sets the game difficulty.
    public void SetDifficulty(string difficulty)
    {
        gameDifficulty = difficulty;
        LoadDifficulty();
    }

    // Loads the difficulty.
    private void LoadDifficulty()
    {
        difficulties.TryGetValue(gameDifficulty, out difficultySettings);
    }

    // Updates the state of the game (playing, stopped, paused) and raises an event to notify any listener (UI...).
    private void UpdateState(GameState newState)
    {
        gameState = newState;
        stateUpdate.Invoke(gameState);
    }

    private void FinishGame()
    {
        if (gameState == GameState.Stopped) return;
        UpdateState(GameState.Stopped);
        wallManager.Disable();
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
        currentMoleTimeLeft = duration;
        while (currentMoleTimeLeft > 0)
        {
            if (gameState == GameState.Playing)
            {
                currentMoleTimeLeft -= Time.deltaTime;
            }
            yield return null;
        }
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
        currentGameTimeLeft = duration;
        while (currentGameTimeLeft > 0)
        {
            if (gameState == GameState.Playing)
            {
                currentGameTimeLeft -= Time.deltaTime;
                timeUpdate.Invoke(currentGameTimeLeft);
            }
            yield return null;
        }
        timeUpdate.Invoke(0f);
        OnGameEndTimeout();
    }

    private void OnGameEndTimeout()
    {
        FinishGame();
    }
}
