using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomNodeTypeListSO", menuName = "Scriptable Objects/Dungeon/Room Node Type List")]
public class RoomNodeTypeListSO : ScriptableObject
{
    #region Header ROOM NODE TYPE LIST
    [Header("ROOM NODE TYPE LIST")]
    #endregion
    #region Tooltip
    [Tooltip("This list should be populated with all the RoomNodeTypeSo for the game - it is used instead of an enum")]
    #endregion
    public List<RoomNodeTypeSO> list;

    #region
#if UNITY_EDITOR //Only runs in the Unity Editor
    private void OnValidate() //Editor-only function that Unity calls when the script is loaded or a value changes in the inspector
    {
        HelperUtilities.ValidateCheckEnumerableValues(this, nameof(list), list);
    }
#endif
    #endregion
}
