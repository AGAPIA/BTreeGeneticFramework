using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Threading;


public class DeepTestingSystem : MonoBehaviour
{
    RestUploadingImg m_restUploadingImg = null;

    public DeepTestingSystem(GameObject parentGameObject)
    {
        m_parentGameObject = parentGameObject;
    }

    private void Start()
    {
        // Bring in the REST componnents
        m_restUploadingImg = m_parentGameObject.AddComponent<RestUploadingImg>();

        // Find the needed components
        m_gameManager = m_parentGameObject.GetComponent<GameManager>();
    }

    public void Setup(GameObject parentObj)
    {
        m_parentGameObject = parentObj;
    }

    public void CustomUpdate()
    {
        // Not fully inited yet ?
        if (m_restUploadingImg == null)
            return;

        // DEBUG CODE
        if (Time.frameCount % 100 == 0)
        {
            //Thread.Sleep(5000);
            m_restUploadingImg.DoUploadPNG();
        }
    }

    GameObject m_parentGameObject;
    GameManager m_gameManager;
}
