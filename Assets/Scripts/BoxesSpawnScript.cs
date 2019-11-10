using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public enum BoxType
{
    BOXTYPE_AMMO = 0,
    BOXTYPE_SHIELD,
    BOXTYPE_WEAPONUPGRADE,
    BOXTYPE_HEALTH,
    BOXTYPE_LIFE,
    BOXTYPE_NUMS,
};

public class BoxesSpawnScript : MonoBehaviour
{
    public int MaxNumberOfBoxesPerType = 10;
    public float TimeInSecondsBetweenBoxSpawn = 3.0f;
    public GameObject[] m_boxPrefab = new GameObject[(int)BoxType.BOXTYPE_NUMS];
    private float m_timeUntilNextSpawn;
    private System.Random m_random = new System.Random();

    public GameObject m_parentForBoxes;

    // From Id to BoxItem and number of boxes by each category
    Dictionary<int, BoxType> m_boxesDict = new Dictionary<int, BoxType>();
    int[] m_numBoxesPerType = new int[(int)BoxType.BOXTYPE_NUMS];

    // The global id used for all boxes
    int m_globalId = 0;
    BoxType[] m_tempAvailableTypes = new BoxType[(int)BoxType.BOXTYPE_NUMS];
    int m_tempAvailableTypes_count = 0;

    public BoxesSpawnScript ()
    {
        for (int i = 0; i < (int)BoxType.BOXTYPE_NUMS; i++)
            m_numBoxesPerType[i] = 0;
    }

    // Returns the id of the newly spawned box
    private int spawnBox(BoxType type)
    {
        Debug.Assert(m_numBoxesPerType[(int)type] < MaxNumberOfBoxesPerType);
        m_numBoxesPerType[(int)type]++;

        int thisBoxId = m_globalId++;
        m_boxesDict.Add(thisBoxId, type);

        return thisBoxId;
    }

    public void despawnBox(int boxId)
    {
        BoxType boxType;
        bool foundObj = m_boxesDict.TryGetValue(boxId, out boxType);

        if (foundObj)
        {
            m_boxesDict.Remove(boxId);
            m_numBoxesPerType[(int)boxType]--;
            Debug.Assert(m_numBoxesPerType[(int)boxType] >= 0);
        }
        else
        {
            Debug.Assert(false, string.Format("Couldn't delete object %d because it can't be found in the dictionary ", boxId));
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        m_parentForBoxes = GameObject.Find("BoxesParent");
        m_timeUntilNextSpawn = 0;
    }

    void Update()
    {
        m_timeUntilNextSpawn -= Time.deltaTime;

        // Need to spawn a box ?
        if (m_timeUntilNextSpawn < 0.0f)
        {
            // Choose the position and orientation
            Vector3 newBoxPos = UtilsNavMesh_Impl.GenerateRandomPointOnMesh();
            Debug.DrawRay(newBoxPos, Vector3.up, Color.blue, 10.0f);
            Quaternion newBoxQuat = Quaternion.identity;

            // Choose the type to spawn from available ones            
            BoxType chosenType = BoxType.BOXTYPE_NUMS;
            {
                m_tempAvailableTypes_count = 0;
                for (int i = 0; i < (int)BoxType.BOXTYPE_NUMS; i++)
                {
                    if (m_numBoxesPerType[i] < MaxNumberOfBoxesPerType)
                    {
                        m_tempAvailableTypes[m_tempAvailableTypes_count++] = (BoxType)i;
                    }
                }

                if (m_tempAvailableTypes_count > 0)
                {
                    chosenType = (BoxType)m_tempAvailableTypes[m_random.Next(0, m_tempAvailableTypes_count)];
                    int boxId = spawnBox(chosenType);
                    GameObject cloned = Instantiate(m_boxPrefab[(int)chosenType], newBoxPos, newBoxQuat, m_parentForBoxes.transform);

                    if (cloned == null || cloned.GetComponent<DataContainer>() == null)
                        Debug.Break();

                    cloned.GetComponent<DataContainer>().SetData(this, chosenType, boxId);
                }
            }
            
            m_timeUntilNextSpawn = TimeInSecondsBetweenBoxSpawn;
        }
    }
}
