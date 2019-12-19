using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

class UtilsNavMesh_Impl
{
    static Mesh m_levelNavMesh; // Here we store the mesh of the navmesh
    static float[] m_normalizedAreaWeights; // This stores the normalized area weights for all triangles inside the levelNavMesh
    static Rect m_safeArea;
    
    public static void LoadAndPreprocessNavMesh(float safetyXPercent, float safetyYPercent)
    {
        NavMeshTriangulation triangulatedNavMesh = NavMesh.CalculateTriangulation();
        m_levelNavMesh = new Mesh();
        m_levelNavMesh.name = "ExportedNavMesh";
        m_levelNavMesh.vertices = triangulatedNavMesh.vertices;
        m_levelNavMesh.triangles = triangulatedNavMesh.indices;
        

        // 1 - Calculate Surface Areas
        float[] triangleSurfaceAreas = CalculateSurfaceAreas(m_levelNavMesh);

        // 2 - Normalize area weights
        m_normalizedAreaWeights = NormalizeAreaWeights(triangleSurfaceAreas);

        // Compute the safe area (2D coordinates for navmesh)
        for (int i = 0; i < m_levelNavMesh.vertices.Length; i++)
        {
            Vector3 point = m_levelNavMesh.vertices[i];
            if (m_safeArea.xMin > point.x)
                m_safeArea.xMin = point.x;

            if (m_safeArea.xMax < point.x)
                m_safeArea.xMax = point.x;

            if (m_safeArea.yMin > point.z)
                m_safeArea.yMin = point.z;

            if (m_safeArea.yMax < point.z)
                m_safeArea.yMax = point.z;
        }

        float originalWidth = m_safeArea.width;
        float originalHeight = m_safeArea.height;
        m_safeArea.xMin += originalWidth * safetyXPercent;
        m_safeArea.xMax -= originalWidth * safetyYPercent;
        m_safeArea.yMin += originalHeight * safetyYPercent;
        m_safeArea.yMax -= originalHeight * safetyYPercent;
    }

    /// <summary>
    /// //////////////////////////////////////////////////////////////////////////////////
    /// </summary>

    public static Vector3 GenerateRandomPointOnMesh()
    {
        do
        {
            // 3 - Generate 'triangle selection' random #
            float triangleSelectionValue = Random.value;

            // 4 - Walk through the list of weights to select the proper triangle
            int triangleIndex = SelectRandomTriangle(triangleSelectionValue);

            // 5 - Generate a random barycentric coordinate
            Vector3 randomBarycentricCoordinates = GenerateRandomBarycentricCoordinates();

            // 6 - Using the selected barycentric coordinate and the selected mesh triangle, convert
            //     this point to world space.
            Vector3 res = ConvertToLocalSpace(randomBarycentricCoordinates, triangleIndex);

            // This is the most stupid implementation i've ever done
            // Instead of this, please modify this code when you find it to cut the triangles in the preprocessing part( see load function)
            // And perform a single iteration please..
            Vector2 pos2D = default;
            pos2D.Set(res.x, res.z);
            if (m_safeArea.Contains(pos2D))
            {
                return res;
            }

        } while (true);

        return Vector3.zero;
    }

    static private float[] CalculateSurfaceAreas(Mesh mesh)
    {
        int triangleCount = mesh.triangles.Length / 3;

        float[] surfaceAreas = new float[triangleCount];


        for (int triangleIndex = 0; triangleIndex < triangleCount; triangleIndex++)
        {
            Vector3[] points = new Vector3[3];
            points[0] = mesh.vertices[mesh.triangles[triangleIndex * 3 + 0]];
            points[1] = mesh.vertices[mesh.triangles[triangleIndex * 3 + 1]];
            points[2] = mesh.vertices[mesh.triangles[triangleIndex * 3 + 2]];

            // calculate the three sidelengths and use those to determine the area of the triangle
            // http://www.wikihow.com/Sample/Area-of-a-Triangle-Side-Length
            float a = (points[0] - points[1]).magnitude;
            float b = (points[0] - points[2]).magnitude;
            float c = (points[1] - points[2]).magnitude;

            float s = (a + b + c) / 2;

            surfaceAreas[triangleIndex] = Mathf.Sqrt(s * (s - a) * (s - b) * (s - c));
        }

        return surfaceAreas;
    }

    static private float[] NormalizeAreaWeights(float[] surfaceAreas)
    {
        float[] normalizedAreaWeights = new float[surfaceAreas.Length];

        float totalSurfaceArea = 0;
        foreach (float surfaceArea in surfaceAreas)
        {
            totalSurfaceArea += surfaceArea;
        }

        for (int i = 0; i < normalizedAreaWeights.Length; i++)
        {
            normalizedAreaWeights[i] = surfaceAreas[i] / totalSurfaceArea;
        }

        return normalizedAreaWeights;
    }

    static private int SelectRandomTriangle(float triangleSelectionValue)
    {
        float accumulated = 0;

        for (int i = 0; i < m_normalizedAreaWeights.Length; i++)
        {
            accumulated += m_normalizedAreaWeights[i];

            if (accumulated >= triangleSelectionValue)
            {
                return i;
            }
        }

        // unless we were handed malformed normalizedAreaWeights, we should have returned from this already.
        throw new System.ArgumentException("Normalized Area Weights were not normalized properly, or triangle selection value was not [0, 1]");
    }

    static private Vector3 GenerateRandomBarycentricCoordinates()
    {
        Vector3 barycentric = new Vector3(Random.value, Random.value, Random.value);

        while (barycentric == Vector3.zero)
        {
            // seems unlikely, but just in case...
            barycentric = new Vector3(Random.value, Random.value, Random.value);
        }

        // normalize the barycentric coordinates. These are normalized such that x + y + z = 1, as opposed to
        // normal vectors which are normalized such that Sqrt(x^2 + y^2 + z^2) = 1. See:
        // http://en.wikipedia.org/wiki/Barycentric_coordinate_system
        float sum = barycentric.x + barycentric.y + barycentric.z;

        return barycentric / sum;
    }

    static private Vector3 ConvertToLocalSpace(Vector3 barycentric, int triangleIndex)
    {
        Vector3[] points = new Vector3[3];
        points[0] = m_levelNavMesh.vertices[m_levelNavMesh.triangles[triangleIndex * 3 + 0]];
        points[1] = m_levelNavMesh.vertices[m_levelNavMesh.triangles[triangleIndex * 3 + 1]];
        points[2] = m_levelNavMesh.vertices[m_levelNavMesh.triangles[triangleIndex * 3 + 2]];

        return (points[0] * barycentric.x + points[1] * barycentric.y + points[2] * barycentric.z);
    }

};

public class UtilsNavMesh : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        UtilsNavMesh_Impl.LoadAndPreprocessNavMesh(0.15f, 0.15f);
    }
    
}
