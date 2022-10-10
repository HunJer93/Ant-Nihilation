using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Callbacks;
using UnityEditor;

public class RoomNodeGraphEditor : EditorWindow
{
    private GUIStyle roomNodeStyle;
    private GUIStyle roomNodeSelectedStyle;
    private static RoomNodeGraphSO currentRoomNodeGraph;

    private Vector2 graphOffset;
    private Vector2 graphDrag;

    private RoomNodeSO currentRoomNode = null;
    private RoomNodeTypeListSO roomNodeTypeList;

    //Node layout values
    private const float nodeWidth = 160f;
    private const float nodeHeight = 75f;
    private const int nodePadding = 25;
    private const int nodeBorder = 12;

    //Connecting Line values
    private const float connectingLineWidth = 3f;
    private const float connectingLineArrowSize = 6f;

    //Grid Spacing
    private const float gridLarge = 100f;
    private const float gridSmall = 25f;

    [MenuItem("Room Node Graph Editor", menuItem = "Window/Dungeon Editor/Room Node Graph Editor")]
    private static void OpenWindow()
    {
        GetWindow<RoomNodeGraphEditor>("Room Node Graph Editor");
    }

    private void OnEnable()
    {
        //Subscribe to the inspector selection changed event
        Selection.selectionChanged += InspectorSelectionChanged;

        //Define node layout style
        roomNodeStyle = new GUIStyle();
        roomNodeStyle.normal.background = EditorGUIUtility.Load("node1") as Texture2D;
        roomNodeStyle.normal.textColor = Color.white;
        roomNodeStyle.padding = new RectOffset(nodePadding, nodePadding, nodePadding, nodePadding);
        roomNodeStyle.border = new RectOffset(nodeBorder, nodeBorder, nodeBorder, nodeBorder);

        //Define selected node style
        roomNodeSelectedStyle = new GUIStyle();
        roomNodeSelectedStyle.normal.background = EditorGUIUtility.Load("node1 on") as Texture2D;
        roomNodeSelectedStyle.normal.textColor = Color.white;
        roomNodeSelectedStyle.padding = new RectOffset(nodePadding, nodePadding, nodePadding, nodePadding);
        roomNodeSelectedStyle.border = new RectOffset(nodeBorder, nodeBorder, nodeBorder, nodeBorder);

        //Load room node types
        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= InspectorSelectionChanged;
    }

    //<summary>
    //Open the room node graph editor window if a room node graph scriptable object asset is double clicked in the inspector
    //<summary>
    [OnOpenAsset(0)] //Needs the namespace using UnityEditor.Callbacks
    public static bool OnDoubleClickAsset(int instanceID, int line)
    {
        RoomNodeGraphSO roomNodeGraph = EditorUtility.InstanceIDToObject(instanceID) as RoomNodeGraphSO;

        if (roomNodeGraph != null)
        {
            OpenWindow();

            currentRoomNodeGraph = roomNodeGraph;

            return true;
        }
        return false;
    }

    //<summary>
    //Draw Editor GUI
    //<summary>
    private void OnGUI()
    {
        //if a scriptable object of type RoomNodeGraphSO has been selected then process
        if (currentRoomNodeGraph != null)
        {
            //Draw Grid
            DrawBackgroundGrid(gridSmall, 0.2f, Color.gray);
            DrawBackgroundGrid(gridLarge, 0.3f, Color.gray);

            //Draw line if being dragged
            DrawDraggedLine();

            ProcessEvents(Event.current);

            //Draw Connections between room nodes
            DrawRoomNodeConnections();

            DrawRoomNodes();
        }

        if (GUI.changed)
            Repaint();
    }

    //<summary>
    //Draw a background grid for the room node graph editor
    //<summary>
    private void DrawBackgroundGrid(float gridSize, float gridOpacity, Color gridColor)
    {
        int verticalLineCount = Mathf.CeilToInt((position.width + gridSize) / gridSize);
        int horizontalLineCount = Mathf.CeilToInt((position.height + gridSize) / gridSize);
        
        Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

        graphOffset += graphDrag * 0.5f;

        Vector3 gridOffset = new Vector3(graphOffset.x % gridSize, graphOffset.y % gridSize, 0);

        for (int i = 0; i < verticalLineCount; i++)
        {
            Handles.DrawLine(new Vector3(gridSize * i, -gridSize, 0) + gridOffset, 
                             new Vector3(gridSize * i, position.height + gridSize, 0f) + gridOffset);
        }

        for (int j = 0; j < horizontalLineCount; j++)
        {
            Handles.DrawLine(new Vector3(-gridSize, gridSize * j, 0) + gridOffset,
                             new Vector3(position.width + gridSize, gridSize * j, 0f) + gridOffset);
        }

        Handles.color = Color.white;
    }

