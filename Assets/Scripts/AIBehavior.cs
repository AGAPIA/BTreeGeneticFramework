// Different strategies that we can use:
// Hierarchical: Defense, Attack, Look for Boxes. We can also combine them such that: Defense + Look for boxes or Attach + Look.
// Local strategies are very sensitive to parameters:
// - is there any enemy looking like he is going to take a box ?
// - is there any enemy with active shield around me ? => evade (defense)
// - does enemy have upgrades / more lives than me ? Maybe I shouldn't initiate attack if he is better positioned than me.
// - how low is too low on health / ammo to get some boxes ?
// - does enemy shoot me and he is better, has more lives, shields etc ? Maybe hide 
// RL or GANs to mimic different users ?

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class IndexValuePosPair
{
    public int index;
    public float value;
    public Vector3 pos;

};

public class IndexValuePairLowestComparer : IComparer<IndexValuePosPair>
{
    public int Compare(IndexValuePosPair x, IndexValuePosPair y)
    {
        // Compare x and y in reverse order. If one is invalid is put to the end of the sorted array always
        if (x.index == UtilsGeneral.INVALID_INDEX)
        {
            if (y.index == UtilsGeneral.INVALID_INDEX)
                return 0;
            else return 1;
        }
        else
        {
            if (y.index == UtilsGeneral.INVALID_INDEX)
                return -1;
            else
            {
                // Both values are valid
                return x.value < y.value ? -1 : (x.value > y.value ? 1 : 0);
            }
        }

    }
}

// These are actions that needs to be taken whatever utility there is (forced , imposed actions like).
public class ImminentActions
{
    // SHould fire decision + debug info captured about this? 
    public bool m_fire              = false; 
    public RaycastHit  lastShootRayHit;
    public Ray         lastUsedShootRay;
    public bool        lastShootRayHitTank;

    public ImminentActions() { reset(); }

    public void reset()
    {
        m_fire = false;
        lastShootRayHit = default(RaycastHit);
        lastUsedShootRay = default(Ray);
        lastShootRayHitTank = false;
    }
};

// Abstract behavior class attached to a tank agent. All will be derived from this
public abstract class AIBehavior : MonoBehaviour
{
    // Imposed actions regardless of their utility
    public ImminentActions m_iminentActions = new ImminentActions();
    //-------

    public GlobalAIBlackBox m_globalAIBlackBox;
    public LocalAIBlackBoard m_localAIBlackBox;

    // Min time to continue the same decision taken
    public float m_minTimeToContinueSameAction = 4.0f;

    // Max time to continue the same decision taken
    public float m_maxTimeToContinueSameAction = 10.0f;


    bool m_isSetupFinished = false;

    // --------------------------------------------

    // Gathers informations for the local AI blackboard
    void FillLocalBlackBoard()
    {
        m_localAIBlackBox.deltaTime = Time.deltaTime;
    }

    public void SetGlobalBlackbox(GlobalAIBlackBox aiGlobalBlackBox)
    {
        m_globalAIBlackBox = aiGlobalBlackBox;
    }

    //Rigidbody m_tankRigidBody;
    [HideInInspector]
    public TankMovement m_tankMovement;
    [HideInInspector]
    public TankShooting m_tankShooting;
    [HideInInspector]
    public TankHealth   m_tankHealth;
    [HideInInspector]
    public Transform m_fireTransform;
    [HideInInspector]
    public Rigidbody m_rigidBody;

    [HideInInspector]
    public NavMeshAgent m_navMeshAgent;
    public TankUI m_tankUI; // For debugging support
    public int m_id = -1;
    public LayerMask m_EnemyLayerMask;

    protected AIBehaviorActions m_actions;

    // Start is called before the first frame update
    void Start()
    {
        
        //m_tankRigidBody = gameObject.GetComponent<Rigidbody>();
        m_localAIBlackBox = new LocalAIBlackBoard();

        m_tankMovement  = gameObject.GetComponent<TankMovement>();
        m_tankShooting  = gameObject.GetComponent<TankShooting>();
        m_tankHealth    = gameObject.GetComponent<TankHealth>();
        m_fireTransform = gameObject.transform.Find("FireTransform");
        m_rigidBody     = gameObject.GetComponent<Rigidbody>();
        m_navMeshAgent  = GetComponent<NavMeshAgent>();

        //m_navMeshAgent.speed = m_tankMovement.m_Speed * GameManager.m_SpeedMultiplier;
        //m_navMeshAgent.angularSpeed = m_tankMovement.m_TurnSpeed * GameManager.m_SpeedMultiplier;
        m_actions       = new AIBehaviorActions(this);
    }

