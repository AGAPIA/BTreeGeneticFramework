using UnityEngine;
using UnityEditor;

public class TankMovement : MonoBehaviour
{
    //TODO: implement ml-agents
    public int m_PlayerNumber = 1;              // Used to identify which tank belongs to which player.  This is set by this tank's manager.
    public static float m_Speed = 12f;                 // How fast the tank moves forward and back.
    public float m_TurnSpeed = 180f;            // How fast the tank turns in degrees per second.
    public AudioSource m_MovementAudio;         // Reference to the audio source used to play engine sounds. NB: different to the shooting audio source.
    public AudioClip m_EngineIdling;            // Audio to play when the tank isn't moving.
    public AudioClip m_EngineDriving;           // Audio to play when the tank is moving.
    public float m_PitchRange = 0.2f;           // The amount by which the pitch of the engine noises can vary.

    public Vector3 m_movingAvgVel = Vector3.zero;              // Moving average velocity for this agent


    private string m_MovementAxisName;          // The name of the input axis for moving forward and back.
    private string m_TurnAxisName;              // The name of the input axis for turning.
    private Rigidbody m_Rigidbody;              // Reference used to move the tank.
    private float m_MovementInputValue;         // The current value of the movement input.
    private float m_TurnInputValue;             // The current value of the turn input.
    private float m_OriginalPitch;              // The pitch of the audio source at the start of the scene.
    private int m_numFramesAlive;               // The number of frames that the agent is alive
    bool m_IsAI = false;
    public void SetAsAI() { m_IsAI = true; }

    Vector3 m_lastMove;

    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
    }


    private void OnEnable()
    {
        ResetComp();
    }

    public void ResetComp()
    {
        // When the tank is turned on, make sure it's not kinematic.
        if (m_Rigidbody)
            m_Rigidbody.isKinematic = false;

        // Also reset the input values.
        m_MovementInputValue = 0f;
        m_TurnInputValue = 0f;
        m_numFramesAlive++;
        m_movingAvgVel = Vector3.zero;
    }


    private void OnDisable()
    {
        // When the tank is turned off, set it to kinematic so it stops moving.
        m_Rigidbody.isKinematic = true;
    }


    private void Start()
    {
        // The axes names are based on player number.
        m_MovementAxisName = "Vertical" + m_PlayerNumber;
        m_TurnAxisName = "Horizontal" + m_PlayerNumber;

        // Store the original pitch of the audio source.
        m_OriginalPitch = m_MovementAudio.pitch;
    }


    private void Update()
    {
        if (m_IsAI)
        {

        }
        else // Human
        {
            // Store the value of both input axes.
            m_MovementInputValue = Input.GetAxis(m_MovementAxisName);
            m_TurnInputValue = Input.GetAxis(m_TurnAxisName);
        }

        float alpha = UtilsGeneral.lerp(m_numFramesAlive, 0.0f, 9.0f, 0.0f, 0.95f); // In the beggining, take into account more the instant vel rather than history
        m_movingAvgVel = m_IsAI ? (gameObject.transform.forward * m_Speed) : (m_lastMove * alpha + (1.0f - alpha) * m_lastMove);

        EngineAudio();
        m_numFramesAlive++;

        //Debug.DrawRay(transform.position, m_movingAvgVel*10.0f);

    }

    void OnDrawGizmos()
    {
        /*
        string vel = m_Rigidbody.velocity.ToString();
        string velString = m_lastMove.ToString();
        Handles.Label(transform.position, velString);
        */
    }

    /*
    public int health = 17;
    private int[] healthUp = new int[] { 25, 10, 5, 1 };
    private int[] healthDown = new int[] { -10, -5, -2, -1 };

    // Width and height for the buttons.
    private int xButton = 75;
    private int yButton = 50;

    // Place of the top left button.
    private int xPos1 = 50, yPos1 = 100;
    private int xPos2 = 125, yPos2 = 100;

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 100, 20), "Hello World!");
        GUI.skin.label.fontSize = 20;
        GUI.skin.button.fontSize = 20;

        // Generate and show positive buttons.
        for (int i = 0; i < healthUp.Length; i++)
        {
            if (GUI.Button(new Rect(xPos1, yPos1 + i * yButton, xButton, yButton), healthUp[i].ToString()))
            {
                health += healthUp[i];
            }
        }

        // Generate and show negative buttons.
        for (int i = 0; i < healthDown.Length; i++)
        {
            if (GUI.Button(new Rect(xPos2, yPos2 + i * yButton, xButton, yButton), healthDown[i].ToString()))
            {
                health += healthDown[i];
            }
        }

        // Show health between 1 and 100.
        health = Mathf.Clamp(health, 1, 100);
        GUI.Label(new Rect(xPos1, xPos1, 2 * xButton, yButton), "Health: " + health.ToString("D3"));
    }
    */

    private void EngineAudio()
    {
        // If there is no input (the tank is stationary)...
        if (Mathf.Abs(m_MovementInputValue) < 0.1f && Mathf.Abs(m_TurnInputValue) < 0.1f)
        {
            // ... and if the audio source is currently playing the driving clip...
            if (m_MovementAudio.clip == m_EngineDriving)
            {
                // ... change the clip to idling and play it.
                m_MovementAudio.clip = m_EngineIdling;
                m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                m_MovementAudio.Play();
            }
        }
        else
        {
            // Otherwise if the tank is moving and if the idling clip is currently playing...
            if (m_MovementAudio.clip == m_EngineIdling)
            {
                // ... change the clip to driving and play.
                m_MovementAudio.clip = m_EngineDriving;
                m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                m_MovementAudio.Play();
            }
        }
    }


    private void FixedUpdate()
    {
        // Adjust the rigidbodies position and orientation in FixedUpdate.
        Move();
        Turn();
    }


    private void Move()
    {
        // Create a vector in the direction the tank is facing with a magnitude based on the input, speed and the time between frames.
        Vector3 movement = transform.forward * m_MovementInputValue * m_Speed * Time.deltaTime * GameManager.m_SpeedMultiplier;

        m_lastMove = movement;

        // Apply this movement to the rigidbody's position.
        m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
    }

    //https://docs.unity3d.com/ScriptReference/Transform-eulerAngles.html

    private void Turn()
    {
        // Determine the number of degrees to be turned based on the input, speed and time between frames.
        float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime * GameManager.m_SpeedMultiplier;

        // Make this into a rotation in the y axis.
        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);

        // Apply this rotation to the rigidbody's rotation.
        m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
    }

    public void TurnToTarget(Vector3 target, float deltaTime)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0;

        float angleDiff = Vector3.SignedAngle(gameObject.transform.forward, dir, Vector3.up);
        float maxAngleDiffToApply = m_TurnSpeed * deltaTime;

        float angleSign = Mathf.Sign(angleDiff);
        float angleToApply = Mathf.Min(Mathf.Abs(angleDiff), maxAngleDiffToApply) * angleSign;

        Quaternion turnRotation = Quaternion.Euler(0, angleToApply, 0);
        m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
    }
}
