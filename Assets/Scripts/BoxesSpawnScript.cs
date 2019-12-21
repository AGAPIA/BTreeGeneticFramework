using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum BoxType
{
    BOXTYPE_AMMO = 0,
    BOXTYPE_SHIELD,
    BOXTYPE_WEAPONUPGRADE,
    BOXTYPE_HEALTH,
    //BOXTYPE_LIFE,
    BOXTYPE_NUMS,
};

public class BoxesSpawnScript : MonoBehaviour
{
    private string[] BoxTypeTagStrings = new string[(int)BoxType.BOXTYPE_NUMS]
    {
        "Box_Ammo",
        "Box_Shield",
        "Box_WeaponUpgrade",
        "Box_HP",
        //"Box_Life",
    };

    public string GetTagForBoxType(BoxType type) { return BoxTypeTagStrings[(int)type]; }

    public int MaxNumberOfBoxesPerType = 10;
    public float TimeInSecondsBetweenBoxSpawn = 3.0f;
    public GameObject[] m_boxPrefab = new GameObject[(int)BoxType.BOXTYPE_NUMS];
    private float m_timeUntilNextSpawn = 1.0f;
    private System.Random m_random = new System.Random();

    private bool m_spawnOnDemand = false;
    public bool SpawnOnDemand
    {
        get { return m_spawnOnDemand; }
        set { m_spawnOnDemand = value; }
    }

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
    public int spawnBox(BoxType type, Vector3 boxPos, Quaternion boxRotation)
    {
        // Get an id for this box, add it to local dictionary logic
        Debug.Assert(m_numBoxesPerType[(int)type] < MaxNumberOfBoxesPerType);
        m_numBoxesPerType[(int)type]++;

        int thisBoxId = m_globalId++;
        m_boxesDict.Add(thisBoxId, type);


        // Then create the physical object in the world
        GameObject prefabTarget = m_boxPrefab[(int)type];

        // Transform the object such that it is fully visible in the world (translate by render bbox size) and rotate it according to prefab rotation
        Quaternion instanceRotation = boxRotation * prefabTarget.transform.rotation;
        Vector3 instancePos = boxPos + prefabTarget.transform.position;
        Bounds prefabBounds = default(Bounds);
        Renderer prefabRenderer = prefabTarget.GetComponent<Renderer>();
        if (prefabRenderer)
        {
            prefabBounds = prefabRenderer.bounds;
        }
        
        instancePos.y += prefabBounds.size.y * 0.5f;
        
        GameObject cloned = Instantiate(prefabTarget, instancePos, instanceRotation, m_parentForBoxes.transform);
        if (cloned == null || cloned.GetComponent<DataContainer>() == null)
            Debug.Break();
        cloned.GetComponent<DataContainer>().SetData(this, type, thisBoxId);

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
        if (SpawnOnDemand)
            return;

        m_timeUntilNextSpawn -= Time.deltaTime;

        // Need to spawn a box ?
        if (m_timeUntilNextSpawn < 0.0f)
        {
            // Choose the position and orientation
            Vector3 newBoxPos = UtilsNavMesh_Impl.GenerateRandomPointOnMesh();
            Debug.DrawRay(newBoxPos, Vector3.up, Color.blue, 10.0f);
            Quaternion newBoxRotation = Quaternion.identity;

            // Choose the type to spawn from available ones            
            BoxType chosenType = BoxType.BOXTYPE_NUMS;
            {
                m_tempAvailableTypes_count = 0;

                //Used for debug to spawn only a certain box type
                int forcedSpawnBoxType = (int)BoxType.BOXTYPE_NUMS; //(int)BoxType.BOXTYPE_HEALTH;//
                for (int i = 0; i < (int)BoxType.BOXTYPE_NUMS; i++)
                {
                    if (forcedSpawnBoxType != (int)BoxType.BOXTYPE_NUMS && i != forcedSpawnBoxType)
                        continue;

                    if (m_numBoxesPerType[i] < MaxNumberOfBoxesPerType)
                    {
                        m_tempAvailableTypes[m_tempAvailableTypes_count++] = (BoxType)i;
                    }
                }

                if (m_tempAvailableTypes_count > 0)
                {
                    chosenType = (BoxType)m_tempAvailableTypes[m_random.Next(0, m_tempAvailableTypes_count)];
                    spawnBox(chosenType, newBoxPos, newBoxRotation);
                }
            }
            
            m_timeUntilNextSpawn = TimeInSecondsBetweenBoxSpawn;
        }
    }
}