    // Executes an action based on observations.
    // Observation are located in blackboxes (local or global), while actions are leveraged to m_actions
    public abstract void Execute();

    public void onSetupFinished() { m_isSetupFinished = true; }

    // Update is called once per frame
    void Update()
    {
        if (!enabled || !m_isSetupFinished)
            return;

        FillLocalBlackBoard();

        Execute();
    }

    private void OnDrawGizmos()
    {
    
    }
}


/// <summary>
///  These are the set of generic actions and query that the agent can do in general regardless of the behavior
///  The purpose of this is to simplify the client code (the one implementing behaviors in this case)
///  Also it provide a set of queries 
/// </summary>
public class AIBehaviorActions
{
    public AIBehaviorActions(AIBehavior baseObj)
    {
        m_base = baseObj;

        GameObject mainObject = GameObject.Find("GameManager");
        GameManager gameManager = mainObject.GetComponent<GameManager>();
        int numTotalTanks = gameManager.m_AiTanks.Length + gameManager.m_HumanTanks.Length;
        BoxesSpawnScript boxesSpawnManager = mainObject.GetComponent<BoxesSpawnScript>();
        

        m_tempSortedBoxes = new IndexValuePosPair[(int)boxesSpawnManager.MaxNumberOfBoxesPerType];
        for (int i = 0; i < (int)boxesSpawnManager.MaxNumberOfBoxesPerType; i++)
        {
            m_tempSortedBoxes[i] = new IndexValuePosPair();
        }

        m_tempSortedOpponents = new IndexValuePosPair[numTotalTanks];
        for (int i = 0; i < numTotalTanks; i++)
        {
            m_tempSortedOpponents[i] = new IndexValuePosPair();
        }
        
        m_tempSortedBoxes_count = m_tempSortedOpponents_count = 0;
    }

    private AIBehavior m_base;

    // Fire in the forward direction
    public void Fire()
    {
        m_base.m_tankShooting.Fire();
    }

    public void MoveToPosition(Vector3 targetPos)
    {
        m_base.m_navMeshAgent.SetDestination(targetPos);
    }

    public void TurnToPosition(Vector3 targetPos)
    {
        m_base.m_tankMovement.TurnToTarget(targetPos, m_base.m_localAIBlackBox.deltaTime);
    }

    // Finds the closest box of a given type from a source position
    public Vector3 FindClosestBoxPosFromPos(BoxType boxType, Vector3 sourcePos)
    {
        ArrayList boxPositions = m_base.m_globalAIBlackBox.m_boxPositionsByType[boxType];
        float closestDist = float.MaxValue;
        Vector3 closestPos = UtilsGeneral.INVALID_POS;
        foreach (Vector3 pos in boxPositions)
        {
            float sqrDistToThis = (pos - sourcePos).sqrMagnitude;
            if (sqrDistToThis < closestDist)
            {
                closestDist = sqrDistToThis;
                closestPos = pos;
            }
        }

        return closestPos;
    }

    // Checks wheter if a box of a give type exist at a known pos. 
    // TODO: this is not optimal a listener system should be used here 
    public bool IsBoxTypeAndPosAvailable(BoxType boxType, Vector3 knownBoxPos)
    {
        ArrayList boxPositions = m_base.m_globalAIBlackBox.m_boxPositionsByType[boxType];
        foreach (Vector3 pos in boxPositions)
        {
            if ((pos - knownBoxPos).sqrMagnitude < Mathf.Epsilon)
                return true;
        }

        return false;
    }

    public struct MoveToBoxParams
    {

    };

