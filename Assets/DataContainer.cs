using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataContainer : MonoBehaviour
{
    public LayerMask m_TankMask;
    public int boxId;
    public BoxType boxType;

    BoxesSpawnScript m_observer;

    // Start is called before the first frame update
    void Start()
    {
        // Adding by hand a box collider, it will match the rendering mesh size by default
        //boxDetails = default;
        gameObject.AddComponent<BoxCollider>();

        /*
        BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
        If you want to manually scale the boxcollider you can use the following

         MeshRenderer renderer = gameObject.getcomponent<MeshRenderer>();
        boxCollider.center = renderer.bounds.center;
        boxCollider.size = renderer.bounds.size;


        */
    }

    public void SetData(BoxesSpawnScript observer, BoxType _type, int _id)
    {
        boxId = _id;
        boxType = _type;
        m_observer = observer;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Collect all the colliders in a sphere from the shell's current position to a radius of the explosion radius.
        Collider[] colliders = Physics.OverlapSphere(transform.position, 10, m_TankMask);

        // Go through all the colliders...
        for (int i = 0; i < colliders.Length; i++)
        {
            BoxAddonBehavior targetBoxBehavior = colliders[i].GetComponent<BoxAddonBehavior>();

            // If they don't have a target box behavior, go on to the next collider.
            if (!targetBoxBehavior)
                continue;

            targetBoxBehavior.ActivateUpgrade(boxType, boxId);

            // Notify the observer that this box object lifetime is over
            if (m_observer)
            {
                m_observer.despawnBox(boxId);
            }

            // Destroy this game object
            Destroy(gameObject);
            break;
        }

    }
}