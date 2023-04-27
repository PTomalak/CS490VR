using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/BlockDictionary", order = 1)]
[System.Serializable]
public class BlockDictionary : ScriptableObject
{
    [SerializeField]
    public List<BlockDictionaryObject> LIST;

    [System.Serializable]
    public class BlockDictionaryObject
    {
        public string block;
        public GameObject prefab;
        public Texture icon;
    }
}
