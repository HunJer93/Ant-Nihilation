using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenuAttribute(fileName = "RoomNodeType_", menuName = "Scriptable Objects/Dungeon/Room Node Type")]
public class RoomNodeTypeSO : ScriptableObject
{
    public string roomNodeTypeName;

    #region Header
    [Header("Only flag the RoomNodeTypes that should be visible in the editor")]
    #endregion Header
    public bool displayInNodeGraphEditor = true;
    #region Header
    [Header("One Type should be a corridor")]
    #endregion Header
    public bool isCorridor;
    #region Header
    [Header("One Type Should be a corridorNS")] //North South
    #endregion Header
    public bool isCorridorNS;
    #region Header
    [Header("One Type should be a corridorEW")] //East West
    #endregion Header
    public bool isCorridorEW;
    #region Header
    [Header("One Type should be an Entrance")] //Will be created by default
    #endregion Header
    public bool isEntrance;
    #region Header
    [Header("One Type should be a Boss Room")]
    #endregion Header
    public bool isBossRoom;
    #region Header
    [Header("One Type should be None")] //default rooms with nothing special in them
    #endregion Header
    public bool isNone;

    #region Validation
#if UNITY_EDITOR //Only runs in the Unity Editor
    private void OnValidate() //Editor-only function that Unity calls when the script is loaded or a value changes in the inspector
    {
        HelperUtilities.ValidateCheckEmptyString(this, nameof(roomNodeTypeName), roomNodeTypeName);
    }
#endif
    #endregion
}