    private void DrawDraggedLine()
    {
        if (currentRoomNodeGraph.linePosition != Vector2.zero)
        {
            //Draw line from node to line position
            Handles.DrawBezier(currentRoomNodeGraph.roomNodeToDrawLineFrom.rect.center, currentRoomNodeGraph.linePosition,
                currentRoomNodeGraph.roomNodeToDrawLineFrom.rect.center, currentRoomNodeGraph.linePosition, 
                Color.white, null, connectingLineWidth);
        }
    }

    private void ProcessEvents(Event currentEvent)
    {
        //Reset graph drag
        graphDrag = Vector2.zero;

        //Get room node that mouse is over if it's null or not currently being dragged
        if (currentRoomNode == null || currentRoomNode.IsLeftClickDragging == false)
        {
            currentRoomNode = IsMouseOverRoomNode(currentEvent);
        }

        //if mouse isn't over a room node
        if (currentRoomNode == null || currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            ProcessRoomNodeGraphEvents(currentEvent);
        }
        else
        {
            currentRoomNode.ProcessEvents(currentEvent);
        }
    }

    //<summary>
    //Checks whether mouse is over a room node, if so then return the room node else return null
    //<summary>
    private RoomNodeSO IsMouseOverRoomNode(Event currentEvent)
    {
        for (int i = currentRoomNodeGraph.roomNodeList.Count - 1; i >=0; i--)
        {
            if (currentRoomNodeGraph.roomNodeList[i].rect.Contains(currentEvent.mousePosition))
            {
                return currentRoomNodeGraph.roomNodeList[i];
            }
        }

        return null;
    }

    //<summary>
    //Process Room Node Graph Events
    //<summary>
    private void ProcessRoomNodeGraphEvents(Event currentEvent)
    {
        switch (currentEvent.type)
        {
            //Process Mouse Down Events
            case EventType.MouseDown:
                ProcessMouseDownEvent(currentEvent);
                break;
            case EventType.MouseDrag:
                ProcessMouseDragEvent(currentEvent);
                break;
            case EventType.MouseUp:
                ProcessMouseUpEvent(currentEvent);
                break;
            default:
                break;
        }
    }

    //<summary>
    //Process Mouse Down events on the room node graph (not over a node)
    //<summary>
    private void ProcessMouseDownEvent(Event currentEvent)
    {
        if (currentEvent.button == 1)
        {
            ShowContextMenu(currentEvent.mousePosition);
        }
        else if (currentEvent.button == 0)
        {
            ClearLineDrag();
            ClearAllSelectedRoomNodes();
        }
    }

    //<summary>
    //Show Context Menu
    //<summary>
    private void ShowContextMenu(Vector2 mousePosition)
    {
        GenericMenu menu = new GenericMenu();

        menu.AddItem(new GUIContent("Create Room Node"), false, CreateRoomNode, mousePosition);
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Select All Room Nodes"), false, SelectAllRoomNodes);
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Delete Selected Room Node Links"), false, DeleteSelectedRoomNodeLinks);
        menu.AddItem(new GUIContent("Delete Selected Room Nodes"), false, DeleteSelectedRoomNodes);

        menu.ShowAsContext();
    }

    //<summary>
    //Creat a room node at the mouse position
    //<summary>
    private void CreateRoomNode(object mousePositionObject)
    {
        if (currentRoomNodeGraph.roomNodeList.Count == 0)
        {
            CreateRoomNode(new Vector2(200f, 200f), roomNodeTypeList.list.Find(x => x.isEntrance));
        }

        CreateRoomNode(mousePositionObject, roomNodeTypeList.list.Find(x => x.isNone));
    }

