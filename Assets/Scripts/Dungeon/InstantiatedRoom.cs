using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class InstantiatedRoom : MonoBehaviour
{
    [HideInInspector] public Room room;
    [HideInInspector] public Grid grid;
    [HideInInspector] public Tilemap groundTilemap;
    [HideInInspector] public Tilemap decoration1Tilemap;
    [HideInInspector] public Tilemap decoration2Tilemap;
    [HideInInspector] public Tilemap frontTilemap;
    [HideInInspector] public Tilemap collisionTilemap;
    [HideInInspector] public Tilemap minimapTilemap;
    [HideInInspector] public Bounds roomColliderBounds;

    private BoxCollider2D boxCollider2D;

    private void Awake()
    {
        boxCollider2D = GetComponent<BoxCollider2D>();

        //Save room collider bounds
        roomColliderBounds = boxCollider2D.bounds;
    }

    //<summary>
    //Initialize The instantiated Room
    //<summary>
    public void Initialize(GameObject roomGameObject)
    {
        PopulateTilemapMemberVariable(roomGameObject);

        BlockOffUnusedDoorways();

        DisableCollisionTilemapRenderer();
    }

    private void PopulateTilemapMemberVariable(GameObject roomGameObject)
    {
        //Get Grid component
        grid = roomGameObject.GetComponentInChildren<Grid>();

        //Get Tilemaps in children
        Tilemap[] tilemaps = roomGameObject.GetComponentsInChildren<Tilemap>();

        foreach(Tilemap tilemap in tilemaps)
        {
            if (tilemap.gameObject.tag == "groundTilemap")
            {
                groundTilemap = tilemap;
            }
            else if (tilemap.gameObject.tag == "decoration1Tilemap")
            {
                decoration1Tilemap = tilemap;
            }
            else if (tilemap.gameObject.tag == "decoration2Tilemap")
            {
                decoration2Tilemap = tilemap;
            }
            else if (tilemap.gameObject.tag == "frontTilemap")
            {
                frontTilemap = tilemap;
            }
            else if (tilemap.gameObject.tag == "collisionTilemap")
            {
                collisionTilemap = tilemap;
            }
            else if (tilemap.gameObject.tag == "minimapTilemap")
            {
                minimapTilemap = tilemap;
            }
        }
    }

    //<summary>
    //Block Off Unused Doorways in Room
    //<summary>
    private void BlockOffUnusedDoorways()
    {
        //Loop through all doorways
        foreach(Doorway doorway in room.doorwayList)
        {
            if (doorway.isConnected)
                continue;

            //Block unconnected doorways using tiles on tilemaps
            if(collisionTilemap != null)
            {
                BlockADoorwayOnTileMapLayer(collisionTilemap, doorway);
            }

            if (minimapTilemap != null)
            {
                BlockADoorwayOnTileMapLayer(minimapTilemap, doorway);
            }

            if(groundTilemap != null)
            {
                BlockADoorwayOnTileMapLayer(groundTilemap, doorway);
            }

            if(decoration1Tilemap != null)
            {
                BlockADoorwayOnTileMapLayer(decoration1Tilemap, doorway);
            }

            if (decoration2Tilemap != null)
            {
                BlockADoorwayOnTileMapLayer(decoration2Tilemap, doorway);
            }

            if (frontTilemap != null)
            {
                BlockADoorwayOnTileMapLayer(frontTilemap, doorway);
            }
        }
    }

    private void BlockADoorwayOnTileMapLayer(Tilemap tilemap, Doorway doorway)
    {
        switch (doorway.orientation)
        {
            case Orientation.north:
            case Orientation.south:
                BlockDoorwayHorizontally(tilemap, doorway);
                break;

            case Orientation.east:
            case Orientation.west:
                BlockDoorwayVertically(tilemap, doorway);
                break;

            case Orientation.none:
                break;
            
            default:
                break;
        }
    }

    //<summary>
    //Block Doorways Horizontally - For North South Doorways
    //<summary>
    private void BlockDoorwayHorizontally(Tilemap tilemap, Doorway doorway)
    {
        Vector2Int startPosition = doorway.doorwayStartCopyPosition;
        
        //loop through all tiles to be copied
        for(int xPos = 0; xPos < doorway.doorwayCopyTileWidth; xPos++)
        {
            for (int yPos = 0; yPos < doorway.doorwayCopyTileHeight; yPos++)
            {
                //Get rotation of tile being copied
                Matrix4x4 transformMatrix = tilemap.GetTransformMatrix(new Vector3Int(startPosition.x + xPos, startPosition.y - yPos, 0));

                //Copy tile
                tilemap.SetTile(new Vector3Int(startPosition.x + 1 + xPos, startPosition.y - yPos, 0),
                    tilemap.GetTile(new Vector3Int(startPosition.x + xPos, startPosition.y - yPos, 0)));

                //Set rotation of tile copied
                tilemap.SetTransformMatrix(new Vector3Int(startPosition.x + 1 + xPos, startPosition.y - yPos, 0), transformMatrix);
            }
        }
    }
    
    //<summary>
    //Block Doorways Vertically - For East West Doorways
    //<summary>
    private void BlockDoorwayVertically(Tilemap tilemap, Doorway doorway)
    {
        Vector2Int startPosition = doorway.doorwayStartCopyPosition;

        //loop through all tiles to be copied
        for(int yPos = 0; yPos < doorway.doorwayCopyTileHeight; yPos++)
        {
            for (int xPos = 0; xPos < doorway.doorwayCopyTileWidth; xPos++)
            {
                //Get rotation of the tile being copied
                Matrix4x4 transformMatrix = tilemap.GetTransformMatrix(new Vector3Int(startPosition.x + xPos, startPosition.y - yPos, 0));

                //Copy tile
                tilemap.SetTile(new Vector3Int(startPosition.x + xPos, startPosition.y - 1 - yPos, 0),
                    tilemap.GetTile(new Vector3Int(startPosition.x + xPos, startPosition.y - yPos, 0)));

                //Set rotation of tile copied
                tilemap.SetTransformMatrix(new Vector3Int(startPosition.x + xPos, startPosition.y - 1 - yPos, 0), transformMatrix);
            }
        }
    }

    private void DisableCollisionTilemapRenderer()
    {
        collisionTilemap.gameObject.GetComponent<TilemapRenderer>().enabled = false;
    }
}
