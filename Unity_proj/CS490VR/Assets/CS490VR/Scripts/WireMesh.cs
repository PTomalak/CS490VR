using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WireMesh : MonoBehaviour
{
    // How big the center tile of each wire should be
    const float WIRE_SIZE = 0.25f;

    // The size of the rectangle containing all of the wires
    public Vector3Int size = new Vector3Int(3,3,3);

    // A 3D array of booleans representing whether there is a wire at each point
    bool[,,] voxels;

    private void Awake()
    {
        voxels = new bool[size.x, size.y, size.z];
        voxels[1, 1, 1] = true;
        voxels[1, 1, 0] = true;
        voxels[1, 0, 0] = true;
        voxels[1, 1, 2] = true;
        voxels[2, 1, 2] = true;
        voxels[2, 1, 1] = true;

        CreateMesh();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void CreateMesh()
    {
        // Mesh variables
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();

        // Populate list of vertices, as well as offsets to quickly find vertices from xyz pos
        int[,,] offsets = new int[size.x, size.y, size.z];
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    PopulateVertices(ref vertices, ref offsets, ref uv, x, y, z);
                }
            }
        }

        // Storing each direction vector for convenient checking
        List<int[]> directions = new List<int[]>
        {
                new int[3]{1,0,0},                    // front
                new int[3]{-1,0,0},                    // back
                new int[3]{0,1,0},                    // top
                new int[3]{0,-1,0},                   // bottom
                new int[3]{0,0,1},                    // right
                new int[3]{0,0,-1},                   // left
        };

        // Populate triangles
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    if (!voxels[x, y, z]) continue;

                    for (int i = 0; i < 6; i++)
                    {
                        int[] dir = directions[i];                      // Obtain direction vector
                        int opposite = (i % 2 == 0) ? i + 1 : i - 1;    // Opposite direction on same axis

                        if (HasConnection(x, y, z, dir[0], dir[1], dir[2]))
                        {
                            // Skip double-creating the mesh
                            // Those with lower X/Y/Z coords are prioritized
                            if (i % 2 == 0) continue;

                            // Need to attach the longer wire connection
                            int x2 = x + dir[0];
                            int y2 = y + dir[1];
                            int z2 = z + dir[2];
                            int[] fv = GetVertices(ref vertices, ref offsets, x, y, z, i);
                            int[] ov = GetVertices(ref vertices, ref offsets, x2, y2, z2, opposite);

                            // Top/down meshes are handled differently
                            if (i < 2 || i > 3)
                            {
                                // Create 8 triangles, connecting the face vertices
                                int[] connect_triangles = new int[24]
                                {
                                    ov[0], ov[1], fv[0],    // Top #1
                                    ov[0], fv[0], fv[1],    // Top #2
                                    ov[2], ov[3], fv[2],    // Bottom #1
                                    fv[2], fv[3], ov[2],    // Bottom #2
                                    fv[1], ov[3], ov[0],    // Right #1
                                    fv[1], fv[2], ov[3],    // Right #2
                                    ov[1], fv[3], fv[0],    // Left #1
                                    ov[1], ov[2], fv[3],    // Left #2
                                };
                                triangles.AddRange(connect_triangles);
                            } else
                            {
                                int[] connect_triangles = new int[24]
                                {
                                    ov[0], ov[1], fv[2],    // Front #1
                                    ov[0], fv[2], fv[3],    // Front #2
                                    ov[2], ov[3], fv[0],    // Back #1
                                    ov[2], fv[0], fv[1],    // Back #2
                                    ov[1], ov[2], fv[2],    // Right #1
                                    ov[2], fv[1], fv[2],    // Right #2
                                    ov[3], ov[0], fv[0],    // Left #1
                                    ov[0], fv[3], fv[0],    // Left #2
                                };
                                triangles.AddRange(connect_triangles);
                            }
                        } else
                        {
                            // Create two triangles on the face vertices
                            int[] face_vertices = GetVertices(ref vertices, ref offsets, x, y, z, i);
                            int[] face_triangles = new int[6]{
                                face_vertices[0],
                                face_vertices[1],
                                face_vertices[2],
                                face_vertices[0],
                                face_vertices[2],
                                face_vertices[3]
                            };
                            triangles.AddRange(face_triangles);
                        }
                    }
                }
            }
        }

        Mesh newMesh = new Mesh()
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray()
        };
        newMesh.Optimize();
        newMesh.RecalculateNormals();
        //newMesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = newMesh;
    }

    // Adds the vertices representing coords x,y,z to vertices and updates offsets
    void PopulateVertices(ref List<Vector3> vertices, ref int[,,] offsets, ref List<Vector2> uv, int x, int y, int z)
    {
        if (voxels[x, y, z])
        {
            Vector3 basePos = new Vector3(x, y, z);
            float co = WIRE_SIZE / 2;
            List<Vector3> corners = new List<Vector3>()
            {
                basePos + new Vector3(co, co, co),          //front-top-right
                basePos + new Vector3(co, co, -co),         //front-top-left
                basePos + new Vector3(co, -co, co),         //front-bottom-right
                basePos + new Vector3(co, -co, -co),        //front-bottom-left
                basePos + new Vector3(-co, co, co),         //back-top-right
                basePos + new Vector3(-co, co, -co),        //back-top-left
                basePos + new Vector3(-co, -co, co),        //back-bottom-right
                basePos + new Vector3(-co, -co, -co),       //back-bottom-left
            };
            offsets[x, y, z] = vertices.Count;
            vertices.AddRange(corners);

            // Populate UVs
            //List<Vector2> uvs = new List<Vector2>()
            //{
            //    new Vector2(5f/8f, 5f/8f),
            //    new Vector2(3f/8f, 5f/8f),
            //    new Vector2(5f/8f, 3f/8f),
            //    new Vector2(3f/8f, 3f/8f),
            //    new Vector2(5f/8f, 5f/8f),
            //    new Vector2(3f/8f, 5f/8f),
            //    new Vector2(5f/8f, 3f/8f),
            //    new Vector2(3f/8f, 3f/8f)
            //};
            //uv.AddRange(uvs);
        }
    }

    // Obtain the four vertices of a given block that are perpendicular to that direction, in clockwise order
    int[] GetVertices(ref List<Vector3> vertices, ref int[,,] offsets, int x, int y, int z, int dir_num)
    {
        int offset = offsets[x, y, z];

        // Pre-calculate the relevant index options manually since there's only 6 options
        // Additionally, elements are sorted to be in clockwise (TL -> TR -> BR -> BL) order when facing them
        List<int[]> indices_options = new List<int[]>
        {
            new int[4] { 1, 0, 2, 3 },  // Front face
            new int[4] { 4, 5, 7, 6 },  // Back face
            new int[4] { 5, 4, 0, 1 },  // Top face
            new int[4] { 3, 2, 6, 7 },  // Bottom face
            new int[4] { 0, 4, 6, 2 },  // Right face
            new int[4] { 5, 1, 3, 7 }   // Left face
        };
        int[] indices = indices_options[dir_num];
        for (int i = 0; i < indices.Length; i++)
        {
            indices[i] += offset;
        }

        return indices;
    }

    // Returns whether there is a wire connection on this axis
    bool HasConnection(int x, int y, int z, int dx, int dy, int dz)
    {
        if (x + dx < 0 || x + dx > size.x - 1) return false;
        if (y + dy < 0 || y + dy > size.y - 1) return false;
        if (z + dz < 0 || z + dz > size.z - 1) return false;
        return voxels[x + dx, y + dy, z + dz];
    }
}
