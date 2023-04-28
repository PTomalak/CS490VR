using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/PoweredTextures", order = 1)]
public class PoweredTextures : ScriptableObject
{
    // Two materials (textures) for whether it is on or off
    [SerializeField]
    public Material ON_MATERIAL;
    [SerializeField]
    public Material OFF_MATERIAL;
}
