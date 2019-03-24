using UnityEngine;


public class ShellExplosion : MonoBehaviour
{
    public LayerMask m_TankMask;                        // Used to filter what the explosion affects, this should be set to "Players".
    public ParticleSystem m_ExplosionParticles;         // Reference to the particles that will play on explosion.
    public AudioSource m_ExplosionAudio;                // Reference to the audio that will play on explosion.
    public float m_MaxDamage = 100f;                    // The amount of damage done if the explosion is centred on a tank.
    public float m_ExplosionForce = 1000f;              // The amount of force added to a tank at the centre of the explosion.
    public static float m_MaxDistanceTravelledSqr = 900f;                    // The time in seconds before the shell is removed.
    public float m_ExplosionRadius = 5f;                // The maximum distance away from the explosion tanks can be and are still affected.
    public float m_WeaponUpgradedMultiplier = 2.0f;     // How much to boost damage taken if weapon is upgraded
    public bool m_isUpgraded = false;                   // True if this shield is upgraded or not

    [HideInInspector]
    public GameObject m_parent; // The game object who shot this

    private Vector3 m_startPos;

    private void Start()
    {
        // If it isn't destroyed by then, destroy the shell after it's lifetime.
        //Destroy(gameObject, m_MaxLifeTime);
        m_startPos = transform.position;
    }

    private void Update()
    {
        // Check already travelled distance
        float actualTravelDistanceSqr = (m_startPos - transform.position).sqrMagnitude;
        if (actualTravelDistanceSqr > m_MaxDistanceTravelledSqr)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Collect all the colliders in a sphere from the shell's current position to a radius of the explosion radius.
        Collider[] colliders = Physics.OverlapSphere(transform.position, m_ExplosionRadius, m_TankMask);

        bool hitOtherObjectsThanFriendlyOnes = false;

        // Go through all the colliders...
        for (int i = 0; i < colliders.Length; i++)
        {            
            // If I hit a friendly object don't care
            if (colliders[i].gameObject == m_parent)
                continue;

            hitOtherObjectsThanFriendlyOnes = true;

            // ... and find their rigidbody.
            Rigidbody targetRigidbody = colliders[i].GetComponent<Rigidbody>();

            // If they don't have a rigidbody, go on to the next collider.
            if (!targetRigidbody)
                continue;

            // Don't take damage while shiled is active !
            BoxAddonBehavior tankAddons = targetRigidbody.GetComponent<BoxAddonBehavior>();
            if (tankAddons.IsUpgradeActive(UpgradeType.E_UPGRADE_SHIELD))
                continue;

            // Add an explosion force.
            targetRigidbody.AddExplosionForce(m_ExplosionForce, transform.position, m_ExplosionRadius);

            // Find the TankHealth script associated with the rigidbody.
            TankHealth targetHealth = targetRigidbody.GetComponent<TankHealth>();

            // If there is no TankHealth script attached to the gameobject, go on to the next collider.
            if (!targetHealth)
                continue;

            // Calculate the amount of damage the target should take based on it's distance from the shell.
            float damage = CalculateDamage(targetRigidbody.position) * (m_isUpgraded == false ? 1.0f : m_WeaponUpgradedMultiplier);


            // Deal this damage to the tank.
            targetHealth.TakeDamage(damage);
        }

        if (hitOtherObjectsThanFriendlyOnes)
        {
            // Unparent the particles from the shell.
            m_ExplosionParticles.transform.parent = null;

            // Play the particle system.
            m_ExplosionParticles.Play();

            // Play the explosion sound effect.
            m_ExplosionAudio.Play();

            // Once the particles have finished, destroy the gameobject they are on.
            Destroy(m_ExplosionParticles.gameObject, m_ExplosionParticles.duration);

            // Destroy the shell.
            Destroy(gameObject);
        }
    }


    private float CalculateDamage(Vector3 targetPosition)
    {
        // Create a vector from the shell to the target.
        Vector3 explosionToTarget = targetPosition - transform.position;

        // Calculate the distance from the shell to the target.
        float explosionDistance = explosionToTarget.magnitude;

        // Calculate the proportion of the maximum distance (the explosionRadius) the target is away.
        float relativeDistance = (m_ExplosionRadius - explosionDistance) / m_ExplosionRadius;

        // Calculate damage as this proportion of the maximum possible damage.
        float damage = relativeDistance * m_MaxDamage;

        // Make sure that the minimum damage is always 0.
        damage = Mathf.Max(0f, damage);

        return damage;
    }
}
