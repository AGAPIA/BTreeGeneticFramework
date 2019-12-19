using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerAgentDebugStorage
{
    // A reference to the agent itself ! Do it whatever you want with it !
    [HideInInspector]
    public TankManager m_agent;

    // Both variables below are part of m_agent actually but are copied here for access efficiency
    // The data is stored on the behavior itself. THis is what we query at runtime for visual debugging
    [HideInInspector]
    public AIBehavior m_behavior;

    // A reference the tankUI component to inject debug text
    [HideInInspector]
    public TankUI m_tankUI;

    public void reset()
    {

    }
}

public class AIDebugHelper : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // TODO: reset things here
    }


    public bool m_DrawOverallState          = false;
    public bool m_DrawHealthBoxScores       = false;
    public bool m_DrawImmFireLastRayHitInfo = false;

    bool m_isSetupFinished = false;

    PerAgentDebugStorage[] m_storagePerTank;
    public PerAgentDebugStorage GetPerAgentStorage(int agentId)
    {
        return m_storagePerTank[agentId];
    }

    public void Setup(TankManager[] allAgents)
    {
        m_storagePerTank = new PerAgentDebugStorage[allAgents.Length];
        for (int agentId = 0; agentId < allAgents.Length; agentId++)
        {
            PerAgentDebugStorage storage = new PerAgentDebugStorage();
            m_storagePerTank[agentId] = storage;

            TankManager agent   = allAgents[agentId];
            storage.m_agent     = agent;
            storage.m_behavior  = agent.m_AI;
            storage.m_tankUI    = agent.m_tankUI;
        }

        m_isSetupFinished = true;
    }

    public void Reset()
    {
        for (int i = 0; i < m_storagePerTank.Length; i++)
        {
            PerAgentDebugStorage storage = m_storagePerTank[i];

            storage.reset();
        }
    }

    public void OnDrawGizmos()
    {
        if (!m_isSetupFinished)
        {
            return;
        }


        for (int i = 0; i < m_storagePerTank.Length; i++)
        {
            PerAgentDebugStorage storage    = m_storagePerTank[i];
            if (!storage.m_agent.IsAlive())
                continue;

            string textToDisplay = "";

            Vector3 thisAgentPos = storage.m_behavior.gameObject.transform.position;

            if (m_DrawImmFireLastRayHitInfo)
            {
                if (storage.m_behavior.m_iminentActions.lastShootRayHit.transform == null)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawRay(storage.m_behavior.m_iminentActions.lastUsedShootRay);
                }
                else
                {
                    Gizmos.color = storage.m_behavior.m_iminentActions.lastShootRayHitTank ? Color.red : Color.green;
                    Gizmos.DrawLine(storage.m_behavior.m_iminentActions.lastUsedShootRay.origin,
                                    storage.m_behavior.m_iminentActions.lastShootRayHit.point);
                }
            }

            if (m_DrawOverallState)
            {
                string baseString = System.String.Format("Id: {0} S[{1}] IM[F:{2}]\n", storage.m_behavior.m_id,
                                        storage.m_behavior.m_localAIBlackBox.m_currentState.ToString(),
                                        storage.m_behavior.m_iminentActions.m_fire ? 1 : 0);

                textToDisplay += baseString;
            }

            if (m_DrawHealthBoxScores)
            {
                AIBehavior_Utility utilityBehavior = storage.m_behavior as AIBehavior_Utility;
                if (utilityBehavior)
                {
                    BoxTypeEval eval = utilityBehavior.GetBoxTypeEval(BoxType.BOXTYPE_HEALTH);
                    string evalText = System.String.Format("S:{0} [N:{1}][P:{2}]\n", eval.score, eval.needForBox, eval.probabilityToGetBox);
                    textToDisplay += evalText;

                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(thisAgentPos, eval.pos);
                }
            }

            if (storage.m_tankUI != null)
            {
                storage.m_tankUI.setDebugText(textToDisplay);
            }
        }
    }
}
