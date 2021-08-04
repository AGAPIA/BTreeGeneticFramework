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
        m_camera = null;
    }

    private void Start()
    {
        // Bring in the REST componnents
        m_restUploadingImg = m_parentGameObject.AddComponent<RestUploadingImg>();

        // Find the needed components
        m_gameManager = m_parentGameObject.GetComponent<GameManager>();

        m_renderer = GetComponent<Renderer>();
        m_camera = GetComponent<Camera>();
    }

    private void OnDrawGizmosSelected()
    {
        // A sphere that fully encloses the bounding box.
        Vector3 center = m_renderer.bounds.center;
        float radius = m_renderer.bounds.extents.magnitude;

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(center, radius);
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


            if (false) // TODO
            {
                // 3D to 2D bounding box, screen space
                Bounds bounds = m_renderer.bounds;
                Vector3 c = bounds.center;
                Vector3 e = bounds.extents;

                // Vertices of the 3D bbox
                UnityEngine.Vector3[] worldCorners = new[] {
            new UnityEngine.Vector3( c.x + e.x, c.y + e.y, c.z + e.z ),
            new UnityEngine.Vector3( c.x + e.x, c.y + e.y, c.z - e.z ),
            new UnityEngine.Vector3( c.x + e.x, c.y - e.y, c.z + e.z ),
            new UnityEngine.Vector3( c.x + e.x, c.y - e.y, c.z - e.z ),
            new UnityEngine.Vector3( c.x - e.x, c.y + e.y, c.z + e.z ),
            new UnityEngine.Vector3( c.x - e.x, c.y + e.y, c.z - e.z ),
            new UnityEngine.Vector3( c.x - e.x, c.y - e.y, c.z + e.z ),
            new UnityEngine.Vector3( c.x - e.x, c.y - e.y, c.z - e.z ),
            };

                // Find the 2D on screen rectangle encompasing the 3D bounding box
                Rect retVal = Rect.MinMaxRect(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);

                // iterate through the vertices to get the equivalent screen projection
                for (int i = 0; i < worldCorners.Length; i++)
                {
                    Vector3 v = m_camera.WorldToScreenPoint(worldCorners[i]);
                    if (v.x < retVal.xMin)
                        retVal.xMin = v.x;
                    if (v.y < retVal.yMin)
                        retVal.yMin = v.y;
                    if (v.x > retVal.xMax)
                        retVal.xMax = v.x;
                    if (v.y > retVal.yMax)
                        retVal.yMax = v.y;
                }
            }
        }
    }

    GameObject m_parentGameObject;
    GameManager m_gameManager;
    Renderer m_renderer;
    Camera m_camera;
}
