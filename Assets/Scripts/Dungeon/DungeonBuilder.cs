using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class DungeonBuilder : SingletonMonoBehavior<DungeonBuilder>
{
    public Dictionary<string, Room> dungeonBuilderRoomDictionary = new Dictionary<string, Room>();
    private Dictionary<string, RoomTemplateSO> roomTemplateDictionary = new Dictionary<string, RoomTemplateSO>();
    private List<RoomTemplateSO> roomTemplateList = null;
    private RoomNodeTypeListSO roomNodeTypeList;
    private bool dungeonBuildSuccessful;

    protected override void Awake()
    {
        base.Awake();

        //Load the room node type list
        LoadRoomNodeTypeList();

        //Set dimmer material to fully visible
        GameResources.Instance.dimmedMaterial.SetFloat("Alpha_Slider", 1f);
    }

    private void LoadRoomNodeTypeList()
    {
        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
    }

    public bool GenerateDungeon(DungeonLevelSO currentDungeonLevel)
    {
        roomTemplateList = currentDungeonLevel.roomTemplateList;

        LoadRoomTemplatesIntoDictionary();

        dungeonBuildSuccessful = false;
        int dungeonBuildAttempts = 0;

        while (!dungeonBuildSuccessful && dungeonBuildAttempts < Settings.maxDungeonBuildAttempts)
        {
            dungeonBuildAttempts++;

            //Select a random room node graph from the list
            RoomNodeGraphSO roomNodeGraph = SelectRandomRoomNodeGraph(currentDungeonLevel.roomNodeGraphList);

            int dungeonRebuildAttemptsForNodeGraph = 0;
            dungeonBuildSuccessful = false;

            //Loop until dungeon successfully built or more than max attempts for node graph
            while (!dungeonBuildSuccessful && dungeonRebuildAttemptsForNodeGraph <= Settings.maxDungeonRebuildAttemptsForRoomGraph)
            {
                ClearDungeon();

                dungeonRebuildAttemptsForNodeGraph++;

                dungeonBuildSuccessful = AttemptToBuildRandomDungeon(roomNodeGraph);
            }

            if (dungeonBuildSuccessful)
            {
                InstantiateRoomGameObjects();
            }
        }

        return dungeonBuildSuccessful;
    }

    private void LoadRoomTemplatesIntoDictionary()
    {
        roomTemplateDictionary.Clear();

        foreach(RoomTemplateSO roomTemplate in roomTemplateList)
        {
            if (!roomTemplateDictionary.ContainsKey(roomTemplate.guid))
            {
                roomTemplateDictionary.Add(roomTemplate.guid, roomTemplate);
            }
            else
            {
                Debug.Log("Duplicate Room Template Key in " + roomTemplateList);
            }
        }
    }

    //<summary>
    //Attempt to randomly build the dungeon for the specified roomNodeGraph. Returns true if a successful layout was generated,
    //else returns false if a problem was encountered and another attempt is required.
    //<summary>
    private bool AttemptToBuildRandomDungeon(RoomNodeGraphSO roomNodeGraph)
    {
        //Create Open Room node Queue
        Queue<RoomNodeSO> openRoomNodeQueue = new Queue<RoomNodeSO>();

        //AddEntrance Node to Room Node Queue From RoomNodeGraph
        RoomNodeSO entranceNode = roomNodeGraph.GetRoomNodeSO(roomNodeTypeList.list.Find(x => x.isEntrance));

        if (entranceNode != null)
        {
            openRoomNodeQueue.Enqueue(entranceNode);
        }
        else
        {
            Debug.Log("No Entrance Node");
            return false; //Failed Dungeon Build
        }

        //Start with no room overlaps
        bool noRoomOverlaps = true;

        //Process open room node queue
        noRoomOverlaps = ProcessRoomsInOpenRoomNodeQueue(roomNodeGraph, openRoomNodeQueue, noRoomOverlaps);

        //If all the room nods have been processed and there hasn't been a room overlap then return true
        if (openRoomNodeQueue.Count == 0 && noRoomOverlaps)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool ProcessRoomsInOpenRoomNodeQueue(RoomNodeGraphSO roomNodeGraph, Queue<RoomNodeSO> openRoomNodeQueue, bool noRoomOverlaps)
    {
        //While room nodes in open room node queue & no room overlaps detected
        while (openRoomNodeQueue.Count > 0 && noRoomOverlaps == true)
        {
            //Get next room node from open room node queue
            RoomNodeSO roomNode = openRoomNodeQueue.Dequeue();

            //Add child Nodes to queue from room node graph (with links to this parent Room)
            foreach(RoomNodeSO childRoomNode in roomNodeGraph.GetChildRoomNodes(roomNode))
            {
                openRoomNodeQueue.Enqueue(childRoomNode);
            }

            //If the room is the entrance mark as positioned and add to room dictionary
            if (roomNode.roomNodeType.isEntrance)
            {
                RoomTemplateSO roomTemplate = GetRandomRoomTemplate(roomNode.roomNodeType);

                Room room = CreateRoomFromRoomTemplate(roomTemplate, roomNode);

                room.isPositioned = true;

                //Add room to room Dictionary
                dungeonBuilderRoomDictionary.Add(room.id, room);
            }

            else
            {
                Room parentRoom = dungeonBuilderRoomDictionary[roomNode.parentRoomNodeIDList[0]];

                noRoomOverlaps = CanPlaceRoomWithNoOverlaps(roomNode, parentRoom);
            }
        }

        return noRoomOverlaps;
    }

    private bool CanPlaceRoomWithNoOverlaps(RoomNodeSO roomNode, Room parentRoom)
    {
        //initialize with room overlaps until proven otherwise
        bool roomOverlaps = true;

        while (roomOverlaps)
        {
            List<Doorway> unconnectedAvailableParentDoorways = GetUnconnectedAvailableDoorways(parentRoom.doorwayList).ToList();
           
            if (unconnectedAvailableParentDoorways.Count == 0)
            {
                return false; //room overlaps
            }

            Doorway doorwayParent = unconnectedAvailableParentDoorways[UnityEngine.Random.Range(0, unconnectedAvailableParentDoorways.Count)];

            //Get a random room template for room node that is consistent with the parent door orientation
            RoomTemplateSO roomTemplate = GetRandomRoomTemplateForConsistentWithParent(roomNode, doorwayParent);

            //Create a room
            Room room = CreateRoomFromRoomTemplate(roomTemplate, roomNode);

            //Place the room - room returns true if the room doesn't overlap
            if (PlaceTheRoom(parentRoom, doorwayParent, room))
            {
                //If room doesn't overlap then set to false to exit while loop
                roomOverlaps = false;

                //Mark room as positioned
                room.isPositioned = true;

                //Add room to dictionary
                dungeonBuilderRoomDictionary.Add(room.id, room);
            }
            else
            {
                roomOverlaps = true;
            }
        }

        return true; // no room overlaps
    }

    //<summary>
    //Get a random room template for room node taking into account the parent doorway orientation
    //<summary>
    private RoomTemplateSO GetRandomRoomTemplateForConsistentWithParent(RoomNodeSO roomNode, Doorway doorwayParent)
    {
        RoomTemplateSO roomTemplate = null;

        //If room node is a corridor then select random correct Corridor room template based on parent doorway orientation
        if (roomNode.roomNodeType.isCorridor)
        {
            switch (doorwayParent.orientation)
            {
                case Orientation.north:
                case Orientation.south:
                    roomTemplate = GetRandomRoomTemplate(roomNodeTypeList.list.Find(x => x.isCorridorNS));
                    break;

                case Orientation.east:
                case Orientation.west:
                    roomTemplate = GetRandomRoomTemplate(roomNodeTypeList.list.Find(x => x.isCorridorEW));
                    break;

                case Orientation.none:
                    break;
                
                default:
                    break;
            }
        }
        //Else select random room template
        else
        {
            roomTemplate = GetRandomRoomTemplate(roomNode.roomNodeType);
        }

        return roomTemplate;
    }

    //<summary>
    //Place the room - returns true if the room doesn't overlap, false otherwise
    //<summary>
    private bool PlaceTheRoom(Room parentRoom, Doorway doorwayParent, Room room)
    {
        Doorway doorway = GetOppositeDoorway(doorwayParent, room.doorwayList);

        if (doorway == null)
        {
            //Just mark the parent doorway as unavailable so we don't try and connect it again
            doorwayParent.isUnavailable = true;

            return false;
        }

        //Calculate world grid parent doorway position
        Vector2Int parentDoorwayPosition = parentRoom.lowerBounds + doorwayParent.position - parentRoom.templateLowerBounds;

        Vector2Int adjustment = Vector2Int.zero;

        //Calculate adjustment position offset based on room doorway position that we are trying to connect (e.g. if this doorway is west
        //then we need to add (1,0) to the east parent doorway

        switch (doorway.orientation)
        {
            case Orientation.north:
                adjustment = new Vector2Int(0, -1);
                break;

            case Orientation.east:
                adjustment = new Vector2Int(-1, 0);
                break;

            case Orientation.south:
                adjustment = new Vector2Int(0, 1);
                break;

            case Orientation.west:
                adjustment = new Vector2Int(1, 0);
                break;

            case Orientation.none:
                break;

            default:
                break;
        }

        //Calculate room lower bounds and upper bounds based on positioning to laign with parent doorway
        room.lowerBounds = parentDoorwayPosition + adjustment + room.templateLowerBounds - doorway.position;
        room.upperBounds = room.lowerBounds + room.templateUpperBounds - room.templateLowerBounds;

        Room overlappingRoom = CheckForRoomOverlap(room);

        if (overlappingRoom == null)
        {
            //mark doorways as connected and unavailable
            doorwayParent.isConnected = true;
            doorwayParent.isUnavailable = true;

            doorway.isConnected = true;
            doorway.isUnavailable = true;

            //return true to show rooms have been connected with no overlap
            return true;
        }
        else
        {
            doorwayParent.isUnavailable = true;

            return false;
        }
    }

    //<summary>
    // Get the doorway from the doorway list that has the opposite orientation to doorway
    //<summary>
    private Doorway GetOppositeDoorway(Doorway parentDoorway, List<Doorway> doorwayList)
    {
        foreach(Doorway doorwayToCheck in doorwayList)
        {
            if (parentDoorway.orientation == Orientation.east && doorwayToCheck.orientation == Orientation.west)
            {
                return doorwayToCheck;
            }
            else if (parentDoorway.orientation == Orientation.west && doorwayToCheck.orientation == Orientation.east)
            {
                return doorwayToCheck;
            }
            else if (parentDoorway.orientation == Orientation.north && doorwayToCheck.orientation == Orientation.south)
            {
                return doorwayToCheck;
            }
            else if (parentDoorway.orientation == Orientation.south && doorwayToCheck.orientation == Orientation.north)
            {
                return doorwayToCheck;
            }
        }

        return null;
    }

    private Room CheckForRoomOverlap(Room roomToTest)
    {
        foreach(KeyValuePair<string, Room> keyValuePair in dungeonBuilderRoomDictionary)
        {
            Room room = keyValuePair.Value;

            //skip if same room as room to test or room hasn't been posiitoned
            if (room.id == roomToTest.id || !room.isPositioned)
                continue;

            //If room overlaps
            if(IsOverlappingRoom(roomToTest, room))
            {
                return room;
            }
        }

        return null;
    }

    //<summary>
    //Check if 2 rooms overlap each other - return true if they overlap or false if they don't overlap
    //<summary>
    private bool IsOverlappingRoom(Room room1, Room room2)
    {
        bool isOverLappingX = IsOverlappingInterval(room1.lowerBounds.x, room1.upperBounds.x, room2.lowerBounds.x, room2.upperBounds.x);
        bool isOverLappingY = IsOverlappingInterval(room1.lowerBounds.y, room1.upperBounds.y, room2.lowerBounds.y, room2.upperBounds.y);

        if (isOverLappingX && isOverLappingY)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    //<summary>
    //Check if inerval 1 overlaps interval 2 - this method is used by the IsOverlappingRoom method
    //<summary>
    private bool IsOverlappingInterval(int imin1, int imax1, int imin2, int imax2)
    {
        if (Mathf.Max(imin1, imin2) <= Mathf.Min(imax1, imax2))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private RoomTemplateSO GetRandomRoomTemplate(RoomNodeTypeSO roomNodeType)
    {
        List<RoomTemplateSO> matchingRoomTemplateList = new List<RoomTemplateSO>();

        foreach(RoomTemplateSO roomTemplate in roomTemplateList)
        {
            //Add matching room Templates
            if(roomTemplate.roomNodeType == roomNodeType)
            {
                matchingRoomTemplateList.Add(roomTemplate);
            }
        }

        if (matchingRoomTemplateList.Count == 0)
            return null;

        return matchingRoomTemplateList[UnityEngine.Random.Range(0, matchingRoomTemplateList.Count)];
    }

    private IEnumerable<Doorway> GetUnconnectedAvailableDoorways(List<Doorway> roomDoorwayList)
    {
        //Loop through doorway List
        foreach(Doorway doorway in roomDoorwayList)
        {
            if (!doorway.isConnected && !doorway.isUnavailable)
                yield return doorway;
        }
    }

    private Room CreateRoomFromRoomTemplate(RoomTemplateSO roomTemplate, RoomNodeSO roomNode)
    {
        Room room = new Room();

        room.templateID = roomTemplate.guid;
        room.id = roomNode.id;
        room.prefab = roomTemplate.prefab;
        room.roomNodeType = roomTemplate.roomNodeType;
        room.lowerBounds = roomTemplate.lowerBounds;
        room.upperBounds = roomTemplate.upperBounds;
        room.spawnPositionArray = roomTemplate.spawnPositionArray;
        room.templateLowerBounds = roomTemplate.lowerBounds;
        room.templateUpperBounds = roomTemplate.upperBounds;
        room.childRoomIDList = CopyStringList(roomNode.childRoomNodeIDList);
        room.doorwayList = CopyDoorwayList(roomTemplate.doorwayList);

        if (roomNode.parentRoomNodeIDList.Count == 0) //Entrance
        {
            room.parentRoomID = "";
            room.isPreviouslyVisited = true;
        }
        else
        {
            room.parentRoomID = roomNode.parentRoomNodeIDList[0];
        }

        return room;
    }

    private RoomNodeGraphSO SelectRandomRoomNodeGraph(List<RoomNodeGraphSO> roomNodeGraphList)
    {
        if (roomNodeGraphList.Count > 0)
        {
            return roomNodeGraphList[UnityEngine.Random.Range(0, roomNodeGraphList.Count)];
        }
        else
        {
            Debug.Log("no room node graphs in List");
            return null;
        }
    }

    //<summary>
    //Create deep copy of Doorway list
    //<summary>
    private List<Doorway> CopyDoorwayList(List<Doorway> oldDoorwayList)
    {
        List<Doorway> newDoorwayList = new List<Doorway>();

        foreach(Doorway doorway in oldDoorwayList)
        {
            Doorway newDoorway = new Doorway();

            newDoorway.position = doorway.position;
            newDoorway.orientation = doorway.orientation;
            newDoorway.doorPrefab = doorway.doorPrefab;
            newDoorway.isConnected = doorway.isConnected;
            newDoorway.isUnavailable = doorway.isUnavailable;
            newDoorway.doorwayStartCopyPosition = doorway.doorwayStartCopyPosition;
            newDoorway.doorwayCopyTileWidth = doorway.doorwayCopyTileWidth;
            newDoorway.doorwayCopyTileHeight = doorway.doorwayCopyTileHeight;

            newDoorwayList.Add(newDoorway);
        }

        return newDoorwayList;
    }

    //<summary>
    //Create deep copy of the list
    //<summary>
    private List<string> CopyStringList(List<string> oldStringList)
    {
        List<string> newStringList = new List<string>();

        foreach (string stringValue in oldStringList)
        {
            newStringList.Add(stringValue);
        }
        return newStringList;
    }

    //<summary>
    //Instantiate the dungeon room gameobjects from the prefabs
    //</summary>
    private void InstantiateRoomGameObjects()
    {
        //Iterate through all dungeon rooms
        foreach (KeyValuePair<string, Room> keyvaluepair in dungeonBuilderRoomDictionary)
        {
            Room room = keyvaluepair.Value;

            //Calculate room position (remember the room instantiation posiiton needs to be adjusted by the room template lower bounds)
            Vector3 roomPosition = new Vector3(room.lowerBounds.x - room.templateLowerBounds.x, room.lowerBounds.y - room.templateLowerBounds.y, 0f);
            
            //Instantiate room
            GameObject roomGameObject = Instantiate(room.prefab, roomPosition, Quaternion.identity, transform);

            //Get instantiate room component from instantiated prefab
            InstantiatedRoom instantiatedRoom = roomGameObject.GetComponentInChildren<InstantiatedRoom>();

            instantiatedRoom.room = room;

            //Initialize the Instantiated Room
            instantiatedRoom.Initialize(roomGameObject);

            room.instantiatedRoom = instantiatedRoom;
        }
    }

    //<summary>
    //Get a room template by room template ID, returns null if ID doesn't exist
    //<summary>
    public RoomTemplateSO GetRoomTemplate(string roomTemplateID)
    {
        if (roomTemplateDictionary.TryGetValue(roomTemplateID, out RoomTemplateSO roomTemplate))
        {
            return roomTemplate;
        }
        else
        {
            return null;
        }
    }

    //<summary>
    //Get room by roomID, if no room exists with that ID return null
    //<summary>
    public Room GetRoomByRoomID(string roomID)
    {
        if(dungeonBuilderRoomDictionary.TryGetValue(roomID, out Room room))
        {
            return room;
        }
        else
        {
            return null;
        }
    }
    //<summary>
    //Clear dungeon room game objects and dungeon room dictionary
    //<summary>
    private void ClearDungeon()
    {
        if (dungeonBuilderRoomDictionary.Count > 0)
        {
            foreach (KeyValuePair<string, Room> keyValuePair in dungeonBuilderRoomDictionary)
            {
                Room room = keyValuePair.Value;

                if (room.instantiatedRoom != null)
                {
                    Destroy(room.instantiatedRoom.gameObject);
                }
            }

            dungeonBuilderRoomDictionary.Clear();
        }
    }
}
