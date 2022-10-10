using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameResources : MonoBehaviour
{
    private static GameResources instance;

    public static GameResources Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<GameResources>("GameResources");
            }
            return instance;
        }
    }

    #region Header Dungeon
    [Space(10)]
    [Header("DUNGEON")]
    #endregion
    #region ToolTip
    [Tooltip("Populate with the dungeon RoomNodeTypeListSO")]
    #endregion

    public RoomNodeTypeListSO roomNodeTypeList;

    //#region Header PLAYER
    //[Space(10)]
    //[Header("PLAYER")]
    //#endregion Header PLAYER
    //#region Tooltip
    //[Tooltip("The current player scriptable object - this is used to reference the current player between scenes")]
    //#endregion
    //public CurrentPlayerSO currentPlayer;

    #region Header MATERIALS
    [Space(10)]
    [Header("Materials")]
    #endregion
    #region Header Tooltip
    [Tooltip("Dimmed Material")]
    #endregion
    public Material dimmedMaterial;
}
