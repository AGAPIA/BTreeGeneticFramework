using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MoveTo : MonoBehaviour
{
    public Transform goal;

    NavMeshAgent m_agent = null;
    public Camera cameraRef;

    void Start()
    {
        m_agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (cameraRef == null)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cameraRef.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                m_agent.SetDestination(hit.point);
            }
        }
    }
}
