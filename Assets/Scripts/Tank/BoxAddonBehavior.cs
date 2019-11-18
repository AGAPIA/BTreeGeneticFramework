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

    public class UpgradeInfo
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

    [HideInInspector]
    public UpgradeInfo[] m_upgrades = null;

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
   }

    // Update is called once per frame
    void Update()
    {

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
        if (m_upgrades != null)
        {
            for (int i = 0; i < (int)UpgradeType.E_NUM_UPGRADES; i++)
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
