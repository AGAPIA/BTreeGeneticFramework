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

// Dummy behavior class. All will be derived from this maybe ?
public class AIBehavior : MonoBehaviour
{
    public GlobalAIBlackBox m_globalAIBlackBox;
    public LocalAIBlackBoard m_localAIBlackBox;

    // Min time to continue the same decision taken
    public float m_minTimeToContinueSameAction = 4.0f;

    // Max time to continue the same decision taken
    public float m_maxTimeToContinueSameAction = 10.0f;

    public bool EnableDebugging = true;

    // Store behavior state and functionality 
    // --------------------------------------------
    // Current 
    public enum BehaviorState
    {
        BS_IDLE,
        BS_DEFEND,
        BS_ATTACK,
        BS_FIND_BOX
    };

    BehaviorState m_currentState;

    // The time spent in the current action chosen
    float m_timeInCurrentAction = 0.0f;
    float m_targetTimeInCurrentAction = 0.0f; // How much should we spent in this action

    BoxType m_boxLookingFor_type = BoxType.BOXTYPE_NUMS; // Only valid if state is BS_FIND_BOX
    Vector3 m_boxLookingFor_pos = AIBehaviorActions.s_invalidPos; // Same as above
    // --------------------------------------------

    // Gathers informations for the local AI blackboard
    void FillLocalBlackBoard()
    {
        m_localAIBlackBox.deltaTime = Time.deltaTime;
    }



    public class DebugInfo 
    {
        public Ray lastUsedRay;
        public RaycastHit lastRayHit;
        public bool lastRayHitTank;
        

        public void Reset()
        {
            lastUsedRay = default(Ray);
            lastRayHitTank = false;
            lastRayHit = default(RaycastHit);
        }
    };

    public void SetGlobalBlackbox(GlobalAIBlackBox aiGlobalBlackBox)
    {
        m_globalAIBlackBox = aiGlobalBlackBox;
    }

    //Rigidbody m_tankRigidBody;
    public TankMovement m_tankMovement;
    public TankShooting m_tankShooting;
    public TankHealth   m_tankHealth;
    public Transform m_fireTransform;
    public Rigidbody m_rigidBody;
    public DebugInfo m_debugInfo = new DebugInfo();
    public NavMeshAgent m_navMeshAgent;
    public TankUI m_tankUI; // For debugging support
    public int m_id = -1;
    public LayerMask m_EnemyLayerMask;

    AIBehaviorActions m_actions;

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

