using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDataLoader
{
    public virtual void Load() {}

    public virtual void SetData(object o) {}
}
