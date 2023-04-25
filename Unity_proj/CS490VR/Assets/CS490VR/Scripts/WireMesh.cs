using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WireMesh : MonoBehaviour
{
    // Load the WireManager (so we can get the wiremap and the connections map)
    WireManager wm;

    // How big the center tile of each wire should be
    // This affects the texture too, which should accomodate for this size
    const float WIRE_SIZE = 3f/8f;

    // UV positions stored here for convenience
    Vector2 CONNECTOR_TL = new Vector2(WIRE_SIZE/2, 1);
    Vector2 CONNECTOR_TR = new Vector2(0.5f, 1);
    Vector2 CONNECTOR_BL = new Vector2(WIRE_SIZE/2, 1 - WIRE_SIZE);
    Vector2 CONNECTOR_BR = new Vector2(0.5f, 1 - WIRE_SIZE);

    Vector2 CENTER_TL = new Vector2(0, 1);
    Vector2 CENTER_TR = new Vector2(WIRE_SIZE/2, 1);
    Vector2 CENTER_BL = new Vector2(0, 1 - WIRE_SIZE);
    Vector2 CENTER_BR = new Vector2(WIRE_SIZE/2, 1 - WIRE_SIZE);

    Vector2 ON_OFFSET = new Vector2(0.5f, 0);

    public void ReloadMesh()
    {
        if (!wm) wm = GetComponent<WireManager>();
        if (!wm) return;

        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();
        MeshCollider mc = GetComponent<MeshCollider>();

        Mesh wireMesh = CreateMesh();
        mf.mesh = wireMesh;
        mc.sharedMesh = wireMesh;
        mr.enabled = true;
        mc.enabled = true;
    }

    Mesh CreateMesh()
    {
        // Mesh variables
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();

        // Storing each direction vector for convenient checking
        List<int[]> directions = new List<int[]>
        {
                new int[3]{1,0,0},                    // front
                new int[3]{-1,0,0},                   // back
                new int[3]{0,1,0},                    // top
                new int[3]{0,-1,0},                   // bottom
                new int[3]{0,0,1},                    // right
                new int[3]{0,0,-1},                   // left
        };

        // Array used for convenience of reversing faces
        bool[] reverseArray = new bool[6] { true, false, false, true, true, false };

        foreach (int[] c in wm.wire_map.Keys)
        {
            // Create faces
            for (int i = 0; i < 6; i++)
            {
                int[] dir = directions[i];

                // Pre-calculate relevant variables to be used for both connection faces and cap faces
                Vector3 basePos = new Vector3(c[0], c[1], c[2]);     // Position of the center of the block
                int axis = i / 2;                           // 0 = x, 1 = y, 2 = z
                int magnitude = (i % 2 == 0) ? 1 : -1;      // Sign of the direction's axis
                float co = WIRE_SIZE / 2;                   // Distance between center and edge of cube
                float a_co = co * magnitude;                // Variable center offset that varies based on axis
                bool powered = wm.wire_map[c].powered;

                // Either create a single square (no connection) or a long rectangular prism (wire connection)
                if (HasConnection(c[0], c[1], c[2], dir[0], dir[1], dir[2]))
                {
                    // Skip double-creating the mesh for pairs of connected wires (continue for non-wire connections)
                    // Those with lower X/Y/Z coords are prioritized
                    if (wm.wire_map.ContainsKey(new int[3] { c[0] + dir[0], c[1] + dir[1], c[2] + dir[2] }) && i % 2 == 0) continue;

                    // Calculate other position for convenience
                    Vector3 otherPos = new Vector3(c[0] + dir[0], c[1] + dir[1], c[2] + dir[2]);

                    // Calcuate pool of vertices (which will be drawn from multiple times to make connection faces)
                    List<Vector3> corners = axis switch
                    {
                        0 => new List<Vector3>(){
                                    // Vertices around the base position
                                    basePos + new Vector3(a_co, co, co),          //top-right
                                    basePos + new Vector3(a_co, co, -co),         //top-left
                                    basePos + new Vector3(a_co, -co, co),         //bottom-right
                                    basePos + new Vector3(a_co, -co, -co),        //bottom-left

                                    // Vertices around the other position
                                    otherPos + new Vector3(-a_co, co, co),          //top-right
                                    otherPos + new Vector3(-a_co, co, -co),         //top-left
                                    otherPos + new Vector3(-a_co, -co, co),         //bottom-right
                                    otherPos + new Vector3(-a_co, -co, -co),        //bottom-left
                                },
                        1 => new List<Vector3>(){
                                    // Vertices around the base position
                                    basePos + new Vector3(co, a_co, co),          //front-right
                                    basePos + new Vector3(co, a_co, -co),         //front-left
                                    basePos + new Vector3(-co, a_co, co),         //back-right
                                    basePos + new Vector3(-co, a_co, -co),        //back-left

                                    // Vertices around the other position
                                    otherPos + new Vector3(co, -a_co, co),          //front-right
                                    otherPos + new Vector3(co, -a_co, -co),         //front-left
                                    otherPos + new Vector3(-co, -a_co, co),         //back-right
                                    otherPos + new Vector3(-co, -a_co, -co),        //back-left
                                },
                        _ => new List<Vector3>(){
                                    // Vertices around the base position
                                    basePos + new Vector3(co, co, a_co),          //front-top
                                    basePos + new Vector3(co, -co, a_co),         //front-bottom
                                    basePos + new Vector3(-co, co, a_co),         //back-top
                                    basePos + new Vector3(-co, -co, a_co),        //back-bottom

                                    // Vertices around the other position
                                    otherPos + new Vector3(co, co, -a_co),          //front-top
                                    otherPos + new Vector3(co, -co, -a_co),         //front-bottom
                                    otherPos + new Vector3(-co, co, -a_co),         //back-top
                                    otherPos + new Vector3(-co, -co, -a_co),        //back-bottom
                                },
                    };

                    // Add all vectors twice, since each will be needed twice
                    int offset = vertices.Count;
                    vertices.AddRange(corners);
                    vertices.AddRange(corners);

                    // Add triangles (reverse triangles for y-axis)
                    int[] new_triangles;
                    if (axis == 1 && i == 3)
                    {
                        new_triangles = new int[24]
                        {
                                    // "Front/back" faces
                                    offset+4, offset+0, offset+6,
                                    offset+6, offset+0, offset+2,
                                    offset+1, offset+5, offset+7,
                                    offset+3, offset+1, offset+7,

                                    // "Left/right" faces
                                    offset+8+5, offset+8+0, offset+8+4,
                                    offset+8+1, offset+8+0, offset+8+5,
                                    offset+8+2, offset+8+3, offset+8+6,
                                    offset+8+6, offset+8+3, offset+8+7,
                        };
                    }
                    else
                    {
                        new_triangles = new int[24]
                        {
                                    // "Left/right" faces
                                    offset+0, offset+4, offset+6,
                                    offset+0, offset+6, offset+2,
                                    offset+5, offset+1, offset+7,
                                    offset+1, offset+3, offset+7,

                                    // "Top/bottom" faces
                                    offset+8+0, offset+8+5, offset+8+4,
                                    offset+8+0, offset+8+1, offset+8+5,
                                    offset+8+3, offset+8+2, offset+8+6,
                                    offset+8+3, offset+8+6, offset+8+7,
                        };
                    }

                    triangles.AddRange(new_triangles);

                    // Add UVs (offsetting them all if powered)
                    List<Vector2> new_uvs = new List<Vector2>()
                    {
                        CONNECTOR_TL, CONNECTOR_TR, CONNECTOR_BL, CONNECTOR_BR,
                        CONNECTOR_TR, CONNECTOR_TL, CONNECTOR_BR, CONNECTOR_BL,
                        CONNECTOR_BL, CONNECTOR_TL, CONNECTOR_TL, CONNECTOR_BL,
                        CONNECTOR_BR, CONNECTOR_TR, CONNECTOR_TR, CONNECTOR_BR,
                    };
                    if (powered)
                    {
                        new_uvs = new_uvs.ConvertAll(ApplyOffset);
                    }
                    uv.AddRange(new_uvs);

                    // Add new normal vectors
                    Vector3 norm_a;
                    Vector3 norm_b;
                    switch (axis)
                    {
                        case 0:
                            norm_a = new Vector3(0, 0, 1);
                            norm_b = new Vector3(0, 1, 0);
                            break;
                        case 1:
                            norm_a = new Vector3(1, 0, 0);
                            norm_b = new Vector3(0, 0, 1);
                            break;
                        default:
                            norm_a = new Vector3(0, 1, 0);
                            norm_b = new Vector3(1, 0, 0);
                            break;
                    }
                    List<Vector3> new_normals = new List<Vector3>()
                    {
                        norm_a, -norm_a, norm_a, -norm_a,
                        norm_a, -norm_a, norm_a, -norm_a,
                        norm_b, norm_b, -norm_b, -norm_b,
                        norm_b, norm_b, -norm_b, -norm_b,
                    };
                    normals.AddRange(new_normals);
                }
                else
                {
                    // Populate vertices
                    List<Vector3> corners = axis switch
                    {
                        0 => new List<Vector3>(){
                                    basePos + new Vector3(a_co, co, co),          //top-right
                                    basePos + new Vector3(a_co, co, -co),         //top-left
                                    basePos + new Vector3(a_co, -co, co),         //bottom-right
                                    basePos + new Vector3(a_co, -co, -co),        //bottom-left
                                },
                        1 => new List<Vector3>(){
                                    basePos + new Vector3(co, a_co, co),          //front-right
                                    basePos + new Vector3(co, a_co, -co),         //front-left
                                    basePos + new Vector3(-co, a_co, co),         //back-right
                                    basePos + new Vector3(-co, a_co, -co),        //back-left
                                },
                        _ => new List<Vector3>(){
                                    basePos + new Vector3(co, co, a_co),          //front-top
                                    basePos + new Vector3(co, -co, a_co),         //front-bottom
                                    basePos + new Vector3(-co, co, a_co),         //back-top
                                    basePos + new Vector3(-co, -co, a_co),        //back-bottom
                                },
                    };

                    // Calculate offset (where triangles are in the vertices list)
                    // Add new vertices
                    int offset = vertices.Count;
                    vertices.AddRange(corners);

                    // Add UVs
                    // The end / center UV is the top-left corner of the image
                    // UVs are offset if powered
                    List<Vector2> corner_uvs = new List<Vector2>()
                    {
                        CENTER_TR, CENTER_TL, CENTER_BR, CENTER_BL
                    };
                    if (powered)
                    {
                        corner_uvs = corner_uvs.ConvertAll(ApplyOffset);
                    }
                    uv.AddRange(corner_uvs);

                    // Add new triangles
                    int[] new_triangles;
                    if (reverseArray[i])
                    {
                        new_triangles = new int[6]
                        {
                                    offset+1, offset, offset+2,
                                    offset+1, offset+2, offset+3
                        };
                    }
                    else
                    {
                        new_triangles = new int[6]
                        {
                                    offset+1, offset+2, offset,
                                    offset+1, offset+3, offset+2
                        };
                    }
                    triangles.AddRange(new_triangles);

                    // Create normal vectors (pretty straightforward) and add them
                    Vector3 normal = new Vector3(dir[0], dir[1], dir[2]);
                    for (int j = 0; j < 4; j++)
                    {
                        normals.Add(normal);
                    }
                }
            }
        }

        Mesh newMesh = new Mesh()
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            uv = uv.ToArray(),
        };
        newMesh.Optimize();
        newMesh.RecalculateNormals();
        newMesh.RecalculateBounds();

        return newMesh;
    }

    Vector2 ApplyOffset(Vector2 input)
    {
        return input + ON_OFFSET;
    }

    // Returns whether there is a wire connection on this axis
    // Assumes a wire at xyz
    bool HasConnection(int x, int y, int z, int dx, int dy, int dz)
    {
        //if (x + dx < 0 || x + dx > data.size.x - 1) return false;
        //if (y + dy < 0 || y + dy > data.size.y - 1) return false;
        //if (z + dz < 0 || z + dz > data.size.z - 1) return false;
        //return data.wires[x + dx, y + dy, z + dz] || data.connections[x + dx, y + dy, z + dz];

        int[] coords = new int[3] { x + dx, y + dy, z + dz };
        return (wm.wire_map.ContainsKey(coords) || (wm.connections.Find((c) => c[0] == coords[0] && c[1] == coords[1] && c[2] == coords[2]) != null));
    }
}
