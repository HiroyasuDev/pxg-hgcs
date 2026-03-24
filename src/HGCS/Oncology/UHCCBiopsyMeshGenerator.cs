using UnityEngine;

namespace PXG.HGCS.Oncology
{
    /// <summary>
    /// Procedurally generates a volumetric tumor core mesh with absolute structural precision.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class UHCCBiopsyMeshGenerator : MonoBehaviour
    {
        public float Radius = 0.5f;
        public int Longitude = 24;
        public int Latitude = 24;

        void Awake()
        {
            var meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = CreateSphereMesh(Radius, Longitude, Latitude);
        }

        Mesh CreateSphereMesh(float radius, int longitude, int latitude)
        {
            var vertices = new Vector3[(longitude + 1) * (latitude + 1)];
            var normals  = new Vector3[vertices.Length];
            var uv       = new Vector2[vertices.Length];
            var triangles = new int[longitude * latitude * 6];

            int vertIndex = 0, triIndex = 0;
            for (int lat = 0; lat <= latitude; lat++)
            {
                float a1 = Mathf.PI * lat / latitude;
                float sin1 = Mathf.Sin(a1);
                float cos1 = Mathf.Cos(a1);

                for (int lon = 0; lon <= longitude; lon++)
                {
                    float a2 = 2 * Mathf.PI * lon / longitude;
                    float sin2 = Mathf.Sin(a2);
                    float cos2 = Mathf.Cos(a2);

                    vertices[vertIndex] = new Vector3(sin1 * cos2, cos1, sin1 * sin2) * radius;
                    normals[vertIndex]  = vertices[vertIndex].normalized;
                    uv[vertIndex]       = new Vector2((float)lon / longitude, (float)lat / latitude);

                    if (lat < latitude && lon < longitude)
                    {
                        int current = vertIndex;
                        int next = vertIndex + longitude + 1;

                        triangles[triIndex++] = current;
                        triangles[triIndex++] = next;
                        triangles[triIndex++] = current + 1;

                        triangles[triIndex++] = current + 1;
                        triangles[triIndex++] = next;
                        triangles[triIndex++] = next + 1;
                    }
                    vertIndex++;
                }
            }

            var mesh = new Mesh
            {
                vertices = vertices,
                normals  = normals,
                uv       = uv,
                triangles = triangles
            };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}