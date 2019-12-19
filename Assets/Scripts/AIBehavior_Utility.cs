using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// For upgrade evaluation of different type of boxes
public class BoxTypeEval
{
    // Observe that we put more variables here. These are to debug fast where the problem could be by visual inspecting
    public float needForBox;
    public float probabilityToGetBox;
    public float score; // The score = needness for this type of box

    public Vector3 pos; // The position of the easiest box that can be obtained by the agent

    public BoxTypeEval() { Reset(); }
    public void Reset() { score = -1.0f; pos = UtilsGeneral.INVALID_POS; }
};

// This class scores possible actions/subasctions and choose the best one (i.e. highest valued)
// The score is normalized [0-1].
// Also each node decision is modeled by three factors: need for the thing, chance/probability of achieving the thing, personality for the thing. Usually we multiply all 3 factors.
public class AIBehavior_Utility : AIBehavior
{
    // Evaluated data structures for utility AI results
    //------------------------
    BoxTypeEval[] m_evaluationByType = new BoxTypeEval[(int)BoxType.BOXTYPE_NUMS];
    public BoxTypeEval GetBoxTypeEval(BoxType boxType)
    {
        return m_evaluationByType[(int)boxType];
    }
    //------------------------


    public void DoScoreBoxUpgrades(out float score, out BoxType outBoxTypeToLookAfter, out Vector3 outBoxPosToLookAfter)
	{
        score                   = 0.0f;
        outBoxTypeToLookAfter   = BoxType.BOXTYPE_NUMS;
        outBoxPosToLookAfter    = UtilsGeneral.INVALID_POS;

        for (int i = 0; i < (int) m_evaluationByType.Length; i++)
        {
            if (m_evaluationByType[i] == null)
                m_evaluationByType[i] = new BoxTypeEval();

            m_evaluationByType[i].Reset();
        }

        Vector3 parentAgentPos = gameObject.transform.position; // The position of the parent agent


        // Step 1.1: Utility of life boxes and chances to get there. Chances for each are the third lottery level in the utility tree
        {
            // Score current status vs needs
            int numLivesLeft                    = m_tankHealth.GetRemainingHealths();
            float remainingCurrentLifePercent   = m_tankHealth.GetRemainingLifePercent();

            int numMaxLives = m_tankHealth.m_InitialNumLives;
            float needForLife = 1.0f - (numMaxLives + 1.0f + (remainingCurrentLifePercent - 1.0f)) / (numMaxLives + 1);


            BoxTypeEval evalHealth = m_evaluationByType[(int)BoxType.BOXTYPE_HEALTH];

            Vector3 bestHealthBoxPos;// = default(Vector3);
            float bestHealthBoxSuccessProbability = 0.0f;
            m_actions.FindEasiestBoxOfType(BoxType.BOXTYPE_HEALTH, m_globalAIBlackBox, out bestHealthBoxPos, out bestHealthBoxSuccessProbability);

            // First order benefit: value of action * probability of success
            evalHealth.needForBox           = needForLife;
            evalHealth.probabilityToGetBox  = bestHealthBoxSuccessProbability;
            evalHealth.score                = needForLife * bestHealthBoxSuccessProbability;
            evalHealth.pos                  = bestHealthBoxPos;
        }

        /*
        Vector3 bestBoxPos = 


        int numLivesThreshold = Random.Range(1, Mathf.CeilToInt(m_tankHealth.m_InitialNumLives * 0.5f));
        float healthPercentThreshold = Random.Range(0.0f, 1.0f);
        if ( <= numLivesThreshold &&
        {
            Vector3 closestHPToMe = m_actions.FindClosestBoxPosFromPos(BoxType.BOXTYPE_HEALTH, thisObjectPos);
            if (closestHPToMe != UtilsGeneral.INVALID_POS)
            {
                m_localAIBlackBox.m_currentState = AIBehaviorState.BS_FIND_BOX;
                m_localAIBlackBox.m_boxLookingFor_type = BoxType.BOXTYPE_HEALTH;
                m_localAIBlackBox.m_boxLookingFor_pos = closestHPToMe;
            }
        }

        // // Step 1.2: Utility of ammo boxes and chances to get there
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

        // Step 1.3: Score to upgrade our ammo or shield
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
        */
    }

    // This executes the chosen action
    public void ExecuteCurrentAction()
    {
        Transform thisTankTransform = gameObject.transform;
        //Debug.Assert(m_localAIBlackBox.m_currentState != AIBehaviorState.BS_IDLE);

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
                Debug.Assert(m_localAIBlackBox.m_currentState == AIBehaviorState.BS_IDLE, "State is not idle but it should be in this case ");
            }
        }
    }

    // This function chooses the best action to take using utility theory AI
    public void ChooseBestAction()
    {
        // Score each possible action and set the best one on the local ai blackbox:
        // --------------------------------------
        // First layer: DEFEND, ATTACK, LOOK for UPGRADE BOX

        float bestScoreSoFar = 0.0f;
        m_localAIBlackBox.m_currentState = AIBehaviorState.BS_IDLE;

        // 1. Score box upgrade depending on need and success probability then give a score of this action and some context
        {
            float scoreForBoxes = 0.0f;
            BoxType boxTypeToLookAfter = BoxType.BOXTYPE_NUMS;
            Vector3 boxPosToLookAfter = UtilsGeneral.INVALID_POS;
            DoScoreBoxUpgrades(out scoreForBoxes, out boxTypeToLookAfter, out boxPosToLookAfter);

            if (scoreForBoxes > bestScoreSoFar)
            {
                m_localAIBlackBox.m_currentState        = AIBehaviorState.BS_FIND_BOX;
                m_localAIBlackBox.m_boxLookingFor_pos   = boxPosToLookAfter;
                m_localAIBlackBox.m_boxLookingFor_type  = boxTypeToLookAfter;
                bestScoreSoFar                          = scoreForBoxes;
            }
        }

        // 2. Score Defend
        {

        }

        // 3. Score Attack
        {

        }
    }

    public override void Execute()
	{
        // Get the best score and apply the command
        ChooseBestAction();

        // Execute the choosen action
        ExecuteCurrentAction();
	}
}