    // Checks if the current state is finished or not. If yes, choose a new state
    void CheckCurrentState()
    {
        Vector3 thisObjectPos = gameObject.transform.position;

        // Simple strategy:
        // Iminent actions:
        //  A. If enemy is visible and within shoot range turn to him and shoot ! Otherwise he might shoot us
        //  B. If low on ammo or health move to get some
        // Within a limited time, DEFEND
        // Within a limited time, pathfind and ATTACK
        // Within a limited time, search for shields or ammo upgrade = IMPROVE strategy

        // Check if the current action is finished
        m_timeInCurrentAction += Time.deltaTime;
        bool isTargetTimePast = m_timeInCurrentAction > m_targetTimeInCurrentAction;
        bool isLookingForBoxAndItWasTaken = m_currentState == BehaviorState.BS_FIND_BOX && m_actions.IsBoxTypeAndPosAvailable(m_boxLookingFor_type, m_boxLookingFor_pos) == false;
       
        // Time to reset and change state ?
        if (isTargetTimePast || isLookingForBoxAndItWasTaken)
        {
            // Choose a new target action and time
            m_targetTimeInCurrentAction = Random.Range(m_minTimeToContinueSameAction, m_maxTimeToContinueSameAction);
            m_timeInCurrentAction = 0.0f;
            m_currentState = BehaviorState.BS_IDLE;
            m_boxLookingFor_type = BoxType.BOXTYPE_NUMS;
            m_boxLookingFor_pos = AIBehaviorActions.s_invalidPos;
        }

        // Nothing changed continue using the same action
        if (m_currentState != BehaviorState.BS_IDLE)
            return;

        // TODO: maybe score each item and choose the highest score ? but how to score them ?
        // Currently it is a random priority thing

        // Step 1: Do we need a life box ?
        int numLivesThreshold = Random.Range(1, Mathf.CeilToInt(m_tankHealth.m_InitialNumLives * 0.5f));
        float healthPercentThreshold = Random.Range(0.0f, 1.0f);
        if (m_tankHealth.GetRemainingHealths() <= numLivesThreshold &&
                m_tankHealth.GetRemainingLifePercent() < healthPercentThreshold)
        {
            Vector3 closestHPToMe = m_actions.FindClosestBoxPosFromPos(BoxType.BOXTYPE_HEALTH, thisObjectPos);
            if (closestHPToMe != AIBehaviorActions.s_invalidPos)
            {
                m_currentState = BehaviorState.BS_FIND_BOX;
                m_boxLookingFor_type = BoxType.BOXTYPE_HEALTH;
                m_boxLookingFor_pos = closestHPToMe;
            }
        }

        // Step 2: Do we need ammo ?
        if (m_currentState == BehaviorState.BS_IDLE)
        {
            float currentAmmoPercent = m_tankShooting.GetCurrentAmmoPercent();
            float ammoThrehsold = Random.Range(0.1f, 0.3f);
            if (currentAmmoPercent < ammoThrehsold)
            {
                Vector3 closestAmmoToMe = m_actions.FindClosestBoxPosFromPos(BoxType.BOXTYPE_AMMO, thisObjectPos);
                if (closestAmmoToMe != AIBehaviorActions.s_invalidPos)
                {
                    m_currentState = BehaviorState.BS_FIND_BOX;
                    m_boxLookingFor_type = BoxType.BOXTYPE_AMMO;
                    m_boxLookingFor_pos = closestAmmoToMe;
                }
            }
        }

        // Step 3: if we don't need anything, choose whatever you want with equal probability 
        if (m_currentState == BehaviorState.BS_IDLE)
        {
            float rndImprove = Random.Range(0.0f, 1.0f);

            // Improve status ? (like shield or weapon)
            if (rndImprove < 1.0f)  //0.3f)
            {
                // Choose an improvement box
                BoxType[] improvementTypes = { BoxType.BOXTYPE_WEAPONUPGRADE, BoxType.BOXTYPE_SHIELD };
                int rndBox = Random.Range(0, improvementTypes.Length);
                BoxType boxTryingToLookAfter = improvementTypes[rndBox];

                Vector3 closestImproveBoxToMe = m_actions.FindClosestBoxPosFromPos(boxTryingToLookAfter, thisObjectPos);
                if (closestImproveBoxToMe != AIBehaviorActions.s_invalidPos)
                {
                    m_currentState          = BehaviorState.BS_FIND_BOX;
                    m_boxLookingFor_type    = boxTryingToLookAfter;
                    m_boxLookingFor_pos     = closestImproveBoxToMe;
                }
            }

            if (m_currentState == BehaviorState.BS_IDLE) // Either ATTACK or DEFEND
            {
                BehaviorState[] states = { BehaviorState.BS_ATTACK, BehaviorState.BS_DEFEND };
                int rndState = Random.Range(0, states.Length);
                m_currentState = states[rndState];
            }
        }
    }

    void ExecuteCurrentState()
    {
        Transform thisTankTransform = gameObject.transform;
        Debug.Assert(m_currentState != BehaviorState.BS_IDLE);

        bool shouldFire = false;
        //if (m_currentState == BehaviorState.BS_ATTACK || m_currentState == BehaviorState.BS_DEFEND)
        {
            // Step1: Find the closest target that is within shooting range
            TankManager[] tanks = m_globalAIBlackBox.m_TanksRef;
            Transform closestTankTransform_visible = null;  // Stores the transform of the closest visible tank and any tank. By visible we mean shootable
            Transform closestTankTransform_any = null;
            float closestDistanceSqr_visible = Mathf.Infinity;
            float closestDistanceSqr_any = Mathf.Infinity;
            for (int i = 0; i < tanks.Length; i++)
            {
                // Is this not alive
                if (tanks[i].IsAlive() == false)
                    continue;

                // THis is me ? 
                if (tanks[i].m_Instance.gameObject == gameObject)
                    continue;

                GameObject otherTank = tanks[i].m_Instance.gameObject;
                Transform otherTankTransform = otherTank.transform;

                // If this tank is closer and it is visible from a ray cast perspective
                // And if within shooting range
                float distanceToOtherTankSqr = (otherTankTransform.position - thisTankTransform.position).sqrMagnitude;
                if ((distanceToOtherTankSqr < closestDistanceSqr_visible &&
                     distanceToOtherTankSqr < ShellExplosion.m_MaxDistanceTravelledSqr
                    ) &&
                    IsEnemyTankVisible(otherTankTransform, Mathf.Sqrt(distanceToOtherTankSqr))
                    )

                {
                    closestDistanceSqr_visible = distanceToOtherTankSqr;
                    closestTankTransform_visible = otherTankTransform;
                }

                if (distanceToOtherTankSqr < closestDistanceSqr_any)
                {
                    closestDistanceSqr_any = distanceToOtherTankSqr;
                    closestTankTransform_any = otherTankTransform;
                }
            }

            Transform closestTankTransform = null;
            if (closestTankTransform_visible != null)
            {
                closestTankTransform = closestTankTransform_visible;
                shouldFire = true;
            }
            else
            {
                closestTankTransform = closestTankTransform_any;
                shouldFire = false;
            }

            if (closestTankTransform == null)
                return;

            // Step 2: if there is any thank within the range rotate and shoot it
            // If visible, then fire !
            if (shouldFire)
            {
                m_actions.Fire();
            }
            else if (m_currentState == BehaviorState.BS_ATTACK) // IF in attack state => Move to it !
            {
                m_actions.MoveToPosition(closestTankTransform.position);

                if (shouldFire)
                {
                    m_actions.TurnToPosition(closestTankTransform.position);
                }
            }
            else if (m_currentState == BehaviorState.BS_DEFEND) // IF in defending, take cover and rotate / wait for oponent
            {
                // TODO: take cover
                m_actions.TurnToPosition(closestTankTransform.position);
            }
            else if (m_currentState == BehaviorState.BS_FIND_BOX)
            {
                // Just move to position here
                // However there are many things to take into account here like what if someone is attacking us etc.
                Debug.Assert(m_boxLookingFor_type != BoxType.BOXTYPE_NUMS && m_boxLookingFor_pos != AIBehaviorActions.s_invalidPos);
                Debug.DrawRay(thisTankTransform.position, m_boxLookingFor_pos - thisTankTransform.position);
                m_actions.MoveToPosition(m_boxLookingFor_pos);

                if (shouldFire)
                {
                    m_actions.TurnToPosition(closestTankTransform.position);
                }
            }
            else
            {
                Debug.Assert(false);
            }
        }

        if (m_tankUI != null)
        {
            m_tankUI.setDebugText(System.String.Format("Id: {0} S: {1} F: {2}", m_id, m_currentState.ToString(), shouldFire ? 1 : 0));
        }
    }

