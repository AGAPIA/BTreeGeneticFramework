using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public enum UpgradeType { E_UPGRADE_SHIELD = 0, E_UPGRADE_WEAPON, E_NUM_UPGRADES };

// Handles management of spawned boxes and addons
public class BoxAddonBehavior : MonoBehaviour
{

    [Header("Time in seconds")]
    public float ShieldActiveTime = 6.0f;
    public float AmmoUpgradeTime = 6.0f;

    class UpgradeInfo
    {
        public bool isActive = false;
        public float timeRemainingActive = 0.0f;
        public Text refToText = null; // The item on screen signaling this upgrade

        public void Reset()
        {
            isActive = false;
            timeRemainingActive = 0.0f;
        }
    };

    UpgradeInfo[] m_upgrades = null;
    Text m_ammoText;
    Text m_shieldText;
    Text m_weaponUpgradeText;
    Text m_numLifesText;

    TankShooting m_ShootingComponent;
    TankHealth m_HealthComponent;

    // Start is called before the first frame update
    void Start()
    {
        m_upgrades = new UpgradeInfo[(int)UpgradeType.E_NUM_UPGRADES];
        for (int i = 0; i < (int)UpgradeType.E_NUM_UPGRADES; i++)
        {
            m_upgrades[i] = new UpgradeInfo();
        }

        // Init texts
        m_ammoText = gameObject.transform.Find("TankTextInfo/Canvas/Text_Ammo").gameObject.GetComponent<Text>();
        m_ammoText.enabled = true;
        m_ammoText.text = "Ammo: inf";

        m_shieldText = gameObject.transform.Find("TankTextInfo/Canvas/Text_Shield").gameObject.GetComponent<Text>();
        m_shieldText.enabled = false;
        m_upgrades[(int)UpgradeType.E_UPGRADE_SHIELD].refToText = m_shieldText;

        m_weaponUpgradeText = gameObject.transform.Find("TankTextInfo/Canvas/Text_WeaponUpgrade").gameObject.GetComponent<Text>();
        m_weaponUpgradeText.enabled = false;
        m_upgrades[(int)UpgradeType.E_UPGRADE_WEAPON].refToText = m_weaponUpgradeText;


        m_numLifesText = gameObject.transform.Find("TankTextInfo/Canvas/Text_NumLifes").gameObject.GetComponent<Text>();
        m_numLifesText.enabled = true;
        m_numLifesText.text = "L: inf";

        m_ShootingComponent = gameObject.GetComponent<TankShooting>();
        m_HealthComponent = gameObject.GetComponent<TankHealth>();
    }

    public void ResetComp()
    {
        if (m_upgrades != null)
        {
            for (int i = 0; i < (int)UpgradeType.E_NUM_UPGRADES; i++)
            {
                m_upgrades[i].Reset();
            }
        }

        // If pointers have been cached...
        if (m_ammoText)
        {
            m_ammoText.enabled = false;
            m_shieldText.enabled = false;
            m_weaponUpgradeText.enabled = false;
        }
   }

    // Update is called once per frame
    void Update()
    {
        // Project parent tank pos to 2d screen pos
        Vector3 screenPos = Camera.main.WorldToScreenPoint(gameObject.transform.position);

        // Set the num lives text and pos
        screenPos.y -= 15;
        m_numLifesText.transform.position = screenPos;
        int numLifes = m_HealthComponent.GetCurrentNumLives(); // TODO: make this an observer and change text only when needed
        m_numLifesText.text = "L: " + numLifes;
               
        // Set the ammo text and pos
        screenPos.y += 12;
        m_ammoText.transform.position = screenPos;
        int currentAmmo = m_ShootingComponent.getCurrentAmmo();
        m_ammoText.text = "Ammo: " + currentAmmo; // TODO: make this an observer and change text only when needed

        // Set the shielded and weapon upgraded text
        screenPos.y += 12;
        if (m_shieldText.enabled)
        {
            m_shieldText.transform.position = screenPos;
        }

        screenPos.y += 12;
        if (m_weaponUpgradeText.enabled)
        {
            m_weaponUpgradeText.transform.position = screenPos;
        }
    }

    public bool IsUpgradeActive(UpgradeType upgradeType)
    {
        if (upgradeType >= 0 && upgradeType < UpgradeType.E_NUM_UPGRADES)
        {
            return m_upgrades[(int)upgradeType].isActive;
        }
        else
        {
            Debug.Assert(false, "Invalid upgrade type requstesd " + upgradeType.ToString());
            return false;
        }
    }

    private void FixedUpdate()
    {
        // Check upgrades timing
        for(int i = 0; i < (int)UpgradeType.E_NUM_UPGRADES; i++)
        {
            UpgradeInfo upInfo = m_upgrades[i];
            if (upInfo.isActive)
            {
                upInfo.timeRemainingActive -= Time.fixedDeltaTime;
                if (upInfo.timeRemainingActive < 0.0f)
                {
                    upInfo.isActive = false;

                    if (upInfo.refToText)
                    {
                        upInfo.refToText.enabled = false;
                    }
                }
            }
        }
    }

    public void ActivateUpgrade(BoxType boxType, int id)
    {
        if (boxType == BoxType.BOXTYPE_SHIELD || boxType == BoxType.BOXTYPE_WEAPONUPGRADE)
        {
            UpgradeType upgradeType = UpgradeType.E_NUM_UPGRADES;
            switch(boxType)
            {
                case BoxType.BOXTYPE_SHIELD:
                    upgradeType = UpgradeType.E_UPGRADE_SHIELD;
                    break;
                case BoxType.BOXTYPE_WEAPONUPGRADE:
                    upgradeType = UpgradeType.E_UPGRADE_WEAPON;
                    break;
            }

            int upgradeType_int = (int)upgradeType;

            UpgradeInfo upgradeInfoRef = m_upgrades[upgradeType_int];
            upgradeInfoRef.isActive = true;
            upgradeInfoRef.timeRemainingActive = AmmoUpgradeTime;

            if (upgradeInfoRef.refToText)
            {
                upgradeInfoRef.refToText.enabled = true;
            }
        }
        else
        {
            if (boxType == BoxType.BOXTYPE_HEALTH)
            { 
                m_HealthComponent.MaximizeLife();
            }
            else if (boxType == BoxType.BOXTYPE_AMMO)
            {
                m_ShootingComponent.MaximizeAmmo();
            }
            else if (boxType == BoxType.BOXTYPE_LIFE)
            {
                m_HealthComponent.AddLife();
            }
            else
            {
                Debug.Assert(false, "Uknown type of box type");
            }
        }
    }    
}
