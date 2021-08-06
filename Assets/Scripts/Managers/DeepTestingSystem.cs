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

    public bool m_isDataGatheringEnabled = false;
    public int m_dataGatheringFrameRate = 100;

    public DeepTestingSystem(GameObject parentGameObject)
    {
        m_parentGameObject = parentGameObject;
        m_camera = null;
    }

    private void Start()
    {
        // Bring in the REST componnents
        m_restUploadingImg = m_parentGameObject.AddComponent<RestUploadingImg>();

        // Find the needed components
        m_gameManager = m_parentGameObject.GetComponent<GameManager>();

        m_renderer = GetComponent<Renderer>();
    }

    private void OnDrawGizmosSelected()
    {
        // A sphere that fully encloses the bounding box.
        Vector3 center = m_renderer.bounds.center;
        float radius = m_renderer.bounds.extents.magnitude;

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(center, radius);
    }

    public void Setup(GameObject parentObj, GlobalAIBlackBox globalAIBlackBox,
        bool isDataGatheringEnabled, int dataGatheringFrameRate, Camera camera)
    {
        m_parentGameObject = parentObj;
        m_globalAIBlackBox = globalAIBlackBox;
        m_isDataGatheringEnabled = isDataGatheringEnabled;
        m_dataGatheringFrameRate = dataGatheringFrameRate;
        m_camera = camera;
    }

    public void CustomUpdate()
    {
        // Not fully inited yet ?
        if (m_restUploadingImg == null)
            return;

        DoDataGathering();
    }

    void DoDataGathering()
    {
        if (!m_isDataGatheringEnabled)
            return;

        // Not the rate ?
        if (Time.frameCount % m_dataGatheringFrameRate == 0)
            return;


        // yes, time to setup annotation for this frame and send it further

        Dictionary<String, Rect> annotations_entityNameTo2DRect = new Dictionary<string, Rect>();

        // Add tanks 
        foreach (KeyValuePair<int, Bounds> entry in m_globalAIBlackBox.m_tanksBounds)
        {
            String entityName = String.Format("Tank_{0}", entry.Key);
            Rect rectValue = UtilsGeneral.Bounds3DTo2DRect(entry.Value, m_camera);
            annotations_entityNameTo2DRect.Add(entityName, rectValue);
        }

        // Add boxes
        int boxIndex = 0;
        foreach (KeyValuePair<BoxType, ArrayList> entry in m_globalAIBlackBox.m_boxBoundsByType)
        {
            foreach (Bounds entryBounds in entry.Value)
            {
                String entityName = String.Format("Box_{0}_{1}", boxIndex, entry.Key.ToString());
                Rect rectValue = UtilsGeneral.Bounds3DTo2DRect(entryBounds, m_camera);
                annotations_entityNameTo2DRect.Add(entityName, rectValue);

                boxIndex++;
            }
        }
    }

    GameObject m_parentGameObject;
    GlobalAIBlackBox m_globalAIBlackBox;
    GameManager m_gameManager;
    Renderer m_renderer;
    Camera m_camera;
}