    // Update is called once per frame
    void Update()
    {
        m_debugInfo.Reset();

        if (!enabled)
            return;

        FillLocalBlackBoard();

        CheckCurrentState();
        ExecuteCurrentState();
    }

    private bool IsEnemyTankVisible(Transform otherTankTransform, float maxDistance)
    {
        bool isOtherTankVisibleFromThis = false;

            RaycastHit hit;
            Ray ray = default(Ray);
            ray.origin = m_fireTransform.position;
            ray.direction = transform.forward;
            int layerMask = ~0;
            if (Physics.Raycast(ray, out hit, maxDistance + 2.0f, layerMask))
            {
                bool isEnemyMask = ((m_EnemyLayerMask & (1 << hit.transform.gameObject.layer))) != 0;
                bool isTargetTheCorrectTank = hit.transform == otherTankTransform;

                isOtherTankVisibleFromThis = isEnemyMask && isTargetTheCorrectTank;
                if (EnableDebugging)
                {
                        m_debugInfo.lastRayHit = hit;
                        m_debugInfo.lastUsedRay = ray;

                        m_debugInfo.lastRayHitTank = isOtherTankVisibleFromThis;
                        }
            }

        return isOtherTankVisibleFromThis;

    }

    private void OnDrawGizmos()
    {
        if (!EnableDebugging)
            return;

        if (m_debugInfo.lastRayHit.transform == null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawRay(m_debugInfo.lastUsedRay);
        }
        else
        {
            Gizmos.color = m_debugInfo.lastRayHitTank ? Color.red : Color.green;
            Gizmos.DrawLine(m_debugInfo.lastUsedRay.origin, m_debugInfo.lastRayHit.point);
        }
    }

    AIBehaviorActions m_Actions;
}


/// <summary>
///  These are the set of actions that the agent can do in general regardless of the behavior
///  The purpose of this is to simplify the client code (the one implementing behaviors in this case)
/// </summary>
public class AIBehaviorActions
{
    public AIBehaviorActions(AIBehavior baseObj) { m_base = baseObj; }
    private AIBehavior m_base;

    static public Vector3 s_invalidPos = new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);

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
        List<Vector3> boxPositions = m_base.m_globalAIBlackBox.m_boxPositionsByType[boxType];
        float closestDist = float.MaxValue;
        Vector3 closestPos = s_invalidPos;
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
        List<Vector3> boxPositions = m_base.m_globalAIBlackBox.m_boxPositionsByType[boxType];
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

        if (closestBoxPos == s_invalidPos)
            return false;

        // TODO: check other enemyies maybe they follow the path to this box or are very close to it.
        // A lot of parameters to vary and learn here
        MoveToPosition(closestBoxPos);
        return true;
    }
};

