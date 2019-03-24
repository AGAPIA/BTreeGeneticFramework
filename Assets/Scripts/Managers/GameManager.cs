using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime;


static class RandomExtensions
{
    public static void Shuffle<T>(this System.Random rng, T[] array)
    {
        int n = array.Length;
        while (n > 1)
        {
            int k = rng.Next(n--);
            T temp = array[n];
            array[n] = array[k];
            array[k] = temp;
        }
    }
}

// This contains informations shared between tanks like environment (all tansks, collisions etc), public things in general
public class GlobalAIBlackBox
{
    // Dynamic observations 
    public TankManager[] m_TanksRef;


    // Static observations - that don't change over time
    public Transform[] m_SpawnPoints;
    public float[] m_tempDistancesToSpawnPointsSqr; // Closest distance from any tank to each spawnpoint

    /////////// Helper functions ///////////

    public void SetSpawnPoints(Transform[] positions)
    {
        m_SpawnPoints = positions;
        m_tempDistancesToSpawnPointsSqr = new float[m_SpawnPoints.Length];
    }

    // When dead, we need to get a new respawn pos
    public Transform GetBestSpawnPointForRespawn(GameObject tankRequesting)
    {
        if (m_SpawnPoints == null)
            return null;

        // Find the spawnpoint which is furthest from enemy player
        float bestSpawnPointDistanceSqr     = Mathf.NegativeInfinity;
        int bestSpawnPointIndex             = -1;

        for (int spawnIdx = 0; spawnIdx < m_SpawnPoints.Length; spawnIdx++)
        {
            m_tempDistancesToSpawnPointsSqr[spawnIdx] = Mathf.Infinity;
            Vector3 spawnPos = m_SpawnPoints[spawnIdx].position;

            for (int tankIdx = 0; tankIdx < m_TanksRef.Length; tankIdx++)
            {
                GameObject tankGameObj = m_TanksRef[tankIdx].m_Instance;

                // If tank is not active anymore or it is the same requesting tank
                if (m_TanksRef[tankIdx].IsAlive() == false || tankGameObj == tankRequesting)
                {
                    continue;
                }

                float distTankToSpawnPointSqr = (tankGameObj.transform.position - spawnPos).sqrMagnitude;
                m_tempDistancesToSpawnPointsSqr[spawnIdx] = Mathf.Min(m_tempDistancesToSpawnPointsSqr[spawnIdx], distTankToSpawnPointSqr);  
            }

            if (m_tempDistancesToSpawnPointsSqr[spawnIdx] > bestSpawnPointDistanceSqr)
            {
                bestSpawnPointIndex         = spawnIdx;
                bestSpawnPointDistanceSqr   = m_tempDistancesToSpawnPointsSqr[spawnIdx];
            }
        }

        return (bestSpawnPointIndex != -1 ? m_SpawnPoints[bestSpawnPointIndex] : null);
    }
};

// Informations about a local tank, private, i.e. accessible only to the tank binded to
public class LocalAIBlackBoard
{
    public float deltaTime; // Time since last frame for this AI instance update
}

public class GameManager : MonoBehaviour
{
    public int m_NumRoundsToWin = 5;            // The number of rounds a single player has to win to win the game.
    public float m_StartDelay = 3f;             // The delay between the start of RoundStarting and RoundPlaying phases.
    public float m_EndDelay = 3f;               // The delay between the end of RoundPlaying and RoundEnding phases.
    public CameraControl m_CameraControl;       // Reference to the CameraControl script for control during different phases.
    public Text m_MessageText;                  // Reference to the overlay Text to display winning text, etc.
    public GameObject m_TankPrefab;             // Reference to the prefab the players will control.

    public static float m_SpeedMultiplier = 1.0f;      // Speed multiplier for faster simulation
     
    public TankManager[] m_AiTanks;            // A collection of managers for enabling and disabling different aspects of the tanks.
    public TankManager[] m_HumanTanks;         // A collection of managers for enabling and disabling different aspects of the tanks.

    // First m_HUmanTanks.Length are humans, rest are AIs
    [HideInInspector]
    public TankManager[] m_Tanks;               // Union of the two above since it is easier to manager

    private int m_RoundNumber;                  // Which round the game is currently on.
    private WaitForSeconds m_StartWait;         // Used to have a delay whilst the round starts.
    private WaitForSeconds m_EndWait;           // Used to have a delay whilst the round or game ends.
    private TankManager m_RoundWinner;          // Reference to the winner of the current round.  Used to make an announcement of who won.
    private TankManager m_GameWinner;           // Reference to the winner of the game.  Used to make an announcement of who won.
    Transform[] m_spawnpoints;                 // The spawnpoints authored on the map


