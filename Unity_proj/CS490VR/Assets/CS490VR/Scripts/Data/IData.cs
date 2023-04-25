using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class IData
{
    public virtual object getDefaultState() {
        return new IData();
    }
}
