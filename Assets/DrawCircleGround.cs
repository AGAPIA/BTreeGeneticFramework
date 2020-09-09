using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(LineRenderer))]
public class DrawCircleGround : MonoBehaviour
{
    [Range(0, 50)]
    public int segments = 50;
    [Range(0, 5)]
    public float xradius = 5;
    [Range(0, 5)]
    public float yradius = 5;
    LineRenderer m_refLine;

    private float m_currentTime = 0.0f;
    public Color m_baseColor = new Color(1.0f, 0.0f, 0.0f, 1.0f);


    // Start is called before the first frame update
    void Start()
    {
        m_refLine = gameObject.GetComponent<LineRenderer>();
        //m_refLine.material = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        m_refLine.positionCount = segments + 1;
        m_refLine.useWorldSpace = false;
        CreatePoints();
    }

    void CreatePoints()
    {
        float x;
        float y = 0.5f;
        float z;

        float angle = 20f;

        for (int i = 0; i < (segments + 1); i++)
        {
            x = Mathf.Sin(Mathf.Deg2Rad * angle) * xradius;
            z = Mathf.Cos(Mathf.Deg2Rad * angle) * yradius;

            m_refLine.SetPosition(i, new Vector3(x, y, z));

            angle += (360f / segments);
        }
    }

    // Update is called once per frame
    void Update()
    {
        m_currentTime += Time.deltaTime;

        m_baseColor.a = (float) Math.Abs(Math.Sin(Time.time * 4));
        m_refLine.startColor = m_refLine.endColor = m_baseColor;
    }
}