    GlobalAIBlackBox m_AIGlobalBlackBox = new GlobalAIBlackBox();

    private void Start()
    {
        GatherSpawnPoints();

        // Create the delays so they only have to be made once.
        m_StartWait = new WaitForSeconds(m_StartDelay);
        m_EndWait = new WaitForSeconds(m_EndDelay);

        SpawnAllTanks(true);
        SetCameraTargets();

        // Once the tanks have been created and the camera is using them as targets, start the game.
        StartCoroutine(GameLoop());
    }

    private void Update()
    {
        GatherGlobalBlackboxData();
    }

    // This is used to gather data in the global blackbox and make everything visible from environment to the AI side
    private void GatherGlobalBlackboxData()
    {
        m_AIGlobalBlackBox.m_TanksRef = m_Tanks;
    }

    private void GatherSpawnPoints()
    {
        GameObject spawnPointsParent = transform.Find("SpawnPoints").gameObject;
        int numSpawnPoints = spawnPointsParent.transform.childCount;

        m_spawnpoints = new Transform[numSpawnPoints];
        for (int i = 0; i < numSpawnPoints; i++)
        {
            m_spawnpoints[i] = spawnPointsParent.transform.GetChild(i);
        }

        m_AIGlobalBlackBox.SetSpawnPoints(m_spawnpoints);
    }

    int m_tempspawnPointIndexIter = 0;
    private void SpawnPointIteration_begin()
    {
        m_tempspawnPointIndexIter = 0;
    }

    private Transform SpawnPointIteration_next()
    {
        return m_spawnpoints[m_tempspawnPointIndexIter++].transform;       
    }

    private void SpawnAllTanks(bool isInitialSpawn)
    {
        RandomExtensions.Shuffle(new System.Random(), m_spawnpoints);
        int numTanksRequested = m_AiTanks.Length + m_HumanTanks.Length;
        if (numTanksRequested > m_spawnpoints.Length)
        {
            Debug.Assert(false, "Not enough spawnpoints available. Aborting spawn process ");
            return;
        }

        SpawnPointIteration_begin();

        Color[] colorsToUseForHumans = { Color.red, Color.blue, Color.green, Color.yellow };
        Color AIColor = Color.black;

        m_Tanks = new TankManager[m_HumanTanks.Length + m_AiTanks.Length];

        for (int i = 0; i < m_HumanTanks.Length + m_AiTanks.Length; i++)
        {
            Transform spawnPointInfo = SpawnPointIteration_next(); // Get A spawn point

            bool spawnAsHuman = i < m_HumanTanks.Length;
            m_Tanks[i] = spawnAsHuman ? m_HumanTanks[i] : m_AiTanks[i - m_HumanTanks.Length];
            m_Tanks[i].m_SpawnPoint = spawnPointInfo;

            // If initial spawn, need to create the objects...
            if (isInitialSpawn)
            {
                m_Tanks[i].m_Instance           = Instantiate(m_TankPrefab, spawnPointInfo.position, spawnPointInfo.rotation) as GameObject;
                m_Tanks[i].m_AIGlobalBlackBox   = m_AIGlobalBlackBox;

                int playerId = i + 1;
                if (spawnAsHuman)
                {
                    m_Tanks[i].SetPlayerAsHuman(playerId);
                    m_Tanks[i].m_PlayerColor = colorsToUseForHumans[i];
                }
                else
                {
                    m_Tanks[i].SetPlayerAsAI(playerId, m_AIGlobalBlackBox);
                    m_Tanks[i].m_PlayerColor = AIColor;
                }

                m_Tanks[i].Setup();
            }
        }        
    }


    private void SetCameraTargets()
    {
        // Create a collection of transforms the same size as the number of tanks.
        Transform[] targets = new Transform[m_Tanks.Length];

        // For each of these transforms...
        for (int i = 0; i < targets.Length; i++)
        {
            // ... set it to the appropriate tank transform.
            targets[i] = m_Tanks[i].m_Instance.transform;
        }

        // These are the targets the camera should follow.
        m_CameraControl.m_Targets = targets;
    }


