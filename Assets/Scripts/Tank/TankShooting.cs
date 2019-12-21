﻿using UnityEngine;
using UnityEngine.UI;


public class TankShooting : MonoBehaviour
{
    public int m_PlayerNumber = 1;              // Used to identify the different players.
    public Rigidbody m_Shell;                   // Prefab of the shell.
    public Transform m_FireTransform;           // A child of the tank where the shells are spawned.
    public Slider m_AimSlider;                  // A child of the tank that displays the current launch force.
    public AudioSource m_ShootingAudio;         // Reference to the audio source used to play the shooting audio. NB: different to the movement audio source.
    public AudioClip m_ChargingClip;            // Audio that plays when each shot is charging up.
    public AudioClip m_FireClip;                // Audio that plays when each shot is fired.
    public float m_MinLaunchForce = 15f;        // The force given to the shell if the fire button is not held.
    public float m_MaxLaunchForce = 30f;        // The force given to the shell if the fire button is held for the max charge time.
    public float m_MaxChargeTime = 0.75f;       // How long the shell can charge for before it is fired at max force.


    private string m_FireButton;                // The input axis that is used for launching shells.
    private float m_CurrentLaunchForce;         // The force that will be given to the shell when the fire button is released.
    private float m_ChargeSpeed;                // How fast the launch force increases, based on the max charge time.
    private bool m_Fired;                       // Whether or not the shell has been launched with this button press.

    public int m_maxAmmo;
    private int m_currentAmmo;
    bool m_IsAI = false;
    private BoxAddonBehavior m_tankAddons;

    public float m_cooldownTime = 0.4f;  // THe time between two consecutive shootings
    private float m_lastFireTime = -1000;

    public void SetAsAI() { m_IsAI = true; }

    void initValues()
    {
        // When the tank is turned on, reset the launch force and the UI
        m_CurrentLaunchForce = m_MinLaunchForce;
        m_AimSlider.value = m_MinLaunchForce;

        MaximizeAmmo();
        m_tankAddons = gameObject.GetComponent<BoxAddonBehavior>();

        // The fire axis is based on the player number.
        m_FireButton = "Fire" + m_PlayerNumber;

        // The rate that the launch force charges up is the range of possible forces by the max charge time.
        m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
    }

    private void OnEnable()
    {
        initValues();
    }

    private void Start()
    {
        initValues();
    }

    public void ResetComp()
    {
        initValues();
    }


    private void Update()
    {
        if (m_IsAI == false)
        {
            // Check Fire logic only if we have ammo..
            // The slider should have a default value of the minimum launch force.
            m_AimSlider.value = m_MinLaunchForce;

            if (m_currentAmmo > 0 && (Time.time - m_lastFireTime > m_cooldownTime))
            {
                // If the max force has been exceeded and the shell hasn't yet been launched...
                if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
                {
                    // ... use the max force and launch the shell.
                    m_CurrentLaunchForce = m_MaxLaunchForce;
                    Fire();
                }
                // Otherwise, if the fire button has just started being pressed...
                else if (Input.GetButtonDown(m_FireButton))
                {
                    // ... reset the fired flag and reset the launch force.
                    m_Fired = false;
                    m_CurrentLaunchForce = m_MinLaunchForce;

                    // Change the clip to the charging clip and start it playing.
                    m_ShootingAudio.clip = m_ChargingClip;
                    m_ShootingAudio.Play();
                }
                // Otherwise, if the fire button is being held and the shell hasn't been launched yet...
                else if (Input.GetButton(m_FireButton) && !m_Fired)
                {
                    // Increment the launch force and update the slider.
                    m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;

                    m_AimSlider.value = m_CurrentLaunchForce;
                }
                // Otherwise, if the fire button is released and the shell hasn't been launched yet...
                else if (Input.GetButtonUp(m_FireButton) && !m_Fired)
                {
                    // ... launch the shell.
                    Fire();
                }
            }
        }
    }


    public void Fire()
    {
        if (m_currentAmmo <= 0)
            return;

        if (Time.time - m_lastFireTime < m_cooldownTime)
            return;


        // Set the fired flag so only Fire is only called once.
        m_Fired = true;

        // Create an instance of the shell and store a reference to it's rigidbody.
        Rigidbody shellInstance =
            Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;

        ShellExplosion shellExplosionInstance = shellInstance.gameObject.GetComponent<ShellExplosion>();
        shellExplosionInstance.m_parent = gameObject;
        shellExplosionInstance.m_isUpgraded = m_tankAddons.IsUpgradeActive(UpgradeType.E_UPGRADE_WEAPON);


        // Set the shell's velocity to the launch force in the fire position's forward direction.
        shellInstance.velocity = m_CurrentLaunchForce * m_FireTransform.forward * GameManager.m_SpeedMultiplier;

        // Change the clip to the firing clip and play it.
        m_ShootingAudio.clip = m_FireClip;
        m_ShootingAudio.Play();

        // Reset the launch force.  This is a precaution in case of missing button events.
        m_CurrentLaunchForce = m_MinLaunchForce;

        m_lastFireTime = Time.time;
        m_currentAmmo--;
        Debug.Assert(m_currentAmmo >= 0, "Ammo is negative somehow !!");
    }

    public void MaximizeAmmo()
    {
        m_currentAmmo = m_maxAmmo;
    }

    public int getCurrentAmmo()
    {
        return m_currentAmmo;
    }

    public float GetCurrentAmmoPercent()
    {
        return (float)m_currentAmmo / m_maxAmmo;
    }

    public void SetCurrentAmmoPercent(float ammoParcent)
    {
        Debug.AssertFormat(0.0f <= ammoParcent && ammoParcent <= 1.0f, "ammoParcent given is not between 0-1, %d", ammoParcent);
        m_currentAmmo = (int)(ammoParcent * m_maxAmmo);
    }
}
