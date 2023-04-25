using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class IData
{
    public virtual IData GetDefaultState() {
        return new IData();
    }
}
