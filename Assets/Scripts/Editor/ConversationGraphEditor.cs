using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// GraphView-based authoring tool for ConversationGraph assets (design D15/D16).
/// The canvas is a pure view over the data model: every node view maps back to a node
/// GUID, choice options are output ports keyed by option GUID, and the inspector pane
/// edits the node data directly. All GraphView touch points live in this file.
public class ConversationGraphEditor : EditorWindow
{
    private const string TextAreaControlName = "ConversationNodeMessageTextArea";

    private ConversationGraph targetGraph;
    private ConversationGraphView graphView;
    private IMGUIContainer inspectorContainer;
    private ConversationNodeData selectedNodeData;
    private Vector2 inspectorScroll;

    // node GUID -> view map, rebuilt wholesale by RebuildGraphView(); lets inspector
    // commits refresh a single node's preview in place (design D2)
    private readonly Dictionary<string, ConversationNodeView> nodeViews = new Dictionary<string, ConversationNodeView>();

    /// In-window clipboard (design D5): deep clones of selected nodes plus the
    /// internal edges between them, with old→new GUID remaps prepared at paste.
    private class NodeClipboard
    {
        public List<ConversationNodeData> nodes = new List<ConversationNodeData>();
        public List<ConversationEdgeData> edges = new List<ConversationEdgeData>();
    }

    // clipboard is scoped to one graph (design D5): cleared when targetGraph changes
    // or the window closes; never survives a domain reload (declared non-goal)
    private NodeClipboard clipboard;

    // markable authoring state (ported from ChatLogEditor)
    private string editedMessage = string.Empty;
    private int newMarkableMarkerIndex;
    private int currentSelectionStart = -1;
    private int currentSelectionEnd = -1;
    private static MarkerType[] markerTypesCache;

    private static MarkerType[] MarkerTypes
    {
        get
        {
            if(markerTypesCache == null)
            {
                markerTypesCache = typeof(Markers)
                    .GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(field => field.FieldType == typeof(MarkerType))
                    .Select(field => field.GetValue(null) as MarkerType)
                    .Where(markerType => markerType != null)
                    .ToArray();
            }

            return markerTypesCache;
        }
    }

    [MenuItem("Custom Tools/Conversation Graph Editor")]
    public static void OpenWindow()
    {
        GetWindow<ConversationGraphEditor>("Conversation Graph Editor");
    }

    private void OnEnable()
    {
        // NOTE: Unity re-invokes OnEnable on a surviving window after a domain reload
        //       (e.g. a recompile or the migration's AssetDatabase.Refresh), and the
        //       native visual tree persists across that reload. Without clearing, a
        //       second split view stacks under the first and the window renders doubled
        //       (with the stale graph view in the top half). Clear is a no-op when the
        //       tree is already empty.
        rootVisualElement.Clear();

        var splitView = new TwoPaneSplitView(0, 300, TwoPaneSplitViewOrientation.Horizontal);
        rootVisualElement.Add(splitView);

        graphView = new ConversationGraphView();
        graphView.graphViewChanged = OnGraphViewChanged;
        graphView.SelectionChanged += OnGraphViewSelectionChanged;
        graphView.CutRequested += OnCutRequested;
        graphView.CopyRequested += OnCopyRequested;
        graphView.PasteRequested += OnPasteRequested;
        graphView.DuplicateRequested += OnDuplicateRequested;
        graphView.CanPasteHandler = () => clipboard != null && clipboard.nodes.Count > 0 && targetGraph != null;
        graphView.RegisterCallback<ContextualMenuPopulateEvent>(OnBuildContextualMenu);
        splitView.Add(graphView);

        inspectorContainer = new IMGUIContainer(DrawInspector);
        splitView.Add(inspectorContainer);

        // NOTE: OnEnable can run again without a preceding OnDisable, so unsubscribe
        //       before subscribing to keep the static event single-subscribed
        Selection.selectionChanged -= OnSelectionChanged;
        Selection.selectionChanged += OnSelectionChanged;
        // undo/redo mutates the graph asset in memory — the canvas must be rebuilt or
        // the change is invisible (design D5's operations record Undo)
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        Undo.undoRedoPerformed += OnUndoRedoPerformed;
        OnSelectionChanged();
    }

    private void OnDisable()
    {
        if(graphView != null)
        {
            graphView.SelectionChanged -= OnGraphViewSelectionChanged;
            graphView.CutRequested -= OnCutRequested;
            graphView.CopyRequested -= OnCopyRequested;
            graphView.PasteRequested -= OnPasteRequested;
            graphView.DuplicateRequested -= OnDuplicateRequested;
            graphView.CanPasteHandler = null;
        }
        Selection.selectionChanged -= OnSelectionChanged;
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        nodeViews.Clear();
        clipboard = null;
    }

    /// Undo/redo restores the graph asset's serialized state, but the GraphView is a
    /// separate visual tree that must be rebuilt to reflect it — otherwise Ctrl+Z looks
    /// like a no-op (the nodes stay on the canvas) (design D5).
    private void OnUndoRedoPerformed()
    {
        if(targetGraph == null) return;

        // the selected node may have been removed by the undo (e.g. undoing a paste)
        if(selectedNodeData != null && !string.IsNullOrEmpty(selectedNodeData.guid)
            && targetGraph.GetNode(selectedNodeData.guid) == null)
        {
            selectedNodeData = null;
        }

        RebuildGraphView();
        inspectorContainer?.MarkDirtyRepaint();
    }

    private void OnSelectionChanged()
    {
        ConversationGraph newTarget = Selection.activeObject as ConversationGraph;
        if(newTarget == null || newTarget == targetGraph) return;

        targetGraph = newTarget;
        clipboard = null; // clipboard is per-graph (design D5)
        selectedNodeData = null;
        bool laidOut = EnsureDefaultLayout();
        RebuildGraphView();
        if(laidOut) FrameGraphContent();
        inspectorContainer?.MarkDirtyRepaint();
    }

    #region Graph View Lifecycle

    private void RebuildGraphView()
    {
        if(graphView == null) return;

        nodeViews.Clear();

        foreach(GraphElement element in graphView.graphElements.ToList())
        {
            graphView.RemoveElement(element);
        }

        if(targetGraph == null) return;

        if(targetGraph.nodes != null)
        {
            foreach(ConversationNodeData node in targetGraph.nodes)
            {
                if(node == null || string.IsNullOrEmpty(node.guid)) continue;
                ConversationNodeView view = new ConversationNodeView(node);
                nodeViews[node.guid] = view;
                graphView.AddElement(view);
            }
        }

        if(targetGraph.edges != null)
        {
            foreach(ConversationEdgeData edge in targetGraph.edges)
            {
                if(edge == null) continue;
                if(!nodeViews.TryGetValue(edge.fromNodeGuid, out ConversationNodeView fromView)) continue;
                if(!nodeViews.TryGetValue(edge.toNodeGuid, out ConversationNodeView toView)) continue;

                Port outputPort = string.IsNullOrEmpty(edge.fromOptionGuid)
                    ? fromView.OutputPort
                    : fromView.GetOutputPortForOption(edge.fromOptionGuid);

                if(outputPort == null || toView.InputPort == null) continue;

                Edge viewEdge = outputPort.ConnectTo(toView.InputPort);
                viewEdge.userData = edge;
                graphView.AddElement(viewEdge);
            }
        }

        inspectorContainer?.MarkDirtyRepaint();
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange changes)
    {
        // NOTE: new edges the user just dragged are reported here — Port.edgeConnector is
        //       read-only in Unity 6, so the default DefaultEdgeConnectorListener routes
        //       new edges into edgesToCreate instead of a custom IEdgeConnectorListener
        if(changes.edgesToCreate != null && targetGraph != null)
        {
            foreach(Edge edge in changes.edgesToCreate)
            {
                if(edge == null || edge.output == null || edge.input == null) continue;
                if(!(edge.output.node is ConversationNodeView fromView)) continue;
                if(!(edge.input.node is ConversationNodeView toView)) continue;

                string optionGuid = edge.output.userData as string ?? string.Empty;
                Undo.RecordObject(targetGraph, "Connect conversation nodes");
                targetGraph.Connect(fromView.NodeGuid, toView.NodeGuid, optionGuid);
                PersistGraph();
            }
        }

        // dragged node positions persist with the graph asset, so layouts survive reopening
        if(changes.movedElements != null && targetGraph != null)
        {
            bool anyMoved = false;
            foreach(GraphElement element in changes.movedElements)
            {
                if(element is ConversationNodeView nodeView && nodeView.NodeData != null)
                {
                    nodeView.NodeData.editorPosition = nodeView.GetPosition().position;
                    anyMoved = true;
                }
            }

            if(anyMoved) PersistGraph();
        }

        if(changes.elementsToRemove != null)
        {
            foreach(GraphElement element in changes.elementsToRemove)
            {
                if(element is ConversationNodeView nodeView)
                {
                    if(selectedNodeData != null && selectedNodeData.guid == nodeView.NodeGuid) selectedNodeData = null;

                    if(targetGraph != null)
                    {
                        Undo.RecordObject(targetGraph, "Remove conversation node");
                        targetGraph.RemoveNode(nodeView.NodeGuid);
                        PersistGraph();
                    }

                    EditorApplication.delayCall += RebuildGraphView;
                }
                else if(element is Edge edge && edge.userData is ConversationEdgeData dataEdge)
                {
                    if(targetGraph != null)
                    {
                        Undo.RecordObject(targetGraph, "Disconnect conversation nodes");
                        targetGraph.Disconnect(dataEdge);
                        PersistGraph();
                    }

                    EditorApplication.delayCall += RebuildGraphView;
                }
            }
        }

        return changes;
    }