    //<summary>
    //Create a room node at the mouse position - overloaded to also pass in RoomNodeType
    //<summary>
    private void CreateRoomNode(object mousePositionObject, RoomNodeTypeSO roomNodeType)
    {
        Vector2 mousePosition = (Vector2)mousePositionObject;

        //creat a room node scriptable object asset
        RoomNodeSO roomNode = ScriptableObject.CreateInstance<RoomNodeSO>();

        //add a room node to current room node graph room node list
        currentRoomNodeGraph.roomNodeList.Add(roomNode);

        //set room node values
        roomNode.Initialize(new Rect(mousePosition, new Vector2(nodeWidth, nodeHeight)), currentRoomNodeGraph, roomNodeType);

        //add a room node to room node graph scriptable object
        AssetDatabase.AddObjectToAsset(roomNode, currentRoomNodeGraph);

        AssetDatabase.SaveAssets();

        //Refresh graph node Dictionary
        currentRoomNodeGraph.OnValidate();
    }

    //<summary>
    //Delete Selected room nodes
    //<summary<
    private void DeleteSelectedRoomNodes()
    {
        Queue<RoomNodeSO> roomNodeDeletionQueue = new Queue<RoomNodeSO>();

        //Loop through all nodes
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (roomNode.IsSelected && !roomNode.roomNodeType.isEntrance)
            {
                roomNodeDeletionQueue.Enqueue(roomNode);

                //iterate through child room nodes IDs
                foreach (string childRoomNodeID in roomNode.childRoomNodeIDList)
                {
                    //Retrieve child room node
                    RoomNodeSO childRoomNode = currentRoomNodeGraph.GetRoomNode(childRoomNodeID);

                    if (childRoomNode != null)
                    {
                        //Remove parentID from child room node
                        childRoomNode.RemoveParentRoomNodeIDFromRoomNode(roomNode.id);
                    }
                }

                //iterate through parent room node IDs
                foreach (string parentRoomNodeID in roomNode.parentRoomNodeIDList)
                {
                    //Retrieve parent room node
                    RoomNodeSO parentRoomNode = currentRoomNodeGraph.GetRoomNode(parentRoomNodeID);

                    if (parentRoomNodeID != null)
                    {
                        //Remove Child ID from parent room node
                        parentRoomNode.RemoveChildRoomNodeIDFromRoomNode(roomNode.id);
                    }
                }
            }
        }

