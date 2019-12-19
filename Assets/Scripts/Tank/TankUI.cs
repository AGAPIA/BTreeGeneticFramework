using System;
using UnityEngine;
using UnityEngine.UI;

public class TankUI
{
    Text m_ammoText;
    Text m_shieldText;
    Text m_weaponUpgradeText;
    Text m_numLifesText;
    Text m_debugText;

    bool m_enabled = false;

    TankManager m_parentTankManager = null;
    GameObject m_parentGameObject = null;

    bool m_refsCreated = false;

    public void Setup(TankManager parentTankManager)
    {
        m_parentTankManager = parentTankManager;
        m_parentGameObject = parentTankManager.m_Instance;
        m_enabled = true;

        // Init texts
        m_ammoText = m_parentGameObject.transform.Find("TankTextInfo/Canvas/Text_Ammo").gameObject.GetComponent<Text>();
        m_ammoText.enabled = true;
        m_ammoText.text = "Ammo: inf";

        m_shieldText = m_parentGameObject.transform.Find("TankTextInfo/Canvas/Text_Shield").gameObject.GetComponent<Text>();
        m_shieldText.enabled = false;

        m_weaponUpgradeText = m_parentGameObject.transform.Find("TankTextInfo/Canvas/Text_WeaponUpgrade").gameObject.GetComponent<Text>();
        m_weaponUpgradeText.enabled = false;

        m_numLifesText = m_parentGameObject.transform.Find("TankTextInfo/Canvas/Text_NumLifes").gameObject.GetComponent<Text>();
        m_numLifesText.enabled = true;
        m_numLifesText.text = "L: inf";

        m_debugText = m_parentGameObject.transform.Find("TankTextInfo/Canvas/Text_Debug").gameObject.GetComponent<Text>();
        m_debugText.enabled = true;
        m_debugText.text = "Id: X State: S";
    }

    public void Reset()
    {
        if (!m_enabled)
            return;

        // If pointers have been cached...
        if (m_ammoText)
        {
            m_ammoText.enabled = false;
            m_shieldText.enabled = false;
            m_weaponUpgradeText.enabled = false;
        }
    }

    void CheckRefs()
    {
        if (!m_enabled)
            return;

        if (!m_refsCreated)
        {
            if (m_parentTankManager.m_Addons.m_upgrades != null)
            {
                m_parentTankManager.m_Addons.m_upgrades[(int)UpgradeType.E_UPGRADE_SHIELD].refToText = m_shieldText;
                m_parentTankManager.m_Addons.m_upgrades[(int)UpgradeType.E_UPGRADE_WEAPON].refToText = m_weaponUpgradeText;

                m_refsCreated = true;
            }
        }
    }

    public void Update()
    {
        if (!m_enabled)
            return;

        CheckRefs();

        // Project parent tank pos to 2d screen pos
        Vector3 screenPos = Camera.main.WorldToScreenPoint(m_parentGameObject.transform.position);


        screenPos.y -= 27;
        if (m_debugText.enabled)
        {
            m_debugText.transform.position = screenPos;
        }

        // Set the num lives text and pos
        screenPos.y += 15;
        m_numLifesText.transform.position = screenPos;
        int numLifes = m_parentTankManager.m_Health.GetCurrentNumLives(); // TODO: make this an observer and change text only when needed
        m_numLifesText.text = "L: " + numLifes;

        // Set the ammo text and pos
        screenPos.y += 12;
        m_ammoText.transform.position = screenPos;
        int currentAmmo = m_parentTankManager.m_Shooting.getCurrentAmmo();
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

    public void setDebugText(String s)
    {
        m_debugText.text = s;
    }
};
