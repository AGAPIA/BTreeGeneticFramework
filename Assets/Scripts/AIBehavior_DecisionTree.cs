using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Classic decision -tree like behavior
public class AIBehavior_DecisionTree : AIBehavior
{

    protected float m_targetTimeInCurrentAction = 0.0f; // How much should we spent in this action

    public override void Execute()
    {
        CheckCurrentState();
        ExecuteCurrentState();
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
        m_localAIBlackBox.m_timeInCurrentAction += Time.deltaTime;
        bool isTargetTimePast = m_localAIBlackBox.m_timeInCurrentAction > m_targetTimeInCurrentAction;
        bool isLookingForBoxAndItWasTaken = m_localAIBlackBox.m_currentState == AIBehaviorState.BS_FIND_BOX && m_actions.IsBoxTypeAndPosAvailable(m_localAIBlackBox.m_boxLookingFor_type, m_localAIBlackBox.m_boxLookingFor_pos) == false;

        // Time to reset and change state ?
        if (isTargetTimePast || isLookingForBoxAndItWasTaken)
        {
            // Choose a new target action and time
            m_targetTimeInCurrentAction = Random.Range(m_minTimeToContinueSameAction, m_maxTimeToContinueSameAction);
            m_localAIBlackBox.m_timeInCurrentAction = 0.0f;
            m_localAIBlackBox.m_currentState = AIBehaviorState.BS_IDLE;
            m_localAIBlackBox.m_boxLookingFor_type = BoxType.BOXTYPE_NUMS;
            m_localAIBlackBox.m_boxLookingFor_pos = UtilsGeneral.INVALID_POS;
        }

        // Nothing changed continue using the same action
        if (m_localAIBlackBox.m_currentState != AIBehaviorState.BS_IDLE)
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
            if (closestHPToMe != UtilsGeneral.INVALID_POS)
            {
                m_localAIBlackBox.m_currentState = AIBehaviorState.BS_FIND_BOX;
                m_localAIBlackBox.m_boxLookingFor_type = BoxType.BOXTYPE_HEALTH;
                m_localAIBlackBox.m_boxLookingFor_pos = closestHPToMe;
            }
        }

        // Step 2: Do we need ammo ?
        if (m_localAIBlackBox.m_currentState == AIBehaviorState.BS_IDLE)
        {
            float currentAmmoPercent = m_tankShooting.GetCurrentAmmoPercent();
            float ammoThrehsold = Random.Range(0.1f, 0.3f);
            if (currentAmmoPercent < ammoThrehsold)
            {
                Vector3 closestAmmoToMe = m_actions.FindClosestBoxPosFromPos(BoxType.BOXTYPE_AMMO, thisObjectPos);
                if (closestAmmoToMe != UtilsGeneral.INVALID_POS)
                {
                    m_localAIBlackBox.m_currentState = AIBehaviorState.BS_FIND_BOX;
                    m_localAIBlackBox.m_boxLookingFor_type = BoxType.BOXTYPE_AMMO;
                    m_localAIBlackBox.m_boxLookingFor_pos = closestAmmoToMe;
                }
            }
        }

        // Step 3: if we don't need anything, choose whatever you want with equal probability 
        if (m_localAIBlackBox.m_currentState == AIBehaviorState.BS_IDLE)
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
                if (closestImproveBoxToMe != UtilsGeneral.INVALID_POS)
                {
                    m_localAIBlackBox.m_currentState = AIBehaviorState.BS_FIND_BOX;
                    m_localAIBlackBox.m_boxLookingFor_type = boxTryingToLookAfter;
                    m_localAIBlackBox.m_boxLookingFor_pos = closestImproveBoxToMe;
                }
            }

            if (m_localAIBlackBox.m_currentState == AIBehaviorState.BS_IDLE) // Either ATTACK or DEFEND
            {
                AIBehaviorState[] states = { AIBehaviorState.BS_ATTACK, AIBehaviorState.BS_DEFEND };
                int rndState = Random.Range(0, states.Length);
                m_localAIBlackBox.m_currentState = states[rndState];
            }
        }
    }

    void ExecuteCurrentState()
    {
        Transform thisTankTransform = gameObject.transform;
        Debug.Assert(m_localAIBlackBox.m_currentState != AIBehaviorState.BS_IDLE);

        m_iminentActions.reset();

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
                    m_actions.IsEnemyTankInShootingDirection(otherTankTransform, Mathf.Sqrt(distanceToOtherTankSqr))
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
                m_iminentActions.m_fire = true;
            }
            else
            {
                closestTankTransform = closestTankTransform_any;
                m_iminentActions.m_fire = false;
            }

            if (closestTankTransform == null)
                return;

            // Step 2: if there is any thank within the range rotate and shoot it
            // If visible, then fire !
            if (m_iminentActions.m_fire)
            {
                m_actions.Fire();
            }
            else if (m_localAIBlackBox.m_currentState == AIBehaviorState.BS_ATTACK) // IF in attack state => Move to it !
            {
                m_actions.MoveToPosition(closestTankTransform.position);

                if (m_iminentActions.m_fire)
                {
                    m_actions.TurnToPosition(closestTankTransform.position);
                }
            }
            else if (m_localAIBlackBox.m_currentState == AIBehaviorState.BS_DEFEND) // IF in defending, take cover and rotate / wait for oponent
            {
                // TODO: take cover
                m_actions.TurnToPosition(closestTankTransform.position);
            }
            else if (m_localAIBlackBox.m_currentState == AIBehaviorState.BS_FIND_BOX)
            {
                // Just move to position here
                // However there are many things to take into account here like what if someone is attacking us etc.
                Debug.Assert(m_localAIBlackBox.m_boxLookingFor_type != BoxType.BOXTYPE_NUMS && m_localAIBlackBox.m_boxLookingFor_pos != UtilsGeneral.INVALID_POS);
                Debug.DrawRay(thisTankTransform.position, m_localAIBlackBox.m_boxLookingFor_pos - thisTankTransform.position);
                m_actions.MoveToPosition(m_localAIBlackBox.m_boxLookingFor_pos);

                if (m_iminentActions.m_fire)
                {
                    m_actions.TurnToPosition(closestTankTransform.position);
                }
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }
};