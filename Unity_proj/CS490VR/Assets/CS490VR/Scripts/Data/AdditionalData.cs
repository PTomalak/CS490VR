using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public class AdditionalData
{
    public string block;

    public object data;

    public AdditionalData(string block, object data)
    {
        this.block = block;
        this.data = data;
    }

    // Different types of extra block data
    public class Powered
    {
        public bool powered = false;
    }
    public class Memory : Powered
    {
        public bool stored = false;
    }
    public class Pulse : Powered
    {
        public int start_tick = 0;
        public int pulse_ticks = 10;
    }
    public class PulseLatch : Powered
    {
        public int pulse_battery = 0;
        public int pulse_ticks = 10;
    }
    public class Clock : Powered
    {
        public int start_tick = 0;
        public int rate = 10;
    }

    public static Powered GetPoweredData(object o)
    {
        return JsonConvert.DeserializeObject<Powered>(JsonConvert.SerializeObject(o));
    }
}
