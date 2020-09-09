using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TutorialArrowDrawing : MonoBehaviour
{
    [HideInInspector]
    private LineRenderer m_lineRenderer;


    // Set these two from exterior to have something drawn
    [HideInInspector] 
    public Vector3 m_arrowStart;
    [HideInInspector]
    public Vector3 m_arrowEnd;

    public Color m_baseColor = new Color(1.0f, 0.0f, 0.0f, 1.0f);

    // Start is called before the first frame update
    void Start()
    {
        Setup();
    }

    void Setup()
    {
        m_lineRenderer = GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isActiveAndEnabled)
            return;

        float PercentHead = 0.01f;
        Vector3 ArrowOrigin = m_arrowStart;
        Vector3 ArrowTarget = m_arrowEnd;
        //Vector3 normalLineEnd = Vector3.Lerp(origin, target, 0.9f);
        //Vector3 arrowStart = Vector3.Lerp(origin, target, 0.91f);


        m_baseColor.a = (float)Math.Abs(Math.Sin(Time.time * 4));
        m_lineRenderer.startColor = m_lineRenderer.endColor = m_baseColor;


        m_lineRenderer.enabled = true;
        m_lineRenderer.positionCount = 2;
        m_lineRenderer.SetPositions(new Vector3[] {
            ArrowOrigin
            , Vector3.Lerp(ArrowOrigin, ArrowTarget, 0.999f - PercentHead)
            , Vector3.Lerp(ArrowOrigin, ArrowTarget, 1 - PercentHead)
            , ArrowTarget });

        bool useANim = true;
        if (!useANim)
        {
            m_lineRenderer.startWidth = 0.2f;
            m_lineRenderer.endWidth = 0.2f;
        }
        else
        {
            m_lineRenderer.widthCurve = new AnimationCurve(new Keyframe(0, 0.4f)
                , new Keyframe(0.999f - PercentHead, 0.4f)  // neck of arrow
                , new Keyframe(1 - PercentHead, 1f)  // max width of arrow head
                , new Keyframe(1, 0f));  // tip of arrow
        }
    }
}