    // Moves to the closest box of a certain type
    // TODO: use specs efficienty
    public bool MoveToGetClosestBox(BoxType boxType, MoveToBoxParams specs)
    {
        Vector3 callerTankPos = m_base.gameObject.transform.position;
        Vector3 closestBoxPos = FindClosestBoxPosFromPos(boxType, callerTankPos);

        if (closestBoxPos == UtilsGeneral.INVALID_POS)
            return false;

        // TODO: check other enemyies maybe they follow the path to this box or are very close to it.
        // A lot of parameters to vary and learn here
        MoveToPosition(closestBoxPos);
        return true;
    }

    // Checks if a given enemy tank is visible from a source position and direction.
    public bool IsEnemyTankInShootingDirection(Transform otherTankTransform, float maxDistance)
    {
        bool isOtherTankVisibleFromThis = false;

        RaycastHit hit;
        Ray ray         = default(Ray);
        ray.origin      = m_base.m_fireTransform.position; 
        ray.direction   = m_base.m_fireTransform.forward; 
        int layerMask   = ~0;
        if (Physics.Raycast(ray, out hit, maxDistance + 2.0f, layerMask))
        {
            bool isEnemyMask                            = ((m_base.m_EnemyLayerMask & (1 << hit.transform.gameObject.layer))) != 0;
            bool isTargetTheCorrectTank                 = hit.transform == otherTankTransform;

            isOtherTankVisibleFromThis                  = isEnemyMask && isTargetTheCorrectTank;

            // Complete some debug info
            m_base.m_iminentActions.lastShootRayHit     = hit;
            m_base.m_iminentActions.lastUsedShootRay    = ray;
            m_base.m_iminentActions.lastShootRayHitTank = isOtherTankVisibleFromThis;
        }

        return isOtherTankVisibleFromThis;
    }

    // Gestimated time to get from a source to a fixed target - fast version, no pathfinding
    public float GetEstimatedTimeSourceToStaticTarget_Fast(Vector3 src, Vector3 dest)
    {
        float dist = (dest - src).magnitude;
        return (dist / TankMovement.m_Speed);
    }

    // Gestimated time to get from a source to a fixed target - exact version, using pathfinding data + corners slowdown etc
    public float GetEstimatedTimeSourceToStaticTarget_Exact(Vector3 src, Vector3 dest)
    {
        NavMeshPath path = new NavMeshPath();

        // If no path found ?
        if (!NavMesh.CalculatePath(src, dest, NavMesh.AllAreas, path))
        {
            return Mathf.Infinity;
        }

        // TODO: very simplistic, but we should estimate better because speed decreases around corners
        float totalDist = 0;
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            totalDist += (path.corners[i + 1] - path.corners[i]).magnitude;
        }

