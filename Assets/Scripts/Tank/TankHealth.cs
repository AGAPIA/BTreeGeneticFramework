using UnityEngine;
using UnityEngine.UI;


public class TankHealth : MonoBehaviour
{
    public float m_StartingHealth = 100f;               // The amount of health each tank starts with.
    public Slider m_Slider;                             // The slider to represent how much health the tank currently has.
    public Image m_FillImage;                           // The image component of the slider.
    public Color m_FullHealthColor = Color.green;       // The color the health bar will be when on full health.
    public Color m_ZeroHealthColor = Color.red;         // The color the health bar will be when on no health.
    public GameObject m_ExplosionPrefab;                // A prefab that will be instantiated in Awake, then used whenever the tank dies.
    public int m_InitialNumLives = 3;


    private AudioSource m_ExplosionAudio;               // The audio source to play when the tank explodes.
    private ParticleSystem m_ExplosionParticles;        // The particle system the will play when the tank is destroyed.
    private float m_CurrentHealth;                      // How much health the tank currently has.
    private bool m_Dead;                                // Has the tank been reduced beyond zero health yet?
    private int m_CurrentRemainingLives;

    [HideInInspector]
    public delegate Transform GetRespawnPoint(GameObject tankInstance);

    [HideInInspector]
    public GetRespawnPoint m_respawnFunc;

    BoxAddonBehavior m_tankAddons;

    private void Awake()
    {
        // Instantiate the explosion prefab and get a reference to the particle system on it.
        m_ExplosionParticles = Instantiate(m_ExplosionPrefab).GetComponent<ParticleSystem>();

        // Get a reference to the audio source on the instantiated prefab.
        m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource>();

        // Disable the prefab so it can be activated when it's required.
        m_ExplosionParticles.gameObject.SetActive(false);
    }

    private void Start()
    {
        m_tankAddons = gameObject.GetComponent<BoxAddonBehavior>();
    }


    private void OnEnable()
    {
        ResetComp();
    }

    public void ResetComp()
    {
        // When the tank is enabled, reset the tank's health and whether or not it's dead.
        m_CurrentHealth = m_StartingHealth;
        m_Dead = false;
        m_CurrentRemainingLives = m_InitialNumLives;

        // Update the health slider's value and color.
        SetHealthUI();
    }

    public int GetCurrentNumLives() { return m_CurrentRemainingLives; }


    public void TakeDamage(float amount)
    {
        // Reduce current health by the amount of damage done.
        m_CurrentHealth -= amount;

        // Change the UI elements appropriately.
        SetHealthUI();

        // If the current health is at or below zero and it has not yet been registered, call OnDeath.
        if (m_CurrentHealth <= 0f && !m_Dead)
        {
            OnDeath();
        }
    }


    private void SetHealthUI()
    {
        // Set the slider's value appropriately.
        if (m_Slider)
            m_Slider.value = m_CurrentHealth;

        // Interpolate the color of the bar between the choosen colours based on the current percentage of the starting health.
        if (m_FillImage)
            m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, GetRemainingLifePercent());
    }

    public void MaximizeLife()
    {
        m_CurrentHealth = m_StartingHealth;
        SetHealthUI();
    }

    public void AddLife()
    {
        m_CurrentRemainingLives++;
    }

    public int GetRemainingHealths() { return m_CurrentRemainingLives; }
    public float GetRemainingLifePercent() { return m_CurrentHealth / m_StartingHealth; }

    private void OnDeath()
    {
        //Step 1 - play the effects
        //-----------
        // Move the instantiated explosion prefab to the tank's position and turn it on.
        m_ExplosionParticles.transform.position = transform.position;
        m_ExplosionParticles.gameObject.SetActive(true);

        // Play the particle system of the tank exploding.
        m_ExplosionParticles.Play();

        // Play the tank explosion sound effect.
        m_ExplosionAudio.Play();


        // Step 2 - update lives remaining and set dead is needed
        m_CurrentRemainingLives--;

        if (m_CurrentRemainingLives <= 0)
        {
            // Set the flag so that this function is only called once.
            m_Dead = true;

            // Turn the tank off.
            gameObject.SetActive(false);
        }
        else
        {
            // Respawn
            Transform respawnPos = m_respawnFunc(gameObject);
            transform.position = respawnPos.position;
            transform.rotation = respawnPos.rotation;
        }
    }

    public bool IsAlive()
    {
        return m_CurrentHealth > 0;
    }
}
