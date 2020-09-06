using System.Collections;
using System.Collections.Generic;
using System;
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

    [Serializable]
    public class Params
    {
        // These are parameters for box finding
        //---------
        [Tooltip("How many closest boxes around agent to look for")]
        public const int m_bfClosestBoxNumbersLookingFor = 3;
        [Tooltip("How many closest opponents around to look for when evaluating a box")]
        public const int m_bfClosestOpponentsLookingFor = 3;
        [Tooltip("How important is time ratio vs intention (see paper)")]
        public const float m_boxAlphaTime = 0.75f; // How important is the time ratio
        [Tooltip("Interpolation factors to give ")]
        public const float m_angleX1 = 30, m_angleX2 = 180, m_angleY1 = 0.4f, m_angleY2 = 0.0f; // Interpolation direction_to_box inputs (angles) and probability output. Reflects belief of agent going to the target with his current direction
        //---------


        // These are parameters for defense
        //---------
        [Tooltip("How many closest opponents around to look for when evaluating a box")]
        public const int m_defClosestOpponentsLookingFor = 3;
        // Need for life interp params
        public const float m_lifeX1 = 0.0f; // TODO: add all other paramerters here.
        //
        //---------

        // These are parameters for attack
        //---------

        //---------
    }


    // 
    [Tooltip("Score for IDLE action. If no better score is found by utility AI, idle will be selected")]
    public static float BASE_IDLE_SCORE = 0.001f;

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
            float needForLife = Mathf.Max(1.0f - (numMaxLives + 1.0f + (remainingCurrentLifePercent - 1.0f)) / (numMaxLives + 1), 0.001f);


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

        // Now choose he best box available by score
        score = 0.0f;
        outBoxTypeToLookAfter = BoxType.BOXTYPE_NUMS;
        outBoxPosToLookAfter = UtilsGeneral.INVALID_POS;

        for (int i = 0; i < (int)m_evaluationByType.Length; i++)
        {
            if (m_evaluationByType[i].score > score)
            {
                score                   = m_evaluationByType[i].score;
                outBoxPosToLookAfter    = m_evaluationByType[i].pos;
                outBoxTypeToLookAfter   = (BoxType)i;
            }
        }
    }

    public void DoScoreDefend(out float scoreForDefend, out bool isCoverPreferred, out Vector3 coverPosPreferred)
    {
        scoreForDefend      = UtilsGeneral.MIN_SCORE_VALUE;
        isCoverPreferred    = false;
        coverPosPreferred   = Vector3.zero;

        // TODO: do cover scores. Currently just the second branch of the tree is implemented for defense
        // Proposed idea:
        // Find cover positions either automaticlaly by sampling the points around closest enemies which are on navmesh and no ray is visible to enemies.
        // Compute the probabiliy of success as: how long is the agent exposed to any of the closest enemies while he gets in the chosen cover spot.
        //////
        ///


    }

    public void DoScoreAttack(out float scoreForAttack)
    {
        //TODO: this is just a dummy think, need to take into account the personalities of the player etc...

        // How long idle should be a personality maybe...
        bool wasIdleForTooLong = m_localAIBlackBox.m_currentState == AIBehaviorState.BS_IDLE && m_localAIBlackBox.m_timeInCurrentAction >= 0.1;
        bool prevWasIdleAnyway = m_localAIBlackBox.m_prevState == AIBehaviorState.BS_IDLE;

        scoreForAttack = AIBehavior_Utility.BASE_IDLE_SCORE;
        if (wasIdleForTooLong || prevWasIdleAnyway)
        {
            scoreForAttack += 0.001f;
        }
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

        float bestScoreSoFar                = AIBehavior_Utility.BASE_IDLE_SCORE;
        

        // 1. Score box upgrade depending on need and success probability then give a score of this action and some context
        {
            float scoreForBoxes             = 0.0f;
            BoxType boxTypeToLookAfter      = BoxType.BOXTYPE_NUMS;
            Vector3 boxPosToLookAfter       = UtilsGeneral.INVALID_POS;
            DoScoreBoxUpgrades(out scoreForBoxes, out boxTypeToLookAfter, out boxPosToLookAfter);

            if (scoreForBoxes > bestScoreSoFar)
            {
                m_localAIBlackBox.setNewCurrentState(AIBehaviorState.BS_FIND_BOX); // TODO: or do it by cover !!
                m_localAIBlackBox.m_boxLookingFor_pos   = boxPosToLookAfter;
                m_localAIBlackBox.m_boxLookingFor_type  = boxTypeToLookAfter;

                bestScoreSoFar                          = scoreForBoxes;
            }
        }

        // 2. Score Defend
        {
            float scoreForDefend            = 0.0f;
            bool isCoverPreferred           = false;
            Vector3 coverPosPreferred       = Vector3.zero; // set only if above is true
            DoScoreDefend(out scoreForDefend, out isCoverPreferred, out coverPosPreferred);

            if (scoreForDefend > bestScoreSoFar)
            {
                // TODO other parameters...
                m_localAIBlackBox.setNewCurrentState(AIBehaviorState.BS_DEFEND); // TODO: or do it by cover !!
                bestScoreSoFar = scoreForDefend;
            }
        }

        // 3. Score Attack
        {
            float scoreForAttack            = 0.0f;
            DoScoreAttack(out scoreForAttack);

            if (scoreForAttack > bestScoreSoFar)
            {
                m_localAIBlackBox.setNewCurrentState(AIBehaviorState.BS_ATTACK); // TODO: or do it by cover !!
                bestScoreSoFar = scoreForAttack;
            }
        }

        // Default to idle if no good action to take. At least, conserve the energy
        if (bestScoreSoFar <= AIBehavior_Utility.BASE_IDLE_SCORE)
        {
            m_localAIBlackBox.setNewCurrentState(AIBehaviorState.BS_IDLE);
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
