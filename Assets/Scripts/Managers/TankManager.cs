using System;
using UnityEngine;

[Serializable]
public class TankManager
{
    // This class is to manage various settings on a tank.
    // It works with the GameManager class to control how the tanks behave
    // and whether or not players have control of their tank in the 
    // different phases of the game.

    [HideInInspector] public Color m_PlayerColor;                             // This is the color this tank will be tinted.
    [HideInInspector]
    public int m_PlayerNumber;            // This specifies which player this the manager for.
    [HideInInspector]
    public string m_ColoredPlayerText;    // A string that represents the player with their number colored to match their tank.
    [HideInInspector]
    public GameObject m_Instance;         // A reference to the instance of the tank when it is created.
    [HideInInspector]
    public int m_Wins;                    // The number of wins this player has so far.

    [HideInInspector]
    public Transform m_SpawnPoint;

    [HideInInspector]
    public TankMovement m_Movement;                        // Reference to tank's movement script, used to disable and enable control.
    [HideInInspector]
    public TankShooting m_Shooting;                        // Reference to tank's shooting script, used to disable and enable control.
    [HideInInspector]
    public TankHealth m_Health;
    [HideInInspector]
    public AIBehavior m_AI;
    [HideInInspector]
    public GameObject m_CanvasGameObject;                  // Used to disable the world space UI during the Starting and Ending phases of each round.
    [HideInInspector]
    public BoxAddonBehavior m_Addons;

    [HideInInspector]
    public GlobalAIBlackBox m_AIGlobalBlackBox;

    TankUI m_tankUI;

    public void Setup(bool enableTextUI)
    {
        // Get references to the components.
        m_Movement = m_Instance.GetComponent<TankMovement>();
        m_Shooting = m_Instance.GetComponent<TankShooting>();
        m_Health = m_Instance.GetComponent<TankHealth>();
        m_Addons = m_Instance.GetComponent<BoxAddonBehavior>();
        m_CanvasGameObject = m_Instance.GetComponentInChildren<Canvas>().gameObject;


        m_Health.m_respawnFunc = new TankHealth.GetRespawnPoint(m_AIGlobalBlackBox.GetBestSpawnPointForRespawn);

        if (m_AI.enabled)
        {
            m_Movement.SetAsAI();
            m_Shooting.SetAsAI();
        }

        // Set the player numbers to be consistent across the scripts.
        m_Movement.m_PlayerNumber = m_PlayerNumber;
        m_Shooting.m_PlayerNumber = m_PlayerNumber;

        // Create a string using the correct color that says 'PLAYER 1' etc based on the tank's color and the player's number.
        m_ColoredPlayerText = "<color=#" + ColorUtility.ToHtmlStringRGB(m_PlayerColor) + ">PLAYER " + m_PlayerNumber + "</color>";

        // Get all of the renderers of the tank.
        MeshRenderer[] renderers = m_Instance.GetComponentsInChildren<MeshRenderer>();

        // Go through all the renderers...
        for (int i = 0; i < renderers.Length; i++)
        {
            // ... set their material color to the color specific to this tank.
            renderers[i].material.color = m_PlayerColor;
        }

        m_tankUI = new TankUI();

        m_AI.m_tankUI = m_tankUI;
        if (enableTextUI)
        {
            m_tankUI.Setup(this);
        }
    }

    public void SetPlayerAsHuman(int humanId)
    {
        m_PlayerNumber = humanId;
        m_AI = m_Instance.GetComponent<AIBehavior>();
        m_AI.enabled = false;
    }

    public void SetPlayerAsAI(int id, GlobalAIBlackBox aiGlobalBlackBox)
    {
        m_AI = m_Instance.GetComponent<AIBehavior>();
        m_AI.enabled = true;
        m_AI.SetGlobalBlackbox(aiGlobalBlackBox);
        m_PlayerNumber = id;
        m_AI.m_id = id;
    }


    // Used during the phases of the game where the player shouldn't be able to control their tank.
    public void DisableControl()
    {
        m_Movement.enabled = false;
        m_Shooting.enabled = false;

        m_CanvasGameObject.SetActive(false);
    }


    // Used during the phases of the game where the player should be able to control their tank.
    public void EnableControl()
    {
        m_Movement.enabled = true;
        m_Shooting.enabled = true;

        m_CanvasGameObject.SetActive(true);
    }


    // Used at the start of each round to put the tank into it's default state.
    public void Reset()
    {
        m_Instance.transform.position = m_SpawnPoint.position;
        m_Instance.transform.rotation = m_SpawnPoint.rotation;

        m_Movement.ResetComp();
        m_Shooting.ResetComp();
        m_Health.ResetComp();
        m_Addons.ResetComp();
        m_tankUI.Reset();

        m_Instance.SetActive(false);
        m_Instance.SetActive(true);
    }

    public bool IsAlive()
    {
        return m_Instance.gameObject.activeInHierarchy; //m_Health.IsAlive();
    }

    public void Update()
    {
        if (m_tankUI != null)
        {
            m_tankUI.Update();
        }
    }
}
