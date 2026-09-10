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
        var splitView = new TwoPaneSplitView(0, 300, TwoPaneSplitViewOrientation.Horizontal);
        rootVisualElement.Add(splitView);

        graphView = new ConversationGraphView();
        graphView.graphViewChanged = OnGraphViewChanged;
        graphView.SelectionChanged += OnGraphViewSelectionChanged;
        graphView.RegisterCallback<ContextualMenuPopulateEvent>(OnBuildContextualMenu);
        splitView.Add(graphView);

        inspectorContainer = new IMGUIContainer(DrawInspector);
        splitView.Add(inspectorContainer);

        Selection.selectionChanged += OnSelectionChanged;
        OnSelectionChanged();
    }

    private void OnDisable()
    {
        if(graphView != null) graphView.SelectionChanged -= OnGraphViewSelectionChanged;
        Selection.selectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged()
    {
        ConversationGraph newTarget = Selection.activeObject as ConversationGraph;
        if(newTarget == null || newTarget == targetGraph) return;

        targetGraph = newTarget;
        selectedNodeData = null;
        EnsureDefaultLayout();
        RebuildGraphView();
        inspectorContainer?.MarkDirtyRepaint();
    }

    #region Graph View Lifecycle

    private void RebuildGraphView()
    {
        if(graphView == null) return;

        foreach(GraphElement element in graphView.graphElements.ToList())
        {
            graphView.RemoveElement(element);
        }

        if(targetGraph == null) return;

        Dictionary<string, ConversationNodeView> views = new Dictionary<string, ConversationNodeView>();

        if(targetGraph.nodes != null)
        {
            foreach(ConversationNodeData node in targetGraph.nodes)
            {
                if(node == null || string.IsNullOrEmpty(node.guid)) continue;
                ConversationNodeView view = new ConversationNodeView(node);
                views[node.guid] = view;
                graphView.AddElement(view);
            }
        }

        if(targetGraph.edges != null)
        {
            foreach(ConversationEdgeData edge in targetGraph.edges)
            {
                if(edge == null) continue;
                if(!views.TryGetValue(edge.fromNodeGuid, out ConversationNodeView fromView)) continue;
                if(!views.TryGetValue(edge.toNodeGuid, out ConversationNodeView toView)) continue;

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
        evt.menu.AppendAction("Layout Graph", _ => { LayoutGraph(); RebuildGraphView(); });
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
    private void EnsureDefaultLayout()
    {
        if(targetGraph == null || targetGraph.nodes == null || targetGraph.nodes.Count < 2) return;

        for(int i = 0; i < targetGraph.nodes.Count; i++)
        {
            if(targetGraph.nodes[i] == null) continue;

            for(int j = i + 1; j < targetGraph.nodes.Count; j++)
            {
                if(targetGraph.nodes[j] == null) continue;

                if(Vector2.Distance(targetGraph.nodes[i].editorPosition, targetGraph.nodes[j].editorPosition) < 1f)
                {
                    LayoutGraph();
                    return;
                }
            }
        }
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

    private void RecordChange(string operation)
    {
        if(targetGraph != null) Undo.RecordObject(targetGraph, operation);
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
                DrawConditionList("Wait Conditions", selectedNodeData.conditions);
                break;
            case ConversationNodeKind.Entry:
                DrawConditionList("Entry Conditions", selectedNodeData.conditions);
                DrawEntrySettings();
                break;
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawBubbleSettings()
    {
        ChatBubble bubble = selectedNodeData.bubble;

        EditorGUILayout.LabelField("Bubble Data", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        Constants.ChatUser newUser = (Constants.ChatUser)EditorGUILayout.EnumPopup("Chat User", bubble.chatUser);
        float newDelay = EditorGUILayout.Slider("Delay Length", bubble.delayLength, 0f, 10f);
        float newTyping = EditorGUILayout.Slider("Typing Flag Length", bubble.typingFlagLength, 0f, 10f);
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
        EditorGUILayout.LabelField("Message", EditorStyles.boldLabel);
        GUI.SetNextControlName(TextAreaControlName);
        EditorGUI.BeginChangeCheck();
        editedMessage = GUILayout.TextArea(editedMessage, GUI.skin.textArea, GUILayout.Height(90));
        if(EditorGUI.EndChangeCheck())
        {
            RecordChange("Edit bubble message");
            bubble.message = editedMessage;
            bubble.SyncMarkables();
            PersistGraph();
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
        EditorGUILayout.LabelField("New Markable", EditorStyles.boldLabel);
        newMarkableMarkerIndex = EditorGUILayout.Popup("Marker Type", newMarkableMarkerIndex, GetMarkerTypeLabels());

        string selectionDisplay = "No active selection";
        if(currentSelectionStart >= 0 && currentSelectionEnd > currentSelectionStart)
        {
            string selected = editedMessage.Substring(currentSelectionStart, currentSelectionEnd - currentSelectionStart);
            selectionDisplay = $"'{selected}' ({currentSelectionStart} to {currentSelectionEnd - 1})";
        }
        EditorGUILayout.LabelField("Current Selection", selectionDisplay);

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
        EditorGUILayout.LabelField("Markables", EditorStyles.boldLabel);
        if(bubble.markables == null) bubble.markables = new List<Markable>();

        int markableToRemove = -1;
        for(int i = 0; i < bubble.markables.Count; i++)
        {
            Markable markable = bubble.markables[i];
            if(markable == null) continue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Markable {i + 1}", EditorStyles.boldLabel);

            int currentMarkerIndex = GetMarkerIndex(markable.markerType);
            int selectedMarkerIndex = EditorGUILayout.Popup("Marker Type", currentMarkerIndex, GetMarkerTypeLabels());
            if(selectedMarkerIndex != currentMarkerIndex)
            {
                RecordChange("Change markable type");
                markable.markerType = MarkerTypes[selectedMarkerIndex];
                PersistGraph();
            }

            string newSpan = EditorGUILayout.TextField("Anchor Text", markable.spanText);
            if(newSpan != markable.spanText)
            {
                RecordChange("Edit markable anchor text");
                markable.spanText = newSpan;
                PersistGraph();
            }

            int newOccurrence = EditorGUILayout.IntField("Occurrence", markable.occurrence);
            if(newOccurrence != markable.occurrence)
            {
                RecordChange("Edit markable occurrence");
                markable.occurrence = Mathf.Max(0, newOccurrence);
                PersistGraph();
            }

            EditorGUILayout.LabelField("Indexes", $"{markable.startIndex} - {markable.endIndex}");
            EditorGUILayout.LabelField("Resolved Text", markable.GetSelectedText(editedMessage));

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
        EditorGUILayout.LabelField("Choice Data", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        bool newBlocksDay = EditorGUILayout.Toggle("Blocks Day", selectedNodeData.blocksDay);
        if(EditorGUI.EndChangeCheck())
        {
            RecordChange("Toggle blocks day");
            selectedNodeData.blocksDay = newBlocksDay;
            PersistGraph();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Options (one draft per option)", EditorStyles.boldLabel);

        List<ConversationChoiceOptionData> options = selectedNodeData.options;
        if(options == null) selectedNodeData.options = options = new List<ConversationChoiceOptionData>();

        int optionToRemove = -1;
        for(int i = 0; i < options.Count; i++)
        {
            ConversationChoiceOptionData option = options[i];
            if(option == null) continue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Option {i + 1}", EditorStyles.boldLabel);

            string newPreview = EditorGUILayout.TextField("Preview Text", option.previewText);
            if(newPreview != option.previewText)
            {
                RecordChange("Edit option preview");
                option.previewText = newPreview;
                PersistGraph();
            }

            EditorGUI.BeginChangeCheck();
            Constants.ChatUser newUser = (Constants.ChatUser)EditorGUILayout.EnumPopup("Posted By", option.postedBubble.chatUser);
            if(EditorGUI.EndChangeCheck())
            {
                RecordChange("Edit option author");
                option.postedBubble.chatUser = newUser;
                PersistGraph();
            }

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

    private void DrawEffects(List<ConversationEffectData> effects)
    {
        EditorGUILayout.LabelField("Effects (applied on pick)", EditorStyles.boldLabel);
        if(effects == null) return;

        int effectToRemove = -1;
        for(int i = 0; i < effects.Count; i++)
        {
            ConversationEffectData effect = effects[i];
            if(effect == null) continue;

            EditorGUILayout.BeginHorizontal();
            ConversationEffectOperation newOp = (ConversationEffectOperation)EditorGUILayout.EnumPopup(effect.operation, GUILayout.Width(120));
            if(newOp != effect.operation)
            {
                RecordChange("Edit effect operation");
                effect.operation = newOp;
                PersistGraph();
            }

            switch(effect.operation)
            {
                case ConversationEffectOperation.SetFlag:
                case ConversationEffectOperation.RaiseEvent:
                    string newStringValue = EditorGUILayout.TextField(effect.stringValue);
                    if(newStringValue != effect.stringValue)
                    {
                        RecordChange("Edit effect value");
                        effect.stringValue = newStringValue;
                        PersistGraph();
                    }
                    break;
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

    private void DrawConditionList(string title, List<ConversationConditionClause> conditions)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        if(conditions == null) return;

        int clauseToRemove = -1;
        for(int i = 0; i < conditions.Count; i++)
        {
            ConversationConditionClause clause = conditions[i];
            if(clause == null) continue;

            EditorGUILayout.BeginHorizontal();
            ConversationConditionKind newKind = (ConversationConditionKind)EditorGUILayout.EnumPopup(clause.kind, GUILayout.Width(110));
            if(newKind != clause.kind)
            {
                RecordChange("Edit condition kind");
                clause.kind = newKind;
                PersistGraph();
            }

            switch(clause.kind)
            {
                case ConversationConditionKind.Event:
                case ConversationConditionKind.RequiredFlag:
                    string newStringValue = EditorGUILayout.TextField(clause.stringValue);
                    if(newStringValue != clause.stringValue)
                    {
                        RecordChange("Edit condition value");
                        clause.stringValue = newStringValue;
                        PersistGraph();
                    }
                    break;
                case ConversationConditionKind.DayMin:
                case ConversationConditionKind.DayMax:
                    int newIntValue = EditorGUILayout.IntField(clause.intValue);
                    if(newIntValue != clause.intValue)
                    {
                        RecordChange("Edit condition day");
                        clause.intValue = newIntValue;
                        PersistGraph();
                    }
                    break;
            }

            if(GUILayout.Button("-", GUILayout.Width(25))) clauseToRemove = i;
            EditorGUILayout.EndHorizontal();
        }

        if(clauseToRemove >= 0)
        {
            RecordChange("Remove condition");
            conditions.RemoveAt(clauseToRemove);
            PersistGraph();
        }

        if(GUILayout.Button("+ Add Condition"))
        {
            RecordChange("Add condition");
            conditions.Add(new ConversationConditionClause());
            PersistGraph();
        }
    }

    private void DrawEntrySettings()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Entry Scheduling", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        bool newExclusive = EditorGUILayout.Toggle("Exclusive", selectedNodeData.exclusive);
        string newGroupId = EditorGUILayout.TextField("Exclusive Group Id", selectedNodeData.exclusiveGroupId);
        if(EditorGUI.EndChangeCheck())
        {
            RecordChange("Edit entry scheduling");
            selectedNodeData.exclusive = newExclusive;
            selectedNodeData.exclusiveGroupId = newGroupId;
            PersistGraph();
        }

        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        bool newRepeatable = EditorGUILayout.Toggle("Is Repeatable", selectedNodeData.isRepeatable);
        if(EditorGUI.EndChangeCheck())
        {
            RecordChange("Toggle entry repeatable");
            selectedNodeData.isRepeatable = newRepeatable;
            PersistGraph();
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
}

/// GraphView shell with zoom/pan/box-select and a public SelectionChanged C# event
/// (Unity 6's GraphView exposes only a read-only selection list — selection changes
/// are dispatched by overriding the AddToSelection/RemoveFromSelection virtuals).
public class ConversationGraphView : GraphView
{
    public event System.Action SelectionChanged;

    public ConversationGraphView()
    {
        style.flexGrow = 1f;
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
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
