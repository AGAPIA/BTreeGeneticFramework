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

    private GlobalAIBlackBox m_globalBlackbox;

    enum ScenarioType
    {
        E_SCENARIO_LOW_HEALTH_BOX = 0,
        E_SCENARIO_NUM
    };

    private bool[] m_ActiveScenarios = new bool[(int) ScenarioType.E_SCENARIO_NUM];

    public void setup(GlobalAIBlackBox globalBlackbox, int maxIndicatedObjects, GameObject arrowPrefab, GameObject circlePrefab, Text textPlaceholder)
    {
        m_globalBlackbox = globalBlackbox;

        m_ArrowPrefab = arrowPrefab;
        m_CirclePrefab = circlePrefab;
        m_MessageText = textPlaceholder;
        m_MaxIndicatedObjects = maxIndicatedObjects;

        m_ActiveScenarios[(int)ScenarioType.E_SCENARIO_LOW_HEALTH_BOX] = true;

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
        foreach (TankManager tm in m_globalBlackbox.m_TanksRef)
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

    void resetComponents()
    {
        /*
        m_MessageText.text = "";
        foreach (GameObject circleInstance in m_circleInstances)
        {
            circleInstance.SetActive(false);
        }

        foreach (GameObject arrowInstance in m_arrowsInstances)
        {
            arrowInstance.SetActive(false);
        }
        */
    }


    public void Update()
    {
        resetComponents();
        
        // Check for low health scenario as demo
        if (isScenarioActive(ScenarioType.E_SCENARIO_LOW_HEALTH_BOX) && m_refUserTank.m_Health.GetRemainingLifePercent() < 2.0f)
        {
            // Human user exists and alive ?
            if (m_refUserTankAI && m_refUserTank.IsAlive())
            {
                // Try to find a health box to show on tutorial
                Vector3 easiestBoxPos = Vector3.zero;
                float easiestBoxProbability = 0.0f;
                bool found = m_refUserTankAI.m_actions.FindEasiestBoxOfType(BoxType.BOXTYPE_HEALTH, m_globalBlackbox, out easiestBoxPos, out easiestBoxProbability);

                // If found and easy enough to get to it...
                if (found && easiestBoxProbability > 0.05f)
                {
                    // Highglight it
                    m_circleInstances[0].SetActive(true);
                    m_circleInstances[0].transform.position = easiestBoxPos;

                    m_arrowsInstances[0].SetActive(true);
                    TutorialArrowDrawing arrowInst = m_arrowsInstances[0].GetComponent<TutorialArrowDrawing>();
                    arrowInst.m_arrowStart = m_refUserTankAI.gameObject.transform.position;
                    arrowInst.m_arrowEnd = easiestBoxPos;

                    m_MessageText.text = "Your health is low, go pick a health box !";
                }
            }

            // Select the safest box closest to the player and present them
            //ArrayList boxPositions = m_globalBlackbox.m_boxPositionsByType[BoxType.BOXTYPE_HEALTH];

        }
    }

};
