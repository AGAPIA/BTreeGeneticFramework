using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Dummy behavior class. All will be derived from this maybe ?
public class AIBehavior : MonoBehaviour
{
    public GlobalAIBlackBox m_globalAIBlackBox;
    public LocalAIBlackBoard m_localAIBlackBox;

    // Gathers informations for the local AI blackboard
    void FillLocalBlackBoard()
    {
        m_localAIBlackBox.deltaTime = Time.deltaTime;
    }


    public bool EnableDebugging = true;

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
    public Transform m_fireTransform;
    public Rigidbody m_rigidBody;
    public DebugInfo m_debugInfo = new DebugInfo();
    public NavMeshAgent m_navMeshAgent;

    public LayerMask m_EnemyLayerMask;

    AIBehaviorActions m_actions;

    // Start is called before the first frame update
    void Start()
    {
        //m_tankRigidBody = gameObject.GetComponent<Rigidbody>();
        m_localAIBlackBox = new LocalAIBlackBoard();

        m_tankMovement  = gameObject.GetComponent<TankMovement>();
        m_tankShooting  = gameObject.GetComponent<TankShooting>();
        m_fireTransform = gameObject.transform.Find("FireTransform");
        m_rigidBody     = gameObject.GetComponent<Rigidbody>();
        m_navMeshAgent  = GetComponent<NavMeshAgent>();

        //m_navMeshAgent.speed = m_tankMovement.m_Speed * GameManager.m_SpeedMultiplier;
        //m_navMeshAgent.angularSpeed = m_tankMovement.m_TurnSpeed * GameManager.m_SpeedMultiplier;
        m_actions       = new AIBehaviorActions(this);
    }

    // Update is called once per frame
    void Update()
    {
        if (!enabled)
            return;


        m_debugInfo.Reset();
        FillLocalBlackBoard();

        Transform thisTankTransform = gameObject.transform;

        // Step1: Find the closest target that is within shooting range  and rotate to it
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

        bool shouldFire = false;
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
        // TODO: variations here: choose the tank which we are already shooting against since it has lower health and we can get more
        // TODO: if being shoot and low health - HIDE & search for stuff to heal
        // TODO: search for items like shield, weapon upgrade if close
        // TODO: thresholds values


        // If visible, then fire !
        if (shouldFire)
        {
            m_actions.Fire();
        }
        else // Else move to it.
        {
            m_actions.MoveToPosition(closestTankTransform.position);
        }

        m_actions.TurnToPosition(closestTankTransform.position);
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
};

