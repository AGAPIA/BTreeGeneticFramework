using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class TutorialManager
{
    [HideInInspector]
    public GameObject m_ArrowPrefab;
    [HideInInspector]
    public GameObject m_CirclePrefab;
    [HideInInspector]
    public Text m_MessageText;                  // Reference to the overlay Text to display winning text, etc.
                                                // The number of maximum game objects that could be indicated by drawings 
    [HideInInspector]
    public int m_MaxIndicatedObjects;

    Color m_baseColor = new Color(1.0f, 0.0f, 0.0f, 1.0f);

    class PositionToHighlight
    {
        public Vector3 pos = Vector3.zero;
        public float radius = 0.0f;
    };

    //[HideInInspector]
   // public GameManager m_GameManagerInstance;

    [HideInInspector]
    public TankManager m_refUserTank;
    [HideInInspector]
    public AIBehavior m_refUserTankAI;
    //[HideInInspector]
    //public TankManager[] m_refAiTanks;




    // Pool of objects prefabs 
    private GameObject[] m_arrowsInstances;
    private GameObject[] m_circleInstances;

    // The current list of positions to highglight in this moment
    private PositionToHighlight[] m_positionsToHighlight; 

    private GlobalAIBlackBox m_globalAIBlackbox;

    enum ScenarioType
    {
        E_SCENARIO_LOW_HEALTH_BOX = 0,
        E_SCENARIO_WEAK_ENEMY,
        E_SCENARIO_TAKE_COVER,
        E_SCENARIO_TAKE_SHIELD,
        E_SCENARIO_NUM
    };


    private bool[] m_ActiveScenarios = new bool[(int) ScenarioType.E_SCENARIO_NUM];

    public void setup(GlobalAIBlackBox globalBlackbox, int maxIndicatedObjects, GameObject arrowPrefab, GameObject circlePrefab, Text textPlaceholder)
    {
        m_globalAIBlackbox = globalBlackbox;

        m_ArrowPrefab = arrowPrefab;
        m_CirclePrefab = circlePrefab;
        m_MessageText = textPlaceholder;
        m_MaxIndicatedObjects = maxIndicatedObjects;

        m_ActiveScenarios[(int)ScenarioType.E_SCENARIO_LOW_HEALTH_BOX] = false;
        m_ActiveScenarios[(int)ScenarioType.E_SCENARIO_TAKE_SHIELD] = false;
        m_ActiveScenarios[(int)ScenarioType.E_SCENARIO_TAKE_COVER] = true;
        m_ActiveScenarios[(int)ScenarioType.E_SCENARIO_WEAK_ENEMY] = false;

        m_arrowsInstances = new GameObject[m_MaxIndicatedObjects];
        m_circleInstances = new GameObject[m_MaxIndicatedObjects];

        // Create the pool of displayed objects
        for (int i = 0; i < m_MaxIndicatedObjects; i++)
        {
            m_arrowsInstances[i] = GameObject.Instantiate(m_ArrowPrefab) as GameObject;
            m_arrowsInstances[i].SetActive(false);

            m_circleInstances[i] = GameObject.Instantiate(m_CirclePrefab) as GameObject;
            m_circleInstances[i].SetActive(false);
        }

        // Search for the first human player and set the references to it
        foreach (TankManager tm in m_globalAIBlackbox.m_TanksRef)
        {
            if (tm.IsAI == false)
            {
                m_refUserTank = tm;
                m_refUserTankAI = tm.m_AI;

                if (m_refUserTankAI.m_actions == null)
                {
                    m_refUserTankAI.init();

                    m_MessageText.enabled = true;
                }
                break;
            }
        }
    }

    bool isScenarioActive(ScenarioType scenarioType)
    {
        return m_ActiveScenarios[(int)scenarioType];
    }

    void resetNotUsedComponents(int numComponentsUsedThisFrame)
    {
        // No components used ? => tutorial is not enabled
        if (numComponentsUsedThisFrame == 0)
        {
            m_MessageText.text = "";
            m_MessageText.gameObject.SetActive(false);
        }

        // Disable not used components
        for (int i = numComponentsUsedThisFrame; i < m_MaxIndicatedObjects; i++)
        {
            m_circleInstances[i].SetActive(false);
            m_arrowsInstances[i].SetActive(false);
        }
    }

    // Given state of the AI, game etc and the decision made, try to explain by text what to do using a decision tree
    // TODO: latter, decide with NLP models
    void setTextDecision(GlobalAIBlackBox state_global, LocalAIBlackBoard state_local, ScenarioType decision)
    {
        string textDecision = "";

        // TODO : refactor this totally
        if (decision == ScenarioType.E_SCENARIO_LOW_HEALTH_BOX)
        {
            textDecision = "Your health is low, go pick a health box !";
        }
        else if (decision == ScenarioType.E_SCENARIO_WEAK_ENEMY)
        {
            textDecision = "This enemy is very weak (see its health bar) ! Go and attack him !";
        }
        else if (decision == ScenarioType.E_SCENARIO_TAKE_COVER)
        {
            textDecision = "This enemy is chasing you ! Try to take cover because he has an weapon upgrade !";
        }
        else if (decision == ScenarioType.E_SCENARIO_TAKE_SHIELD)
        {
            textDecision = "Taking a shield box would make you invincible for a 10 seconds !";
        }

        // Customize the text to show
        m_MessageText.gameObject.SetActive(true);
        m_MessageText.text = textDecision;
        m_baseColor.a = (float)(Math.Abs(Math.Sin(Time.time)) * 1.0f);
        m_MessageText.color = m_baseColor;
    }

    public Transform[] GetCameraTargets()
    {
        Transform[] allObjectsToShowInCamera = new Transform[m_circleInstances.Length];
        for (int i = 0; i < m_circleInstances.Length; i++)
        {
            allObjectsToShowInCamera[i] = m_circleInstances[i].transform;
        }

        return allObjectsToShowInCamera;
    }

    private void HighlightObject(Vector3 pos, ref int numObjectsUsed, Color forcedColor)
    {
        if (numObjectsUsed >= m_MaxIndicatedObjects)
        {
            Debug.Assert(false, "Can't allow more objects to highlight !!");
            return;
        }

        // Highglight it
        m_circleInstances[numObjectsUsed].SetActive(true);
        m_circleInstances[numObjectsUsed].transform.position = pos;
        TutorialCircleGroundDrawing circleInst = m_circleInstances[numObjectsUsed].GetComponent<TutorialCircleGroundDrawing>();
        circleInst.m_baseColor = forcedColor;

        // Customize the arrow
        m_arrowsInstances[numObjectsUsed].SetActive(true);
        TutorialArrowDrawing arrowInst = m_arrowsInstances[numObjectsUsed].GetComponent<TutorialArrowDrawing>();
        arrowInst.m_arrowStart = m_refUserTankAI.gameObject.transform.position;
        arrowInst.m_arrowEnd = pos;
        arrowInst.m_baseColor = forcedColor;

        numObjectsUsed += 1;
    }

    private void HighlightObject(Vector3 pos, ref int numObjectsUsed)
    {
        HighlightObject(pos, ref numObjectsUsed, Color.red); // Default
    }

    public void Update()
    {
        // Human user exists and alive ?
        if (!m_refUserTankAI || !m_refUserTank.IsAlive())
            return;

        Vector3 refUserTankPos = m_refUserTank.m_Instance.transform.position;

        int instancesUsedThisFrame = 0;
        
        // Check for low health scenario as demo
        if (isScenarioActive(ScenarioType.E_SCENARIO_LOW_HEALTH_BOX))
        {
            // Low health ?
            if (m_refUserTank.m_Health.GetRemainingLifePercent() < 0.3f)
            {
                // Try to find a health box to show on tutorial
                Vector3 easiestBoxPos = Vector3.zero;
                float easiestBoxProbability = 0.0f;
                bool found = m_refUserTankAI.m_actions.FindEasiestBoxOfType(BoxType.BOXTYPE_HEALTH, m_globalAIBlackbox, out easiestBoxPos, out easiestBoxProbability);

                // If found and easy enough to get to it...
                if (found && easiestBoxProbability > 0.25f)
                {
                    HighlightObject(easiestBoxPos, ref instancesUsedThisFrame);
                    setTextDecision(m_globalAIBlackbox, m_refUserTankAI.m_localAIBlackBox, ScenarioType.E_SCENARIO_LOW_HEALTH_BOX);
                }
            }
        }
        // Check for low take a shield box scenario
        else if (isScenarioActive(ScenarioType.E_SCENARIO_TAKE_SHIELD))
        {
            // Try to find a health box to show on tutorial
            Vector3 easiestBoxPos = Vector3.zero;
            float easiestBoxProbability = 0.0f;
            bool found = m_refUserTankAI.m_actions.FindEasiestBoxOfType(BoxType.BOXTYPE_SHIELD, m_globalAIBlackbox, out easiestBoxPos, out easiestBoxProbability);

            // If found and easy enough to get to it...
            if (found && easiestBoxProbability > 0.25f)
            {
                // Highglight it
                HighlightObject(easiestBoxPos, ref instancesUsedThisFrame);
                setTextDecision(m_globalAIBlackbox, m_refUserTankAI.m_localAIBlackBox, ScenarioType.E_SCENARIO_TAKE_SHIELD);
            }
        }
        else if (isScenarioActive(ScenarioType.E_SCENARIO_WEAK_ENEMY))
        {
            float MAX_DIST_TO_SHOW_WEAK_ENEMY_SQR = 20.0f * 20.0f;
            IndexValuePosPair[] sortedClosestAgents;
            int numClosestAgents = 0;
            m_refUserTankAI.m_actions.FindClosestOpponentsToAgent(m_refUserTank, m_globalAIBlackbox, out sortedClosestAgents, out numClosestAgents);

            TankManager weakestEnemyAround = null;
            for (int i = 0; i < numClosestAgents; i++)
            {
                // Check if this enemy is alive and in the allowed range of action
                int enemeyIndex = sortedClosestAgents[i].index;
                Debug.Assert(0 <= enemeyIndex && enemeyIndex < m_globalAIBlackbox.m_TanksRef.Length);

                TankManager enemyTankManager = m_globalAIBlackbox.m_TanksRef[enemeyIndex];
                Debug.Assert(enemyTankManager.IsAlive());

                float distToEnemy_sqr = (sortedClosestAgents[i].pos - refUserTankPos).sqrMagnitude;
                if (distToEnemy_sqr > MAX_DIST_TO_SHOW_WEAK_ENEMY_SQR)
                    break;

                // Evaluate how dangerous the enemy is
                float enemyDangerScore = m_refUserTankAI.m_actions.EvaluateTankDangerStatus(enemyTankManager);
                if (enemyDangerScore < 0.3f)
                {
                    weakestEnemyAround = enemyTankManager;
                }
            }

            if (weakestEnemyAround != null)
            {
                Vector3 weakestEnemeyPos = weakestEnemyAround.m_Instance.transform.position;

                HighlightObject(weakestEnemeyPos, ref instancesUsedThisFrame);
                setTextDecision(m_globalAIBlackbox, m_refUserTankAI.m_localAIBlackBox, ScenarioType.E_SCENARIO_WEAK_ENEMY);
            }
        }
        else if (isScenarioActive(ScenarioType.E_SCENARIO_TAKE_COVER))
        {
            // Get closest enemies that looks like are chasing us.
            float MAX_DISTANCE_TO_CONSIDER_CHASING = 20.0f;
            float MAX_ANGLE_TO_CONSIDER_CHASING = 45.0f;
            float SAFE_COVER_DISTANCE = 40.0f; // Minimum distance to consider safe against any enemies
            int IDEAL_NUM_POINTS_TO_RETURN = Math.Min(3, m_MaxIndicatedObjects);
            int MAX_NUM_POINTS_TO_EVALUATE = 100;

            IndexValuePosPair[] sortedClosestAgents;
            int numClosestAgents = 0;
            m_refUserTankAI.m_actions.FindClosestOpponentsChasingTank(m_refUserTank, m_globalAIBlackbox, 
                MAX_DISTANCE_TO_CONSIDER_CHASING,
                MAX_ANGLE_TO_CONSIDER_CHASING,
                out sortedClosestAgents, out numClosestAgents);

            // Is there any agent chasing us within some range ?
            if (numClosestAgents > 0 && sortedClosestAgents[0].value < MAX_DISTANCE_TO_CONSIDER_CHASING)
            {
                IndexValuePosPair[] sortedCoverPoints;
                int numCoverPoints = 0;
                m_refUserTankAI.m_actions.FindBestCoverPositions(m_refUserTank, m_globalAIBlackbox, SAFE_COVER_DISTANCE, IDEAL_NUM_POINTS_TO_RETURN,
                    MAX_NUM_POINTS_TO_EVALUATE,
                    out sortedCoverPoints, out numCoverPoints);

                // Highlight the enemy chasing us
                HighlightObject(sortedClosestAgents[0].pos, ref instancesUsedThisFrame, Color.red);

                // Highlight the cover positions
                if (numCoverPoints > 0)
                {
                    for (int i = 0; i < numCoverPoints; i++)
                    {
                        HighlightObject(sortedCoverPoints[i].pos, ref instancesUsedThisFrame, Color.green);
                    }

                    setTextDecision(m_globalAIBlackbox, m_refUserTankAI.m_localAIBlackBox, ScenarioType.E_SCENARIO_TAKE_COVER);
                }
            }


            // Spawn N points randomly on the navmesh
            // From these take the best M points that are furthest from all enemies
        }

        resetNotUsedComponents(instancesUsedThisFrame);
    }

};
