using System;
using UnityEngine;

[Serializable]
public class PlayerData
{
    public bool hasCheckpoint;
    public string sceneName;
    public float checkpointPositionX;
    public float checkpointPositionY;
    public float checkpointPositionZ;

    public Vector3 GetCheckpointPosition()
    {
        return new Vector3(checkpointPositionX, checkpointPositionY, checkpointPositionZ);
    }

    public void SetCheckpointPosition(Vector3 position)
    {
        checkpointPositionX = position.x;
        checkpointPositionY = position.y;
        checkpointPositionZ = position.z;
    }
}