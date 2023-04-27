using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class UVGenerator : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        Mesh mesh = null;
        if (mf != null)
            mesh = mf.mesh;

        if (mesh == null || mesh.uv.Length != 24)
        {
            Debug.Log("Script needs to be attached to built-in cube");
            return;
        }

        Vector2 BL = new Vector2(0, 0);
        Vector2 TL = new Vector2(0, 1);
        Vector2 BR = new Vector2(1, 0);
        Vector2 TR = new Vector2(1, 1);

        var uvs = mesh.uv;

        string uv_list = "";
        foreach (Vector2 uv in mesh.uv)
        {
            string corner = "";
            corner += (uv.y == 0) ? "T" : "B";
            corner += (uv.x == 0) ? "L" : "R";
            uv_list += corner + "\n";
        }

        Vector2[] UVs = new Vector2[mesh.vertices.Length];
        // Front
        UVs[0] = UV(16, 8);      //BL
        UVs[1] = UV(24, 8);      //BR
        UVs[2] = UV(16, 16);      //TL
        UVs[3] = UV(24, 16);      //TR
        // Top
        UVs[4] = UV(16, 16);      //BL
        UVs[5] = UV(16, 8);     //BR
        UVs[8] = UV(0, 16);      //TL
        UVs[9] = UV(0, 8);     //TR
        // Back
        UVs[6] = UV(24, 0);     //BR
        UVs[7] = UV(16, 0);     //BL
        UVs[10] = UV(24, 8);    //TR
        UVs[11] = UV(16, 8);    //TL
        // Bottom
        UVs[12] = UV(16, 16);      //BL
        UVs[13] = UV(0, 16);     //TL
        UVs[14] = UV(0, 8);     //TR
        UVs[15] = UV(16, 8);      //BR
        // Left
        UVs[16] = UV(0,0);     //BL
        UVs[17] = UV(0, 8);    //TL
        UVs[18] = UV(16, 8);   //TR
        UVs[19] = UV(16, 0);    //BR
        // Right        
        UVs[20] = UV(16,0);     //BL
        UVs[21] = UV(16,8);    //TL
        UVs[22] = UV(0,8);    //TR
        UVs[23] = UV(0,0);     //BR
        mesh.uv = UVs;

        //#if UNITY_EDITOR
        //AssetDatabase.CreateAsset(mesh, "Assets/CS490VR/Meshes/Diode.asset");
        //AssetDatabase.SaveAssets();
        //#endif
    }
    
    Vector2 UV(float x, float y)
    {
        int w = GetComponent<MeshRenderer>().material.mainTexture.width;
        int h = GetComponent<MeshRenderer>().material.mainTexture.height;

        return new Vector2(x / w, y / h);
    }
}