    private void OnGraphViewSelectionChanged()
    {
        selectedNodeData = null;

        foreach(ISelectable selectable in graphView.selection)
        {
            if(selectable is ConversationNodeView nodeView)
            {
                selectedNodeData = nodeView.NodeData;
                break;
            }
        }

        editedMessage = selectedNodeData != null && selectedNodeData.bubble != null ? selectedNodeData.bubble.message : string.Empty;
        currentSelectionStart = currentSelectionEnd = -1;
        inspectorContainer?.MarkDirtyRepaint();
    }

    private void OnBuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        if(targetGraph == null) return;

        evt.menu.AppendAction("Add Bubble Node", _ => AddNode(ConversationNodeKind.Bubble));
        evt.menu.AppendAction("Add Choice Node", _ => AddNode(ConversationNodeKind.Choice));
        evt.menu.AppendAction("Add Wait Node", _ => AddNode(ConversationNodeKind.Wait));
        evt.menu.AppendAction("Add Entry Node", _ => AddNode(ConversationNodeKind.Entry));
        evt.menu.AppendAction("Add End Node", _ => AddNode(ConversationNodeKind.End));
        evt.menu.AppendSeparator();
        evt.menu.AppendAction("Layout Graph", _ => { LayoutGraph(); RebuildGraphView(); FrameGraphContent(); });
        evt.menu.AppendAction("Validate Graph", _ => ValidateGraph());
    }

    /// Auto-arranges nodes in depth layers (BFS from entry nodes); islands unreachable
    /// from any entry are placed to the right of the main flow. Positions are written
    /// back to the node data and persisted.
    private void LayoutGraph()
    {
        if(targetGraph == null || targetGraph.nodes == null || targetGraph.nodes.Count == 0) return;

        Undo.RecordObject(targetGraph, "Layout conversation graph");

        const float columnSpacing = 280f;
        const float rowSpacing = 140f;
        const float margin = 40f;

        Dictionary<string, int> depths = new Dictionary<string, int>();
        Queue<ConversationNodeData> frontier = new Queue<ConversationNodeData>();

        foreach(ConversationNodeData node in targetGraph.nodes)
        {
            if(node == null || string.IsNullOrEmpty(node.guid)) continue;
            if(node.kind != ConversationNodeKind.Entry) continue;

            depths[node.guid] = 0;
            frontier.Enqueue(node);
        }

        while(frontier.Count > 0)
        {
            ConversationNodeData current = frontier.Dequeue();
            if(targetGraph.edges == null) continue;

            foreach(ConversationEdgeData edge in targetGraph.edges)
            {
                if(edge == null || edge.fromNodeGuid != current.guid) continue;

                ConversationNodeData next = targetGraph.GetNode(edge.toNodeGuid);
                if(next == null || depths.ContainsKey(next.guid)) continue;

                depths[next.guid] = depths[current.guid] + 1;
                frontier.Enqueue(next);
            }
        }

        int maxDepth = 0;
        foreach(int depth in depths.Values) maxDepth = Mathf.Max(maxDepth, depth);

        int[] rowCounters = new int[maxDepth + 1];
        foreach(ConversationNodeData node in targetGraph.nodes)
        {
            if(node == null || string.IsNullOrEmpty(node.guid) || !depths.ContainsKey(node.guid)) continue;

            int depth = depths[node.guid];
            node.editorPosition = new Vector2(margin + depth * columnSpacing, margin + rowCounters[depth] * rowSpacing);
            rowCounters[depth]++;
        }

        float islandX = margin + (maxDepth + 2) * columnSpacing;
        int islandRow = 0;

        foreach(ConversationNodeData node in targetGraph.nodes)
        {
            if(node == null || string.IsNullOrEmpty(node.guid) || depths.ContainsKey(node.guid)) continue;

            node.editorPosition = new Vector2(islandX, margin + islandRow * rowSpacing);
            islandRow++;
        }

        PersistGraph();
    }

    /// Freshly-authored nodes sit at the origin — run the default layout when any two
    /// nodes overlap. Called on graph open, so manually arranged layouts are untouched.
    /// Returns true when the default layout actually ran (design D3).
    private bool EnsureDefaultLayout()
    {
        if(targetGraph == null || targetGraph.nodes == null || targetGraph.nodes.Count < 2) return false;

        for(int i = 0; i < targetGraph.nodes.Count; i++)
        {
            if(targetGraph.nodes[i] == null) continue;

            for(int j = i + 1; j < targetGraph.nodes.Count; j++)
            {
                if(targetGraph.nodes[j] == null) continue;

                if(Vector2.Distance(targetGraph.nodes[i].editorPosition, targetGraph.nodes[j].editorPosition) < 1f)
                {
                    LayoutGraph();
                    return true;
                }
            }
        }

        return false;
    }

    /// Frames all graph content in the center of the viewport. Bounds come from the data
    /// model (node editorPosition) plus laid-out/placeholder sizes, so — unlike
    /// FrameAll()/CalculateRectToFitAll() — framing does not depend on a measured layout
    /// pass and can be applied synchronously. The transform is produced by Unity's own
    /// GraphView.CalculateFrameTransform, so the fit/centering matches FrameAll exactly.
    private void FrameGraphContent()
    {
        if(graphView == null || targetGraph == null || nodeViews.Count == 0) return;

        ApplyGraphFrame();

        // re-apply once on the next tick so pre-layout placeholder sizes are replaced by
        // the real laid-out sizes
        graphView.schedule.Execute(ApplyGraphFrame);
    }

    /// True when a float is neither NaN nor ±Infinity. Layout sizes read as NaN before
    /// an element has been laid out, and NaN fails every comparison, so a plain
    /// `value < threshold` test silently lets NaN through.
    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private void ApplyGraphFrame()
    {
        if(graphView == null) return;

        // nominal node footprint, ~= one layout grid cell (columnSpacing 280 x rowSpacing 140)
        const float fallbackWidth = 240f;
        const float fallbackHeight = 120f;

        bool hasBounds = false;
        Rect contentRect = new Rect();

        foreach(ConversationNodeView view in nodeViews.Values)
        {
            if(view == null) continue;

            // use the data-model position (the exact content-space coordinate LayoutGraph
            // writes) plus the element's laid-out size; the size is NaN/zero until the
            // layout pass has run, so fall back to a nominal footprint
            Vector2 nodePosition = view.NodeData != null ? view.NodeData.editorPosition : Vector2.zero;
            if(!IsFinite(nodePosition.x) || !IsFinite(nodePosition.y)) nodePosition = Vector2.zero;

            Vector2 nodeSize = view.layout.size;
            if(!IsFinite(nodeSize.x) || nodeSize.x <= 1f) nodeSize.x = fallbackWidth;
            if(!IsFinite(nodeSize.y) || nodeSize.y <= 1f) nodeSize.y = fallbackHeight;
            Rect nodeRect = new Rect(nodePosition, nodeSize);

            if(!hasBounds)
            {
                contentRect = nodeRect;
                hasBounds = true;
            }
            else
            {
                contentRect = Rect.MinMaxRect(
                    Mathf.Min(contentRect.xMin, nodeRect.xMin),
                    Mathf.Min(contentRect.yMin, nodeRect.yMin),
                    Mathf.Max(contentRect.xMax, nodeRect.xMax),
                    Mathf.Max(contentRect.yMax, nodeRect.yMax));
            }
        }

        // a non-finite rect would make CalculateFrameTransform emit NaN, which
        // UpdateViewTransform rejects outright (silent no-op)
        if(!hasBounds || !IsFinite(contentRect.x) || !IsFinite(contentRect.y)
            || !IsFinite(contentRect.width) || !IsFinite(contentRect.height)) return;

        // clientRect is the GraphView's own layout rect, exactly what Frame() passes
        Rect viewport = graphView.layout;
        if(!IsFinite(viewport.width) || !IsFinite(viewport.height) || viewport.width <= 1f || viewport.height <= 1f) return;

        GraphView.CalculateFrameTransform(contentRect, viewport, 30, out Vector3 translation, out Vector3 scaling);
        graphView.UpdateViewTransform(translation, scaling);
        // the editor panel does not always repaint on a pure transform change
        graphView.MarkDirtyRepaint();
    }

    private void AddNode(ConversationNodeKind kind)
    {
        if(targetGraph == null) return;

        Undo.RecordObject(targetGraph, "Add conversation node");
        ConversationNodeData node = targetGraph.AddNode(kind);
        node.editorPosition = new Vector2(100 + (targetGraph.nodes.Count % 8) * 30f, 100 + (targetGraph.nodes.Count % 8) * 30f);
        PersistGraph();
        RebuildGraphView();
    }

    private void PersistGraph()
    {
        if(targetGraph == null) return;
        EditorUtility.SetDirty(targetGraph);
        AssetDatabase.SaveAssetIfDirty(targetGraph);
    }

    /// Targeted in-place refresh of the selected node's preview after an inspector
    /// commit that changed data shown on the node (title, message/condition labels,
    /// option port labels). Structural changes still go through RebuildGraphView().
    private void RefreshSelectedNodePreview()
    {
        if(selectedNodeData == null || string.IsNullOrEmpty(selectedNodeData.guid)) return;
        if(nodeViews.TryGetValue(selectedNodeData.guid, out ConversationNodeView view) && view != null)
        {
            view.RefreshPreview();
        }
    }

    private void RecordChange(string operation)
    {
        if(targetGraph != null) Undo.RecordObject(targetGraph, operation);
    }

    #endregion

    #region Clipboard Operations

    /// Copies the selected nodes plus the edges between them into the in-window
    /// clipboard (design D5). GUIDs are NOT remapped here — that happens at paste
    /// time so repeated pastes each get fresh identities. No mutation, no Undo.
    private void OnCopyRequested()
    {
        if(graphView == null) return;

        List<ConversationNodeView> selectedViews = new List<ConversationNodeView>();
        foreach(ISelectable selectable in graphView.selection)
        {
            if(selectable is ConversationNodeView nodeView) selectedViews.Add(nodeView);
        }
        if(selectedViews.Count == 0) return;

        HashSet<string> selectedGuids = new HashSet<string>();
        NodeClipboard copy = new NodeClipboard();
        foreach(ConversationNodeView view in selectedViews)
        {
            if(view.NodeData == null || string.IsNullOrEmpty(view.NodeGuid)) continue;
            selectedGuids.Add(view.NodeGuid);
            // JsonUtility round-trip deep-clones the nested lists (markables, options,
            // conditions, effects) and editorPosition; ScriptableObject references
            // (Markable.markerType) survive as instance-ID references, which resolve
            // within the same editor session — the declared clipboard scope (design D5)
            copy.nodes.Add(JsonUtility.FromJson<ConversationNodeData>(JsonUtility.ToJson(view.NodeData)));
        }

        if(copy.nodes.Count == 0) return;

        // the payload's edges are the graph edges whose BOTH endpoints are selected
        if(targetGraph != null && targetGraph.edges != null)
        {
            foreach(ConversationEdgeData edge in targetGraph.edges)
            {
                if(edge == null) continue;
                if(!selectedGuids.Contains(edge.fromNodeGuid) || !selectedGuids.Contains(edge.toNodeGuid)) continue;
                copy.edges.Add(new ConversationEdgeData
                {
                    fromNodeGuid = edge.fromNodeGuid,
                    fromOptionGuid = edge.fromOptionGuid,
                    toNodeGuid = edge.toNodeGuid
                });
            }
        }

        clipboard = copy;
    }

    /// Pastes the in-window clipboard (design D5): re-clones each stored node with
    /// fresh node and option GUIDs, reconnects the internal edges via the old→new
    /// remap, persists, rebuilds the view, and selects the new nodes. One Undo step
    /// covers the whole paste — the graph asset is a single object.
    private void OnPasteRequested()
    {
        if(targetGraph == null || clipboard == null || clipboard.nodes.Count == 0) return;

        Undo.RecordObject(targetGraph, "Paste conversation nodes");

        Dictionary<string, string> nodeRemap = new Dictionary<string, string>();
        Dictionary<string, string> optionRemap = new Dictionary<string, string>();
        List<string> pastedGuids = new List<string>();

        foreach(ConversationNodeData clipNode in clipboard.nodes)
        {
            if(clipNode == null || string.IsNullOrEmpty(clipNode.guid)) continue;

            // re-clone per paste so repeated pastes never share instances
            ConversationNodeData fresh = JsonUtility.FromJson<ConversationNodeData>(JsonUtility.ToJson(clipNode));

            string newNodeGuid = Guid.NewGuid().ToString();
            fresh.guid = newNodeGuid;
            nodeRemap[clipNode.guid] = newNodeGuid;

            if(fresh.options != null)
            {
                foreach(ConversationChoiceOptionData option in fresh.options)
                {
                    if(option == null) continue;
                    // record the remap from the clipboard node's option guid before
                    // overwriting — the re-clone carries the same option guids
                    string newOptionGuid = Guid.NewGuid().ToString();
                    optionRemap[option.guid] = newOptionGuid;
                    option.guid = newOptionGuid;
                }
            }

            // offset from the source position so both stay visible and selectable
            fresh.editorPosition += new Vector2(40f, 40f);
            targetGraph.nodes.Add(fresh);
            pastedGuids.Add(newNodeGuid);
        }

        foreach(ConversationEdgeData edge in clipboard.edges)
        {
            if(edge == null) continue;
            if(!nodeRemap.TryGetValue(edge.fromNodeGuid, out string fromGuid)) continue;
            if(!nodeRemap.TryGetValue(edge.toNodeGuid, out string toGuid)) continue;

            string optionGuid = string.Empty;
            if(!string.IsNullOrEmpty(edge.fromOptionGuid))
            {
                if(!optionRemap.TryGetValue(edge.fromOptionGuid, out optionGuid)) continue;
            }

            // Connect dedupes identical edges and SetDirty's the asset
            targetGraph.Connect(fromGuid, toGuid, optionGuid);
        }

        PersistGraph();
        RebuildGraphView();

        // ClearSelection/AddToSelection raise SelectionChanged → OnGraphViewSelectionChanged,
        // which sets selectedNodeData to the first pasted node and repaints the inspector —
        // no need to set selectedNodeData manually (design D5)
        graphView.ClearSelection();
        foreach(string guid in pastedGuids)
        {
            if(nodeViews.TryGetValue(guid, out ConversationNodeView view) && view != null)
            {
                graphView.AddToSelection(view);
            }
        }
    }

    /// Duplicate = copy + paste immediately (design D5).
    private void OnDuplicateRequested()
    {
        OnCopyRequested();
        OnPasteRequested();
    }

    /// Cut = copy into the clipboard, then remove the source nodes (and their touching
    /// edges) in one Undo step (design D5). Kept off OnGraphViewChanged's
    /// elementsToRemove path — that route is for user Delete-key removals and schedules
    /// per-element delayed rebuilds.
    private void OnCutRequested()
    {
        if(graphView == null) return;

        // capture the guids BEFORE removing — the removal mutates the graph data
        List<string> removedGuids = new List<string>();
        foreach(ISelectable selectable in graphView.selection)
        {
            if(selectable is ConversationNodeView nodeView && !string.IsNullOrEmpty(nodeView.NodeGuid))
            {
                removedGuids.Add(nodeView.NodeGuid);
            }
        }
        if(removedGuids.Count == 0) return;

        OnCopyRequested();

        // one Undo step for the whole cut
        Undo.RecordObject(targetGraph, "Cut conversation nodes");
        foreach(string guid in removedGuids)
        {
            // RemoveNode also drops every edge touching the node
            targetGraph.RemoveNode(guid);
        }

        if(selectedNodeData != null && removedGuids.Contains(selectedNodeData.guid)) selectedNodeData = null;

        PersistGraph();
        RebuildGraphView();
        inspectorContainer?.MarkDirtyRepaint();
    }

    #endregion

    #region Inspector

    private void DrawInspector()
    {
        inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Node Inspector", EditorStyles.boldLabel);

        if(targetGraph == null)
        {
            EditorGUILayout.HelpBox("Select a ConversationGraph asset (in the Project window or via the target field).", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        if(selectedNodeData == null)
        {
            EditorGUILayout.HelpBox("Select a node on the canvas to edit its settings.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.LabelField("Kind: " + selectedNodeData.kind, EditorStyles.boldLabel);
        EditorGUILayout.Space();

        switch(selectedNodeData.kind)
        {
            case ConversationNodeKind.Bubble:
                DrawBubbleSettings();
                break;
            case ConversationNodeKind.Choice:
                DrawChoiceSettings();
                break;
            case ConversationNodeKind.Wait:
                DrawConditionList("Wait Conditions", ConversationEditorTooltips.WaitConditions, selectedNodeData.conditions);
                break;
            case ConversationNodeKind.Entry:
                DrawConditionList("Entry Conditions", ConversationEditorTooltips.EntryConditions, selectedNodeData.conditions);
                DrawEntrySettings();
                break;
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawBubbleSettings()
    {
        ChatBubble bubble = selectedNodeData.bubble;

        EditorGUILayout.LabelField(new GUIContent("Bubble Data", ConversationEditorTooltips.BubbleData), EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        Constants.ChatUser newUser = (Constants.ChatUser)EditorGUILayout.EnumPopup(new GUIContent("Chat User", ConversationEditorTooltips.ChatUser), bubble.chatUser);
        float newDelay = EditorGUILayout.Slider(new GUIContent("Delay Length", ConversationEditorTooltips.DelayLength), bubble.delayLength, 0f, 10f);
        float newTyping = EditorGUILayout.Slider(new GUIContent("Typing Flag Length", ConversationEditorTooltips.TypingFlagLength), bubble.typingFlagLength, 0f, 10f);
        if(EditorGUI.EndChangeCheck())
        {
            RecordChange("Edit bubble metadata");
            bubble.chatUser = newUser;
            bubble.delayLength = newDelay;
            bubble.typingFlagLength = newTyping;
            PersistGraph();
        }

        EditorGUILayout.Space();
        DrawMessageEditor(bubble);
        EditorGUILayout.Space();
        DrawMarkableControls(bubble);
        DrawMarkables(bubble);
    }

    private void DrawMessageEditor(ChatBubble bubble)
    {
        // NOTE: GUILayout.TextArea has no GUIContent overload, so the Message tooltip
        // lives on this header; it still describes the editable TextArea below it.
        EditorGUILayout.LabelField(new GUIContent("Message", ConversationEditorTooltips.Message), EditorStyles.boldLabel);
        GUI.SetNextControlName(TextAreaControlName);
        EditorGUI.BeginChangeCheck();
        editedMessage = GUILayout.TextArea(editedMessage, GUI.skin.textArea, GUILayout.Height(90));
        if(EditorGUI.EndChangeCheck())
        {
            RecordChange("Edit bubble message");
            bubble.message = editedMessage;
            bubble.SyncMarkables();
            PersistGraph();
            RefreshSelectedNodePreview();
        }

        if(Event.current.type == EventType.Repaint) CaptureTextAreaSelection();
    }

    private void CaptureTextAreaSelection()
    {
        TextEditor editor = GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;
        if(editor == null) return;

        currentSelectionStart = editor.selectIndex;
        currentSelectionEnd = editor.cursorIndex;

        if(currentSelectionStart == currentSelectionEnd)
        {
            currentSelectionStart = currentSelectionEnd = -1;
            return;
        }

        int start = Mathf.Min(currentSelectionStart, currentSelectionEnd);
        int end = Mathf.Max(currentSelectionStart, currentSelectionEnd);
        if(start < 0 || end > editedMessage.Length)
        {
            currentSelectionStart = currentSelectionEnd = -1;
            return;
        }

        currentSelectionStart = start;
        currentSelectionEnd = end;
    }

    private void DrawMarkableControls(ChatBubble bubble)
    {
        EditorGUILayout.LabelField(new GUIContent("New Markable", ConversationEditorTooltips.NewMarkable), EditorStyles.boldLabel);
        newMarkableMarkerIndex = EditorGUILayout.Popup(new GUIContent("Marker Type", ConversationEditorTooltips.MarkerTypeNew), newMarkableMarkerIndex, GetMarkerTypeLabels());

        string selectionDisplay = "No active selection";
        if(currentSelectionStart >= 0 && currentSelectionEnd > currentSelectionStart)
        {
            string selected = editedMessage.Substring(currentSelectionStart, currentSelectionEnd - currentSelectionStart);
            selectionDisplay = $"'{selected}' ({currentSelectionStart} to {currentSelectionEnd - 1})";
        }
        // NOTE: wrap the display text in GUIContent — a bare string binds to the
        //       LabelField(GUIContent, GUIStyle) overload, and Unity's implicit
        //       string→GUIStyle operator then looks the *text* up as a skin style
        //       ("Unable to find style 'No active selection' in skin 'DarkSkin'").
        EditorGUILayout.LabelField(new GUIContent("Current Selection", ConversationEditorTooltips.CurrentSelection), new GUIContent(selectionDisplay));

        EditorGUI.BeginDisabledGroup(currentSelectionStart < 0 || currentSelectionEnd <= currentSelectionStart);
        if(GUILayout.Button("Create Markable from Selection"))
        {
            CreateMarkableFromSelection(bubble);
        }
        EditorGUI.EndDisabledGroup();

        if(GUILayout.Button("Sync all markable indexes from current message"))
        {
            RecordChange("Sync markable indexes");
            bubble.SyncMarkables();
            PersistGraph();
        }
    }

    private void CreateMarkableFromSelection(ChatBubble bubble)
    {
        if(currentSelectionStart < 0 || currentSelectionEnd <= currentSelectionStart) return;
        if(MarkerTypes.Length == 0)
        {
            Debug.LogWarning("No MarkerType assets found — cannot create a markable.");
            return;
        }

        string selectedText = editedMessage.Substring(currentSelectionStart, currentSelectionEnd - currentSelectionStart);
        MarkerType markerType = MarkerTypes[Mathf.Clamp(newMarkableMarkerIndex, 0, MarkerTypes.Length - 1)];

        RecordChange("Add markable");
        bubble.markables.Add(new Markable
        {
            markerType = markerType,
            spanText = selectedText,
            occurrence = 0,
            startIndex = currentSelectionStart,
            endIndex = currentSelectionEnd - 1
        });

        PersistGraph();
        currentSelectionStart = currentSelectionEnd = -1;
    }

    private void DrawMarkables(ChatBubble bubble)
    {
        EditorGUILayout.LabelField(new GUIContent("Markables", ConversationEditorTooltips.Markables), EditorStyles.boldLabel);
        if(bubble.markables == null) bubble.markables = new List<Markable>();

        int markableToRemove = -1;
        for(int i = 0; i < bubble.markables.Count; i++)
        {
            Markable markable = bubble.markables[i];
            if(markable == null) continue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(new GUIContent($"Markable {i + 1}", ConversationEditorTooltips.Markables), EditorStyles.boldLabel);

            int currentMarkerIndex = GetMarkerIndex(markable.markerType);
            int selectedMarkerIndex = EditorGUILayout.Popup(new GUIContent("Marker Type", ConversationEditorTooltips.MarkerTypeExisting), currentMarkerIndex, GetMarkerTypeLabels());
            if(selectedMarkerIndex != currentMarkerIndex)
            {
                RecordChange("Change markable type");
                markable.markerType = MarkerTypes[selectedMarkerIndex];
                PersistGraph();
            }

            string newSpan = EditorGUILayout.TextField(new GUIContent("Anchor Text", ConversationEditorTooltips.AnchorText), markable.spanText);
            if(newSpan != markable.spanText)
            {
                RecordChange("Edit markable anchor text");
                markable.spanText = newSpan;
                PersistGraph();
            }

            int newOccurrence = EditorGUILayout.IntField(new GUIContent("Occurrence", ConversationEditorTooltips.Occurrence), markable.occurrence);
            if(newOccurrence != markable.occurrence)
            {
                RecordChange("Edit markable occurrence");
                markable.occurrence = Mathf.Max(0, newOccurrence);
                PersistGraph();
            }

            EditorGUILayout.LabelField(new GUIContent("Indexes", ConversationEditorTooltips.Indexes), $"{markable.startIndex} - {markable.endIndex}");
            EditorGUILayout.LabelField(new GUIContent("Resolved Text", ConversationEditorTooltips.ResolvedText), markable.GetSelectedText(editedMessage));

            if(GUILayout.Button("Re-sync indexes"))
            {
                RecordChange("Re-sync markable indexes");
                markable.RecalculateIndexes(editedMessage);
                PersistGraph();
            }

            if(GUILayout.Button("Remove"))
            {
                markableToRemove = i;
            }
            EditorGUILayout.EndVertical();
        }

        if(markableToRemove >= 0)
        {
            RecordChange("Remove markable");
            bubble.markables.RemoveAt(markableToRemove);
            PersistGraph();
        }
    }

    private void DrawChoiceSettings()
    {
        EditorGUILayout.LabelField(new GUIContent("Choice Data", ConversationEditorTooltips.ChoiceData), EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        bool newBlocksDay = EditorGUILayout.Toggle(new GUIContent("Blocks Day", ConversationEditorTooltips.BlocksDay), selectedNodeData.blocksDay);
        if(EditorGUI.EndChangeCheck())
        {
            RecordChange("Toggle blocks day");
            selectedNodeData.blocksDay = newBlocksDay;
            PersistGraph();
            RefreshSelectedNodePreview();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(new GUIContent("Options (one draft per option)", ConversationEditorTooltips.OptionsHeader), EditorStyles.boldLabel);

        List<ConversationChoiceOptionData> options = selectedNodeData.options;
        if(options == null) selectedNodeData.options = options = new List<ConversationChoiceOptionData>();

        int optionToRemove = -1;
        for(int i = 0; i < options.Count; i++)
        {
            ConversationChoiceOptionData option = options[i];
            if(option == null) continue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(new GUIContent($"Option {i + 1}", ConversationEditorTooltips.OptionsHeader), EditorStyles.boldLabel);

            string newPreview = EditorGUILayout.TextField(new GUIContent("Preview Text", ConversationEditorTooltips.OptionPreviewText), option.previewText);
            if(newPreview != option.previewText)
            {
                RecordChange("Edit option preview");
                option.previewText = newPreview;
                PersistGraph();
                RefreshSelectedNodePreview();
            }

            EditorGUI.BeginChangeCheck();
            Constants.ChatUser newUser = (Constants.ChatUser)EditorGUILayout.EnumPopup(new GUIContent("Posted By", ConversationEditorTooltips.PostedBy), option.postedBubble.chatUser);
            if(EditorGUI.EndChangeCheck())
            {
                RecordChange("Edit option author");
                option.postedBubble.chatUser = newUser;
                PersistGraph();
            }

            // NOTE: EditorGUILayout.TextArea has no GUIContent overload, so the Posted
            // Message tooltip is carried by this label immediately above the TextArea.
            EditorGUILayout.LabelField(new GUIContent("Posted Message", ConversationEditorTooltips.PostedMessage));
            EditorGUI.BeginChangeCheck();
            string newMessage = EditorGUILayout.TextArea(option.postedBubble.message, GUI.skin.textArea, GUILayout.Height(60));
            if(EditorGUI.EndChangeCheck())
            {
                RecordChange("Edit option posted message");
                option.postedBubble.message = newMessage;
                PersistGraph();
            }

            DrawEffects(option.effects);

            if(GUILayout.Button("Remove Option"))
            {
                optionToRemove = i;
            }
            EditorGUILayout.EndVertical();
        }

        if(optionToRemove >= 0)
        {
            RecordChange("Remove choice option");
            options.RemoveAt(optionToRemove);
            PersistGraph();
        }

        if(GUILayout.Button("+ Add Option"))
        {
            RecordChange("Add choice option");
            options.Add(new ConversationChoiceOptionData { guid = Guid.NewGuid().ToString() });
            PersistGraph();
            RebuildGraphView();
        }
    }

    /// <summary>
    /// Draws a sequence-event value as a popup of the known
    /// <see cref="Constants.SequenceEventType"/> names (design D4). An unrecognized
    /// non-empty stored value is appended as a final selected entry so legacy/typo
    /// strings stay visible and are NOT rewritten until the designer picks a known type.
    /// Returns the string to store (unchanged if untouched).
    /// </summary>
    /// <remarks>
    /// NOTE: the popup body uses the label-less <c>EditorGUILayout.Popup</c> overload so
    /// the inspector's condition/effect rows stay one line tall (design D4's call sites
    /// draw the value inline). A non-empty <paramref name="tooltip"/> therefore draws a
    /// compact leading "Value" label that carries the hover text — IMGUI binds tooltips
    /// to a label rect, and the label-less value overload cannot take a GUIContent.
    /// NOTE: an empty/null stored value maps to no selection (index -1) rather than index
    /// 0. The task's literal "0 when empty" would make the caller's string-compare see
    /// "MarkerOverload" != "" on the first repaint and silently persist it; index -1 keeps
    /// an unset value unchanged while still allowing any option to be picked.
    /// </remarks>
    private string DrawSequenceEventPopup(string currentValue, string tooltip = null, params GUILayoutOption[] options)
    {
        string[] names = Enum.GetNames(typeof(Constants.SequenceEventType));

        // append the raw stored value when it matches no known name (case-insensitive),
        // matching the runtime's Enum.TryParse(ignoreCase) + OrdinalIgnoreCase compares
        bool hasRawValue = !string.IsNullOrEmpty(currentValue)
            && !names.Any(name => string.Equals(name, currentValue, StringComparison.OrdinalIgnoreCase));

        string[] displayedOptions = hasRawValue ? names.Concat(new[] { currentValue }).ToArray() : names;

        int selectedIndex = -1;
        if(!string.IsNullOrEmpty(currentValue))
        {
            selectedIndex = Array.FindIndex(names, name => string.Equals(name, currentValue, StringComparison.OrdinalIgnoreCase));
            // non-empty + no name match implies hasRawValue, so the raw entry is last
            if(selectedIndex < 0) selectedIndex = displayedOptions.Length - 1;
        }

        if(!string.IsNullOrEmpty(tooltip))
        {
            EditorGUILayout.LabelField(new GUIContent("Value", tooltip), GUILayout.Width(38f));
        }

        int pickedIndex = EditorGUILayout.Popup(selectedIndex, displayedOptions, options);

        // -1 means no selection is active (the stored value was empty and is untouched)
        if(pickedIndex < 0) return currentValue;

        // the appended raw entry maps back to the untouched stored string
        return pickedIndex < names.Length ? names[pickedIndex] : currentValue;
    }

    private void DrawEffects(List<ConversationEffectData> effects)
    {
        EditorGUILayout.LabelField(new GUIContent("Effects (applied on pick)", ConversationEditorTooltips.EffectsHeader), EditorStyles.boldLabel);
        if(effects == null) return;

        int effectToRemove = -1;
        for(int i = 0; i < effects.Count; i++)
        {
            ConversationEffectData effect = effects[i];
            if(effect == null) continue;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Op", ConversationEditorTooltips.EffectOperation), GUILayout.Width(26f));
            ConversationEffectOperation newOp = (ConversationEffectOperation)EditorGUILayout.EnumPopup(effect.operation, GUILayout.Width(105));
            if(newOp != effect.operation)
            {
                RecordChange("Edit effect operation");
                effect.operation = newOp;
                PersistGraph();
            }

            switch(effect.operation)
            {
                case ConversationEffectOperation.SetFlag:
                {
                    // flags are arbitrary strings, NOT sequence events — keep free text
                    EditorGUILayout.LabelField(new GUIContent("Flag", ConversationEditorTooltips.EffectValueFlag), GUILayout.Width(36f));
                    string newStringValue = EditorGUILayout.TextField(effect.stringValue);
                    if(newStringValue != effect.stringValue)
                    {
                        RecordChange("Edit effect value");
                        effect.stringValue = newStringValue;
                        PersistGraph();
                    }
                    break;
                }
                case ConversationEffectOperation.RaiseEvent:
                {
                    string newStringValue = DrawSequenceEventPopup(effect.stringValue, ConversationEditorTooltips.EffectValueEvent);
                    if(newStringValue != effect.stringValue)
                    {
                        RecordChange("Edit effect value");
                        effect.stringValue = newStringValue;
                        PersistGraph();
                    }
                    break;
                }
            }

            if(GUILayout.Button("-", GUILayout.Width(25))) effectToRemove = i;
            EditorGUILayout.EndHorizontal();
        }

        if(effectToRemove >= 0)
        {
            RecordChange("Remove effect");
            effects.RemoveAt(effectToRemove);
            PersistGraph();
        }

        if(GUILayout.Button("+ Add Effect"))
        {
            RecordChange("Add effect");
            effects.Add(new ConversationEffectData());
            PersistGraph();
        }
    }

    private void DrawConditionList(string title, string headerTooltip, List<ConversationConditionClause> conditions)
    {
        EditorGUILayout.LabelField(new GUIContent(title, headerTooltip), EditorStyles.boldLabel);
        if(conditions == null) return;

        int clauseToRemove = -1;
        for(int i = 0; i < conditions.Count; i++)
        {
            ConversationConditionClause clause = conditions[i];
            if(clause == null) continue;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Kind", ConversationEditorTooltips.ConditionKind), GUILayout.Width(32f));
            ConversationConditionKind newKind = (ConversationConditionKind)EditorGUILayout.EnumPopup(clause.kind, GUILayout.Width(105));
            if(newKind != clause.kind)
            {
                RecordChange("Edit condition kind");
                clause.kind = newKind;
                PersistGraph();
                RefreshSelectedNodePreview();
            }

            switch(clause.kind)
            {
                case ConversationConditionKind.Event:
                {
                    string newStringValue = DrawSequenceEventPopup(clause.stringValue, ConversationEditorTooltips.ConditionValueEvent);
                    if(newStringValue != clause.stringValue)
                    {
                        RecordChange("Edit condition value");
                        clause.stringValue = newStringValue;
                        PersistGraph();
                        RefreshSelectedNodePreview();
                    }
                    break;
                }
                case ConversationConditionKind.RequiredFlag:
                {
                    // flags are arbitrary strings, NOT sequence events — keep free text
                    EditorGUILayout.LabelField(new GUIContent("Flag", ConversationEditorTooltips.ConditionValueFlag), GUILayout.Width(36f));
                    string newStringValue = EditorGUILayout.TextField(clause.stringValue);
                    if(newStringValue != clause.stringValue)
                    {
                        RecordChange("Edit condition value");
                        clause.stringValue = newStringValue;
                        PersistGraph();
                        RefreshSelectedNodePreview();
                    }
                    break;
                }
                case ConversationConditionKind.DayMin:
                case ConversationConditionKind.DayMax:
                {
                    EditorGUILayout.LabelField(new GUIContent("Day", ConversationEditorTooltips.ConditionValueDay), GUILayout.Width(36f));
                    int newIntValue = EditorGUILayout.IntField(clause.intValue);
                    if(newIntValue != clause.intValue)
                    {
                        RecordChange("Edit condition day");
                        clause.intValue = newIntValue;
                        PersistGraph();
                        RefreshSelectedNodePreview();
                    }
                    break;
                }
            }

            if(GUILayout.Button("-", GUILayout.Width(25))) clauseToRemove = i;
            EditorGUILayout.EndHorizontal();
        }

        if(clauseToRemove >= 0)
        {
            RecordChange("Remove condition");
            conditions.RemoveAt(clauseToRemove);
            PersistGraph();
            RefreshSelectedNodePreview();
        }

        if(GUILayout.Button("+ Add Condition"))
        {
            RecordChange("Add condition");
            conditions.Add(new ConversationConditionClause());
            PersistGraph();
            RefreshSelectedNodePreview();
        }
    }

    private void DrawEntrySettings()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(new GUIContent("Entry Scheduling", ConversationEditorTooltips.EntryScheduling), EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        bool newExclusive = EditorGUILayout.Toggle(new GUIContent("Exclusive", ConversationEditorTooltips.Exclusive), selectedNodeData.exclusive);
        string newGroupId = EditorGUILayout.TextField(new GUIContent("Exclusive Group Id", ConversationEditorTooltips.ExclusiveGroupId), selectedNodeData.exclusiveGroupId);
        if(EditorGUI.EndChangeCheck())
        {
            RecordChange("Edit entry scheduling");
            selectedNodeData.exclusive = newExclusive;
            selectedNodeData.exclusiveGroupId = newGroupId;
            PersistGraph();
            RefreshSelectedNodePreview();
        }

        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        bool newRepeatable = EditorGUILayout.Toggle(new GUIContent("Is Repeatable", ConversationEditorTooltips.IsRepeatable), selectedNodeData.isRepeatable);
        if(EditorGUI.EndChangeCheck())
        {
            RecordChange("Toggle entry repeatable");
            selectedNodeData.isRepeatable = newRepeatable;
            PersistGraph();
            RefreshSelectedNodePreview();
        }
    }

    private int GetMarkerIndex(MarkerType markerType)
    {
        if(markerType == null) return 0;
        for(int i = 0; i < MarkerTypes.Length; i++)
        {
            if(MarkerTypes[i].name == markerType.name) return i;
        }
        return 0;
    }

    private string[] GetMarkerTypeLabels()
    {
        return MarkerTypes.Select(mt => mt.name).ToArray();
    }

    #endregion

    #region Validation

    private void ValidateGraph()
    {
        if(targetGraph == null)
        {
            Debug.LogWarning("Conversation Graph Editor: no graph selected to validate.");
            return;
        }

        int issueCount = 0;
        bool isSeedGraph = targetGraph.isSeed;

        // dangling edges
        if(targetGraph.edges != null)
        {
            foreach(ConversationEdgeData edge in targetGraph.edges)
            {
                if(edge == null) continue;
                if(targetGraph.GetNode(edge.fromNodeGuid) == null || targetGraph.GetNode(edge.toNodeGuid) == null)
                {
                    Debug.LogWarning($"Conversation Graph '{targetGraph.name}': dangling edge {edge.fromNodeGuid} -> {edge.toNodeGuid}.", targetGraph);
                    issueCount++;
                }
            }
        }

        if(targetGraph.nodes == null)
        {
            Debug.LogWarning($"Conversation Graph '{targetGraph.name}' has no nodes.", targetGraph);
            return;
        }

        // reachability from entries (seed graphs render statically — excluded)
        if(!isSeedGraph)
        {
            HashSet<string> reachable = new HashSet<string>();
            Queue<string> frontier = new Queue<string>();

            foreach(ConversationNodeData node in targetGraph.nodes)
            {
                if(node != null && node.kind == ConversationNodeKind.Entry && !string.IsNullOrEmpty(node.guid))
                {
                    reachable.Add(node.guid);
                    frontier.Enqueue(node.guid);
                }
            }

            while(frontier.Count > 0)
            {
                string current = frontier.Dequeue();
                if(targetGraph.edges == null) continue;

                foreach(ConversationEdgeData edge in targetGraph.edges)
                {
                    if(edge == null || edge.fromNodeGuid != current) continue;
                    if(reachable.Add(edge.toNodeGuid)) frontier.Enqueue(edge.toNodeGuid);
                }
            }

            foreach(ConversationNodeData node in targetGraph.nodes)
            {
                if(node == null || string.IsNullOrEmpty(node.guid)) continue;
                if(node.kind == ConversationNodeKind.Entry) continue;
                if(!reachable.Contains(node.guid))
                {
                    Debug.LogWarning($"Conversation Graph '{targetGraph.name}': node '{node.kind}' ({node.guid}) is unreachable from any entry.", targetGraph);
                    issueCount++;
                }
            }
        }

        // day-blocking choices need at least one option edge; options need a posted message
        foreach(ConversationNodeData node in targetGraph.nodes)
        {
            if(node == null || node.kind != ConversationNodeKind.Choice) continue;

            if(node.blocksDay)
            {
                bool hasOptionEdge = targetGraph.edges != null && targetGraph.edges.Any(edge =>
                    edge != null && edge.fromNodeGuid == node.guid && !string.IsNullOrEmpty(edge.fromOptionGuid));

                if(!hasOptionEdge)
                {
                    Debug.LogWarning($"Conversation Graph '{targetGraph.name}': day-blocking choice '{node.guid}' has no outgoing option edges — the day could never end.", targetGraph);
                    issueCount++;
                }
            }

            if(node.options != null)
            {
                foreach(ConversationChoiceOptionData option in node.options)
                {
                    if(option == null) continue;
                    if(string.IsNullOrEmpty(option.postedBubble.message))
                    {
                        Debug.LogWarning($"Conversation Graph '{targetGraph.name}': choice '{node.guid}' option '{option.guid}' has an empty posted message.", targetGraph);
                        issueCount++;
                    }
                }
            }
        }

        // entries need an outgoing edge
        foreach(ConversationNodeData node in targetGraph.nodes)
        {
            if(node == null || node.kind != ConversationNodeKind.Entry) continue;
            if(targetGraph.edges == null || !targetGraph.edges.Any(edge => edge != null && edge.fromNodeGuid == node.guid))
            {
                Debug.LogWarning($"Conversation Graph '{targetGraph.name}': entry '{node.guid}' has no outgoing edge — it will never play content.", targetGraph);
                issueCount++;
            }
        }

        Debug.Log(issueCount == 0
            ? $"Conversation Graph '{targetGraph.name}' validated with no issues."
            : $"Conversation Graph '{targetGraph.name}' validated with {issueCount} issue(s) — see warnings above.");
    }

    #endregion

    /// <summary>
    /// Single reviewable table of inspector hover-tooltip wording (design D1). Each entry
    /// explains the RUNTIME effect of the field rather than restating its label, so the
    /// authoring tool doubles as documentation.
    /// </summary>
    private static class ConversationEditorTooltips
    {
        public const string ChatUser = "Which chat identity posts this bubble. The player's own identity is Avner.";
        public const string DelayLength = "Seconds waited before the typing indicator appears for this bubble.";
        public const string TypingFlagLength = "Seconds the typing indicator shows before the message posts.";
        public const string Message = "The bubble's message text. Markables are anchored to this text; editing it re-syncs markable indexes.";
        public const string MarkerTypeNew = "Marker type assigned to a markable created from the current text selection.";
        public const string MarkerTypeExisting = "Marker type this markable is scored against at runtime.";
        public const string CurrentSelection = "The text range currently selected in the message above; used to create a markable.";
        public const string AnchorText = "Literal text the markable anchors to. Indexes are resolved by searching the message for this span.";
        public const string Occurrence = "Which occurrence of the anchor text to use when it appears more than once (0 = first).";
        public const string Indexes = "Character range in the message this markable currently resolves to (start - end).";
        public const string ResolvedText = "The message substring the markable currently resolves to.";
        public const string BlocksDay = "While this choice is unanswered, the day cannot end — EndDay defers until the player picks an option.";
        public const string OptionPreviewText = "Text shown on the player's draft reply. May differ from the posted message.";
        public const string PostedBy = "Chat identity that posts the bubble when this option is picked — normally the player.";
        public const string PostedMessage = "Message actually posted to the log when this option is picked.";
        public const string EffectOperation = "What happens when this option is picked: SetFlag records a string flag; RaiseEvent broadcasts a sequence event.";
        public const string EffectValueFlag = "Flag name recorded on pick. Entry/Wait conditions can require this flag.";
        public const string EffectValueEvent = "Sequence event raised on pick. WorkStart starts the work clock; DayEnd ends the day (defers behind day-blocking choices).";
        public const string ConditionKind = "Gate type: Event requires a specific sequence event; DayMin/DayMax bound the day number; RequiredFlag requires a flag.";
        public const string ConditionValueEvent = "Event type that must fire for this condition to pass.";
        public const string ConditionValueFlag = "Flag name that must have been set for this condition to pass.";
        public const string ConditionValueDay = "Day number bound (inclusive) for this condition.";
        public const string Exclusive = "When true, entries sharing a group id compete per event and exactly one is queued.";
        public const string ExclusiveGroupId = "Group id for exclusive resolution. Ignored when Exclusive is off (entry is additive).";
        public const string IsRepeatable = "When true, this entry may activate every time its event fires; when false, at most once per event type per day.";

        // section-header tooltips (design D1: headers that describe editable content)
        public const string BubbleData = "Chat metadata applied when this bubble node plays.";
        public const string NewMarkable = "Create a scored marker target from a text selection in the message above.";
        public const string Markables = "Scored marker targets anchored to spans of this bubble's message.";
        public const string ChoiceData = "Settings for this choice node.";
        public const string OptionsHeader = "One draft reply per option; the player picks one to continue the thread.";
        public const string EffectsHeader = "Effects applied to game state when this option is picked.";
        public const string EntryScheduling = "Controls when this entry activates relative to other entries.";
        public const string WaitConditions = "Conditions that must all pass before a thread parked at this wait resumes.";
        public const string EntryConditions = "Conditions that must all pass for this entry to activate. An empty list means always eligible.";
    }
}

/// GraphView shell with zoom/pan/box-select and a public SelectionChanged C# event
/// (Unity 6's GraphView exposes only a read-only selection list — selection changes
/// are dispatched by overriding the AddToSelection/RemoveFromSelection virtuals).
/// Clipboard intents (design D5) follow the same pattern: the default Cut/Copy/Paste/
/// Duplicate pipeline round-trips through GraphView's element serialization, which
/// carries no payload for our custom nodes — so the view forwards intents to the
/// editor window, which owns all data mutation.
public class ConversationGraphView : GraphView
{
    public event System.Action SelectionChanged;

    // clipboard intents (design D5) — raised from the trickle-down ExecuteCommandEvent
    // handler (Ctrl+X/C/V/D) and from the context menu actions rebuilt in
    // BuildContextualMenu; consumed by ConversationGraphEditor
    public event System.Action CutRequested;
    public event System.Action CopyRequested;
    public event System.Action PasteRequested;
    public event System.Action DuplicateRequested;

    // paste enablement is owned by the window (the in-memory clipboard lives there —
    // design D5); the view only consults it in canPaste and in the menu status callback
    public System.Func<bool> CanPasteHandler;

    // NOTE: UnityEngine.UIElements.EventCommandNames is INTERNAL in Unity 6 — the
    //       clipboard command names arrive as plain strings on
    //       ExecuteCommandEvent.commandName, so they are literal constants here
    private const string CommandCut = "Cut";
    private const string CommandCopy = "Copy";
    private const string CommandPaste = "Paste";
    private const string CommandDuplicate = "Duplicate";

    public ConversationGraphView()
    {
        style.flexGrow = 1f;
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        // Ctrl+X/C/V/D arrive as ExecuteCommandEvent, NOT KeyDownEvent (Unity 6 GraphView
        // handles them in its bubble-phase OnExecuteCommand, which for Cut/Copy/Duplicate
        // calls the non-virtual CutSelectionCallback/CopySelectionCallback/
        // DuplicateSelectionCallback and for Paste calls PasteCallback — all useless for
        // our custom nodes). Intercept trickle-down and forward the intents instead,
        // stopping the event so the default element-serialization pipeline never runs.
        this.RegisterCallback<ExecuteCommandEvent>(OnExecuteClipboardCommand, TrickleDown.TrickleDown);
        // NOTE: no ValidateCommandEvent handler is registered — GraphView's own
        //       OnValidateCommand (registered in its constructor) consults the overridable
        //       canCutSelection/canCopySelection/canPaste/canDuplicateSelection virtuals,
        //       so shortcut and menu enablement already reflect the overrides below.
    }

    // clipboard enablement: the can* virtuals are the ONLY overridable part of the
    // default clipboard pipeline in Unity 6 (design D5) — the callbacks themselves are
    // non-virtual and unusable, so the intents above carry the real work
    protected override bool canCutSelection => HasNodeSelection();
    protected override bool canCopySelection => HasNodeSelection();
    protected override bool canDuplicateSelection => HasNodeSelection();
    protected override bool canPaste => CanPasteHandler != null && CanPasteHandler();

    private bool HasNodeSelection()
    {
        foreach(ISelectable selectable in selection)
        {
            if(selectable is ConversationNodeView) return true;
        }
        return false;
    }

    private void OnExecuteClipboardCommand(ExecuteCommandEvent evt)
    {
        if(evt.commandName == CommandCut)
        {
            if(!canCutSelection) return;
            CutRequested?.Invoke();
            evt.StopImmediatePropagation();
        }
        else if(evt.commandName == CommandCopy)
        {
            if(!canCopySelection) return;
            CopyRequested?.Invoke();
            evt.StopImmediatePropagation();
        }
        else if(evt.commandName == CommandPaste)
        {
            if(!canPaste) return;
            PasteRequested?.Invoke();
            evt.StopImmediatePropagation();
        }
        else if(evt.commandName == CommandDuplicate)
        {
            if(!canDuplicateSelection) return;
            DuplicateRequested?.Invoke();
            evt.StopImmediatePropagation();
        }
        // unhandled commands (Delete, Select All, ...) fall through to the base handler
    }

    /// The base appends the default Cut/Copy/Paste/Duplicate actions, wired to the
    /// unusable element-serialization pipeline. Remove them and re-insert our own
    /// actions at the same positions, wired to the intent events (design D5). The
    /// window's own OnBuildContextualMenu callback still appends the Add-node /
    /// Layout / Validate entries afterwards.
    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        base.BuildContextualMenu(evt);

        // NOTE: Unity 6 removed the nested DropdownMenu.MenuItem type (items are the
        //       top-level DropdownMenuItem), and only DropdownMenuAction exposes the
        //       public `name` the defaults are keyed by — match through a cast, not
        //       ToString() (which yields the type name). .ToList() normalizes the
        //       collection shape returned by MenuItems().
        List<DropdownMenuItem> items = evt.menu.MenuItems().ToList();
        int cutIndex = items.FindIndex(item => item is DropdownMenuAction action && action.name == "Cut");
        int copyIndex = items.FindIndex(item => item is DropdownMenuAction action && action.name == "Copy");
        int pasteIndex = items.FindIndex(item => item is DropdownMenuAction action && action.name == "Paste");
        int duplicateIndex = items.FindIndex(item => item is DropdownMenuAction action && action.name == "Duplicate");

        // nothing to replace (base variant without the clipboard actions) — keep defaults
        if(cutIndex < 0 && copyIndex < 0 && pasteIndex < 0 && duplicateIndex < 0) return;

        // remove from the highest index to the lowest so earlier indices stay valid
        int[] removeAt = { duplicateIndex, pasteIndex, copyIndex, cutIndex };
        for(int i = 0; i < removeAt.Length; i++)
        {
            if(removeAt[i] >= 0) evt.menu.RemoveItemAt(removeAt[i]);
        }

        // DropdownMenuAction callbacks are private, so the defaults cannot be retargeted
        // in place — insert fresh actions at the slot where "Cut" used to sit, keeping
        // the four grouped where the defaults were
        int insertAt = cutIndex >= 0 ? cutIndex : 0;
        evt.menu.InsertAction(insertAt, "Cut", _ => CutRequested?.Invoke(), _ => canCutSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        evt.menu.InsertAction(insertAt + 1, "Copy", _ => CopyRequested?.Invoke(), _ => canCopySelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        evt.menu.InsertAction(insertAt + 2, "Paste", _ => PasteRequested?.Invoke(), _ => canPaste ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        evt.menu.InsertAction(insertAt + 3, "Duplicate", _ => DuplicateRequested?.Invoke(), _ => canDuplicateSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
    }

    // Unity 6 ships no default NodeAdapter for PortSource<T>, so the stock
    // GetCompatiblePorts (adapter type lookup) never matches any port pair —
    // every dragged edge would show as incompatible. Like Unity's official
    // SimpleGraphView sample, drop the adapter check: any output may connect
    // to any input on a different node.
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        return ports.ToList().Where(port =>
            port != startPort &&
            port.node != startPort.node &&
            port.direction != startPort.direction).ToList();
    }

    public override void AddToSelection(ISelectable selectable)
    {
        base.AddToSelection(selectable);
        SelectionChanged?.Invoke();
    }

    public override void RemoveFromSelection(ISelectable selectable)
    {
        base.RemoveFromSelection(selectable);
        SelectionChanged?.Invoke();
    }

    public override void ClearSelection()
    {
        base.ClearSelection();
        SelectionChanged?.Invoke();
    }
}