        return (totalDist / TankMovement.m_Speed);
    }

    // These are constants and preallocated arrays for computing various queries for box finding
    IndexValuePairLowestComparer m_indexValuePairComparer = new IndexValuePairLowestComparer();
    IndexValuePosPair[] m_tempSortedBoxes;
    IndexValuePosPair[] m_tempSortedOpponents;
    int m_tempSortedBoxes_count         = 0;
    int m_tempSortedOpponents_count     = 0;

    public void FindClosestOpponentsToAgent(TankManager agent, GlobalAIBlackBox globalBlackboard,
                                            out IndexValuePosPair[]sortedClosestAgents, out int numClosestAgents)
    {
        numClosestAgents    = 0;
        Vector3 agentPos    = agent.m_Instance.transform.position;
        sortedClosestAgents = null;

        // Take the sorted list of agents closest to that box (first M)
        TankManager[] tanks = globalBlackboard.m_TanksRef;
        m_tempSortedOpponents_count = 0;

        for (int oppIdx = 0; oppIdx < tanks.Length; oppIdx++)
        {
            // Init with an invalid value every entry
            m_tempSortedOpponents[oppIdx].value = UtilsGeneral.MAX_SCORE_VALUE; // max because it's distance, and we sort ascending
            m_tempSortedOpponents[oppIdx].index = UtilsGeneral.INVALID_INDEX;

            TankManager tankIter = tanks[oppIdx];
            if (tankIter.m_Instance == m_base.gameObject || tankIter.IsAlive() == false)
            {
                continue;
            }

            Vector3 tankIterPos = tankIter.m_Instance.transform.position;

            m_tempSortedOpponents[m_tempSortedOpponents_count].index = oppIdx;
            m_tempSortedOpponents[m_tempSortedOpponents_count].value = GetEstimatedTimeSourceToStaticTarget_Fast(tankIterPos, agentPos);//Vector3.Distance(tankIterPos, thisTankPos);
            m_tempSortedOpponents[m_tempSortedOpponents_count].pos = tankIterPos;
            m_tempSortedOpponents_count++;
        }

        System.Array.Sort(m_tempSortedOpponents, 0, m_tempSortedOpponents_count, m_indexValuePairComparer);

        numClosestAgents    = m_tempSortedOpponents_count;
        sortedClosestAgents = m_tempSortedOpponents;
    }


    // Returns false if no compatible box was found.
    // Otherwise, it return the best rated box pos and score in the last two params
    public bool FindEasiestBoxOfType(BoxType testBoxType, GlobalAIBlackBox globalBlackboard, out Vector3 easiestBoxPos, out float easiestBoxProbability)
    {
        // Inputs: - get closest K participants to the box
        //         - get closest M boxes of the types looking for
        // Outputs: - compute my probability for each of the M boxes then select the highest rated one and return it

        // For faster version: use Max-heaps and get complexity O((K+M)*logN)

        easiestBoxPos           = UtilsGeneral.INVALID_POS;
        easiestBoxProbability   = -1.0f;
        Vector3 thisTankPos     = m_base.transform.position;

        // Get the list of box positions and retain the top K among them
        ArrayList listOfBoxPositions = globalBlackboard.m_boxPositionsByType[testBoxType];
        m_tempSortedBoxes_count = 0;
        for (int i = 0; i < listOfBoxPositions.Count; i++)
        {
            Vector3 thisBoxPos = ((Vector3)listOfBoxPositions[i]);
            m_tempSortedBoxes[i].index = i;
            m_tempSortedBoxes[i].value = GetEstimatedTimeSourceToStaticTarget_Fast(thisBoxPos, thisTankPos);// (thisBoxPos - m_base.transform.position).magnitude;
            m_tempSortedBoxes[i].pos = thisBoxPos;
            m_tempSortedBoxes_count++;
        }

        System.Array.Sort(m_tempSortedBoxes, 0, m_tempSortedBoxes_count, m_indexValuePairComparer);

        int bestBoxIdx       = UtilsGeneral.INVALID_INDEX;
        float bestBoxScore    = UtilsGeneral.MAX_SCORE_VALUE;

        // Take the sorted list of closest boxes (first K)
        for (int boxIdx = 0; boxIdx < AIBehavior_Utility.Params.m_bfClosestBoxNumbersLookingFor; boxIdx++)
        {
            if (boxIdx >= m_tempSortedBoxes_count)
            {
                break;
            }

            Vector3 thisBoxPos              = m_tempSortedBoxes[boxIdx].pos;
            float ourAgentTimeToThisBox     = m_tempSortedBoxes[boxIdx].value;

            // Take the sorted list of agents closest to that box (first M)
            TankManager[] tanks = globalBlackboard.m_TanksRef;
            {
                m_tempSortedOpponents_count = 0;
                
                for (int oppIdx = 0; oppIdx < tanks.Length; oppIdx++)
                {
                    // Init with an invalid value every entry
                    m_tempSortedOpponents[oppIdx].value = UtilsGeneral.MAX_SCORE_VALUE; // max because it's distance, and we sort ascending
                    m_tempSortedOpponents[oppIdx].index = UtilsGeneral.INVALID_INDEX;

                    TankManager tankIter = tanks[oppIdx];
                    if (tankIter.m_Instance == m_base.gameObject || tankIter.IsAlive() == false)
                    {
                        continue;
                    }

                    Vector3 tankIterPos = tankIter.m_Instance.transform.position;

                    m_tempSortedOpponents[m_tempSortedOpponents_count].index = oppIdx;
                    m_tempSortedOpponents[m_tempSortedOpponents_count].value = GetEstimatedTimeSourceToStaticTarget_Fast(tankIterPos, thisBoxPos);//Vector3.Distance(tankIterPos, thisTankPos);
                    m_tempSortedOpponents[m_tempSortedOpponents_count].pos = tankIterPos;
                    m_tempSortedOpponents_count++;
                }

                System.Array.Sort(m_tempSortedOpponents, 0, m_tempSortedOpponents_count, m_indexValuePairComparer);
            }

            // Now, for this box, check our probability against each of the M agents
            // We'll keep the relative score in each m_tempSortedOpponentsToBox location
            for (int oppIdx = 0; oppIdx < AIBehavior_Utility.Params.m_bfClosestOpponentsLookingFor; oppIdx++)
            {
                if (oppIdx >= m_tempSortedOpponents_count)
                {
                    break;
                }

                // Compute probability / score here and put it in value
                // Compare the angle between (opponent to boxn VS  current opponent avg dir)

                // First, compute the  probability that the opponent has to get to the box by looking at his direction and estimated time to target
                Vector3 thisOpponentPos                     = m_tempSortedOpponents[oppIdx].pos;
                int thisOpponentGlobalIndex                 = m_tempSortedOpponents[oppIdx].index;
                Vector3 opponentToBox                       = thisBoxPos - thisOpponentPos;
                Vector3 thisOpponentAvgVel                  = tanks[thisOpponentGlobalIndex].m_Movement.m_movingAvgVel;
                float angleBetweenDirs                      = Vector3.Angle(thisOpponentAvgVel, opponentToBox);
                float intentionProbability                  = UtilsGeneral.lerp(angleBetweenDirs,
                                                                                AIBehavior_Utility.Params.m_angleX1,
                                                                                AIBehavior_Utility.Params.m_angleX2,
                                                                                AIBehavior_Utility.Params.m_angleY1,
                                                                                AIBehavior_Utility.Params.m_angleY2);


                // Compute the opponent probability of this opponent relative THIS TANK 
                float opponentTimeToThisBox                 = m_tempSortedOpponents[oppIdx].value;
                float timeToGetProbability_raw              = (ourAgentTimeToThisBox / (opponentTimeToThisBox + Mathf.Epsilon));
                float timeToGetProbability_clamped          = Mathf.Clamp(timeToGetProbability_raw, 0.0f, 1.0f);

                float alpha                                 = AIBehavior_Utility.Params.m_boxAlphaTime;
                float totalOpponentProbability               = timeToGetProbability_clamped * alpha + 
                                                                (1.0f - alpha) * intentionProbability;

                // This contains the opponent probability relative to THIS TANK
                m_tempSortedOpponents[oppIdx].value    = Mathf.Clamp(totalOpponentProbability, 0.0f, 1.0f);
            }

            // Now select the best positioned opponent to this box (the one with the highest probability relative to THIS TANK)
            // the lowest for THIS TANK
            int bestOppIdxToThisBox                         = -1;
            float bestOppValueToThisBox                     = UtilsGeneral.MIN_SCORE_VALUE;
            for (int oppIdx = 0; oppIdx < AIBehavior_Utility.Params.m_bfClosestOpponentsLookingFor; oppIdx++)
            {
                if (oppIdx >= m_tempSortedOpponents_count)
                {
                    break;
                }

                if (m_tempSortedOpponents[oppIdx].value > bestOppValueToThisBox)
                {
                    bestOppValueToThisBox = m_tempSortedOpponents[oppIdx].value;
                    bestOppIdxToThisBox = m_tempSortedOpponents[oppIdx].index;
                }
            }

            // Finally, compare if this global box optimal (probability) is worser than this box opponent relative score
            // We have to select the box with minimum probability for our opponents relative to THIS TANK
            if (bestBoxScore > bestOppValueToThisBox)
            {
                bestBoxScore = bestOppValueToThisBox;
                bestBoxIdx = m_tempSortedBoxes[boxIdx].index;
            }
        }

        if (bestBoxIdx != UtilsGeneral.INVALID_INDEX)
        {
            // Now invert the probability because we are interested in THIS TANK not the one of opponents relative to our tank
            easiestBoxProbability = Mathf.Clamp(1.0f - bestBoxScore, 0.0f, 1.0f);
            easiestBoxPos = ((Vector3)listOfBoxPositions[bestBoxIdx]);
            return true;
        }
        else
        {
            return false;
        }
    }
};