        //Delete qued room nodes
        while (roomNodeDeletionQueue.Count > 0)
        {
            //Get room node from queue
            RoomNodeSO roomNodeToDelete = roomNodeDeletionQueue.Dequeue();

            //Remove node from dictionary
            currentRoomNodeGraph.roomNodeDictionary.Remove(roomNodeToDelete.id);

            //Remove node from list
            currentRoomNodeGraph.roomNodeList.Remove(roomNodeToDelete);

            //Remove node from Asset database
            DestroyImmediate(roomNodeToDelete, true);

            //Save asset database
            AssetDatabase.SaveAssets();
        }
    }

    private void DeleteSelectedRoomNodeLinks()
    {
        //Iterate through all room nodes
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (roomNode.IsSelected && roomNode.childRoomNodeIDList.Count > 0)
            {
                for (int i = roomNode.childRoomNodeIDList.Count - 1; i >= 0; i--)
                {
                    //Get Child room node
                    RoomNodeSO childRoomNode = currentRoomNodeGraph.GetRoomNode(roomNode.childRoomNodeIDList[i]);

                    //If the child room node is selected
                    if (childRoomNode != null && childRoomNode.IsSelected)
                    {
                        //Remove childID from parent room node
                        roomNode.RemoveChildRoomNodeIDFromRoomNode(childRoomNode.id);

                        //Remove parentId from child room node
                        roomNode.RemoveParentRoomNodeIDFromRoomNode(roomNode.id);
                    }
                }
            }
        }
    }

    private void ClearAllSelectedRoomNodes()
    {
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (roomNode.IsSelected)
            {
                roomNode.IsSelected = false;

                GUI.changed = true;
            }
        }
    }

    private void SelectAllRoomNodes()
    {
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            roomNode.IsSelected = true;
        }
        GUI.changed = true;
    }
    private void ProcessMouseUpEvent(Event currentEvent)
    {
        //if releasing the right mouse button and currently dragging a line
        if(currentEvent.button == 1 && currentRoomNodeGraph.roomNodeToDrawLineFrom !=null)
        {
            //Check if over a room node
            RoomNodeSO roomNode = IsMouseOverRoomNode(currentEvent);

            if(roomNode != null)
            {
                //if so set it as a child of the parent room node if it can be added
                if (currentRoomNodeGraph.roomNodeToDrawLineFrom.AddChildRoomNodeIDToRoomNode(roomNode.id))
                {
                    //Set parent ID in child room node
                    roomNode.AddParentRoomNodeIDToRoomNode(currentRoomNodeGraph.roomNodeToDrawLineFrom.id);
                }
            }

            ClearLineDrag();
        }
    }

    private void ProcessMouseDragEvent(Event currentEvent)
    {
        //Process right click drag event - draw line
        if (currentEvent.button == 1)
        {
            ProcessRightMouseDragEvent(currentEvent);
        }
        //Process left click drag event - drag node graph
        else if(currentEvent.button == 0)
        {
            ProcessLeftMouseDragEvent(currentEvent.delta);
        }
    }

    //<summary>
    //Process Right mouse drag event => draw the line
    private void ProcessRightMouseDragEvent(Event currentEvent)
    {
        if (currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            DragConnectingLine(currentEvent.delta);
            GUI.changed = true;
        }
    }

    private void ProcessLeftMouseDragEvent(Vector2 dragDelta)
    {
        graphDrag = dragDelta;

        for(int i = 0; i < currentRoomNodeGraph.roomNodeList.Count; i++)
        {
            currentRoomNodeGraph.roomNodeList[i].DragNode(dragDelta);
        }

        GUI.changed = true;
    }

    public void DragConnectingLine(Vector2 delta)
    {
        currentRoomNodeGraph.linePosition += delta;
    }

    //<summary>
    //Clear Line drag from a room node
    //<summary>
    private void ClearLineDrag()
    {
        currentRoomNodeGraph.roomNodeToDrawLineFrom = null;
        currentRoomNodeGraph.linePosition = Vector2.zero;
        GUI.changed = true;
    }

    private void DrawRoomNodeConnections()
    {
        //Loop through all room nodes
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if(roomNode.childRoomNodeIDList.Count > 0)
            {
                //Loop through all child room nodes
                foreach (string childRoomNodeID in roomNode.childRoomNodeIDList)
                {
                    //get child room node from dictionary
                    if (currentRoomNodeGraph.roomNodeDictionary.ContainsKey(childRoomNodeID))
                    {
                        DrawConnectionLine(roomNode, currentRoomNodeGraph.roomNodeDictionary[childRoomNodeID]);

                        GUI.changed = true;
                    }
                }
            }
        }
    }

    //<summary>
    //Draw connection line between the parent room node and child room node
    //<summary>
    private void DrawConnectionLine(RoomNodeSO parentRoomNode, RoomNodeSO childRoomNode)
    {
        //get line start and end position
        Vector2 startPosition = parentRoomNode.rect.center;
        Vector2 endPosition = childRoomNode.rect.center;

        //calculate midway point
        Vector2 midPosition = (endPosition + startPosition) / 2f;

        //Vector from start to end position of line
        Vector2 direction = endPosition - startPosition;

        //Calculate normalized perpendicular positions from the midpoint
        Vector2 arrowTailPoint1 = midPosition - new Vector2(-direction.y, direction.x).normalized * connectingLineArrowSize;
        Vector2 arrowTailPoint2 = midPosition + new Vector2(-direction.y, direction.x).normalized * connectingLineArrowSize;

        //Calculate midpoint offset position for arrow head
        Vector2 arrowHeadPoint = midPosition + direction.normalized * connectingLineArrowSize;

        //Draw Arrow
        Handles.DrawBezier(arrowHeadPoint, arrowTailPoint1, arrowHeadPoint, arrowTailPoint1, Color.white, null, connectingLineWidth);
        Handles.DrawBezier(arrowHeadPoint, arrowTailPoint2, arrowHeadPoint, arrowTailPoint2, Color.white, null, connectingLineWidth);

        //Draw Line
        Handles.DrawBezier(startPosition, endPosition, startPosition, endPosition, Color.white, null, connectingLineWidth);
    }

    //<summary>
    //Draw room nodes in the graph window
    //<summary>
    private void DrawRoomNodes()
    {
        //Loopp through all room nodes and draw them
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (roomNode.IsSelected)
            {
                roomNode.Draw(roomNodeSelectedStyle);
            }
            else
            {
                roomNode.Draw(roomNodeStyle);
            }
        }

        GUI.changed = true;
    }

    //<summary>
    //Selection changed on the inspector
    //<summary>
    private void InspectorSelectionChanged()
    {
        RoomNodeGraphSO roomNodeGraph = Selection.activeObject as RoomNodeGraphSO;

        if (roomNodeGraph != null)
        {
            currentRoomNodeGraph = roomNodeGraph;
            GUI.changed = true;
        }
    }
}