    // This is called from start and will run each phase of the game one after another.
    private IEnumerator GameLoop()
    {
        // Start off by running the 'RoundStarting' coroutine but don't return until it's finished.
        yield return StartCoroutine(RoundStarting());

        // Once the 'RoundStarting' coroutine is finished, run the 'RoundPlaying' coroutine but don't return until it's finished.
        yield return StartCoroutine(RoundPlaying());

        // Once execution has returned here, run the 'RoundEnding' coroutine, again don't return until it's finished.
        yield return StartCoroutine(RoundEnding());

        // This code is not run until 'RoundEnding' has finished.  At which point, check if a game winner has been found.
        if (m_GameWinner != null)
        {
            // If there is a game winner, restart the level.
            Application.LoadLevel(Application.loadedLevel);
        }
        else
        {
            // If there isn't a winner yet, restart this coroutine so the loop continues.
            // Note that this coroutine doesn't yield.  This means that the current version of the GameLoop will end.
            StartCoroutine(GameLoop());
        }
    }


    private IEnumerator RoundStarting()
    {
        // As soon as the round starts reset the tanks and make sure they can't move.
        ResetAllTanks();
        DisableTankControl();

        // Snap the camera's zoom and position to something appropriate for the reset tanks.
        m_CameraControl.SetStartPositionAndSize();

        // Increment the round number and display text showing the players what round it is.
        m_RoundNumber++;
        m_MessageText.text = "ROUND " + m_RoundNumber;

        // Wait for the specified length of time until yielding control back to the game loop.
        yield return m_StartWait;
    }


    private IEnumerator RoundPlaying()
    {
        // As soon as the round begins playing let the players control the tanks.
        EnableTankControl();

        // Clear the text from the screen.
        m_MessageText.text = string.Empty;

        // While there is not one tank left...
        while (!OneTankLeft())
        {
            // ... return on the next frame.
            yield return null;
        }
    }


    private IEnumerator RoundEnding()
    {
        // Stop tanks from moving.
        DisableTankControl();

        // Clear the winner from the previous round.
        m_RoundWinner = null;

        // See if there is a winner now the round is over.
        m_RoundWinner = GetRoundWinner();

        // If there is a winner, increment their score.
        if (m_RoundWinner != null)
            m_RoundWinner.m_Wins++;

        // Now the winner's score has been incremented, see if someone has one the game.
        m_GameWinner = GetGameWinner();

        // Get a message based on the scores and whether or not there is a game winner and display it.
        string message = EndMessage();
        m_MessageText.text = message;

        // Wait for the specified length of time until yielding control back to the game loop.
        yield return m_EndWait;
    }


    // This is used to check if there is one or fewer tanks remaining and thus the round should end.
    private bool OneTankLeft()
    {
        // Start the count of tanks left at zero.
        int numTanksLeft = 0;

        // Go through all the tanks...
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            // ... and if they are active, increment the counter.
            if (m_Tanks[i].m_Instance.activeSelf)
                numTanksLeft++;
        }

        // If there are one or fewer tanks remaining return true, otherwise return false.
        return numTanksLeft <= 1;
    }


    // This function is to find out if there is a winner of the round.
    // This function is called with the assumption that 1 or fewer tanks are currently active.
    private TankManager GetRoundWinner()
    {
        // Go through all the tanks...
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            // ... and if one of them is active, it is the winner so return it.
            if (m_Tanks[i].m_Instance.activeSelf)
                return m_Tanks[i];
        }

        // If none of the tanks are active it is a draw so return null.
        return null;
    }


    // This function is to find out if there is a winner of the game.
    private TankManager GetGameWinner()
    {
        // Go through all the tanks...
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            // ... and if one of them has enough rounds to win the game, return it.
            if (m_Tanks[i].m_Wins == m_NumRoundsToWin)
                return m_Tanks[i];
        }

        // If no tanks have enough rounds to win, return null.
        return null;
    }


    // Returns a string message to display at the end of each round.
    private string EndMessage()
    {
        // By default when a round ends there are no winners so the default end message is a draw.
        string message = "DRAW!";

        // If there is a winner then change the message to reflect that.
        if (m_RoundWinner != null)
            message = m_RoundWinner.m_ColoredPlayerText + " WINS THE ROUND!";

        // Add some line breaks after the initial message.
        message += "\n\n\n\n";

        // Go through all the tanks and add each of their scores to the message.
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            message += m_Tanks[i].m_ColoredPlayerText + ": " + m_Tanks[i].m_Wins + " WINS\n";
        }

        // If there is a game winner, change the entire message to reflect that.
        if (m_GameWinner != null)
            message = m_GameWinner.m_ColoredPlayerText + " WINS THE GAME!";

        return message;
    }


    // This function is used to turn all the tanks back on and reset their positions and properties.
    private void ResetAllTanks()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].Reset();
        }

        // Respawn
        SpawnAllTanks(false);
    }


    private void EnableTankControl()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].EnableControl();
        }
    }


    private void DisableTankControl()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].DisableControl();
        }
    }
}
