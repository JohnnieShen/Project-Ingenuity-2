using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BlockSaveData
{
    public string blockName;
    public Vector3 localPosition;
    public Quaternion localRotation;
}

[System.Serializable]
public class VehicleSaveData
{
    public string vehicleName;
    public List<BlockSaveData> blocks = new List<BlockSaveData>();
}