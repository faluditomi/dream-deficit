using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// GraphView node view for one ConversationNodeData. The canvas is a pure view over
/// the data model (design D15): every view maps back to a node GUID, and choice
/// options render as separate output ports keyed by option GUID (design D3) so edges
/// survive option reordering.
public class ConversationNodeView : Node
{
    public ConversationNodeData NodeData { get; private set; }
    public string NodeGuid => NodeData != null ? NodeData.guid : string.Empty;
    public Port InputPort { get; private set; }

    // single output for Bubble / Wait / Entry nodes — edges from it have an empty option guid
    public Port OutputPort { get; private set; }

    // one output port per choice option; each port's userData is the option GUID
    private readonly List<Port> optionPorts = new List<Port>();

    // every preview Label (message / condition summaries) added to mainContainer —
    // tracked so RefreshPreview() can clear surgically without touching ports or
    // other container content
    private readonly List<Label> previewLabels = new List<Label>();

    public ConversationNodeView(ConversationNodeData nodeData)
    {
        NodeData = nodeData;

        SetPosition(new Rect(nodeData.editorPosition, Vector2.zero));

        // NOTE: Port.edgeConnector is read-only in Unity 6 — InstantiatePort() builds a
        //       Port with the default DefaultEdgeConnectorListener, which routes new
        //       edges into graphViewChanged.edgesToCreate. The editor window handles
        //       edge creation in OnGraphViewChanged and persists via graph.Connect().
        // A real port type is required: GraphView.GetCompatiblePorts() resolves connectability
        // through NodeAdapter.GetAdapter(Port.source, ...), and Port.source is a PortSource<T>
        // built from this type. null or a type without a registered adapter makes every port
        // incompatible. typeof(object) matches Unity's built-in PortSource<object> adapter, so
        // any output can connect to any input. The port name is blanked so the type name
        // ("Object") does not render as a label.
        // The input port is Multi so several nodes can point into the same node
        // (fan-in); a Single-capacity port would make the default EdgeConnectorListener
        // delete the previous connection when a second one is dragged onto it. The
        // output port stays Single because the runtime only follows the first outgoing
        // edge per node (ConversationRunner.GetNextNode) — fan-out is not supported.
        InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(object));
        InputPort.portName = "";
        inputContainer.Add(InputPort);

        switch(nodeData.kind)
        {
            case ConversationNodeKind.Choice:
                // one output port per option, keyed by the option GUID; kept at Single
                // capacity so each option leads to exactly one target
                if(nodeData.options != null)
                {
                    foreach(ConversationChoiceOptionData option in nodeData.options)
                    {
                        if(option == null) continue;
                        Port optionPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(object));
                        optionPort.userData = option.guid;
                        optionPort.portName = !string.IsNullOrEmpty(option.previewText) ? option.previewText : "(empty option)";
                        optionPorts.Add(optionPort);
                        outputContainer.Add(optionPort);
                    }
                }
                break;
            case ConversationNodeKind.End:
                break;
            default:
                // Single capacity: each node has at most one successor (the runtime
                // only follows the first outgoing edge, so fan-out is unsupported)
                OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(object));
                OutputPort.portName = "";
                outputContainer.Add(OutputPort);
                break;
        }

        RefreshPreview();
        RefreshExpandedState();
        RefreshPorts();
    }

    /// Rebuilds everything on the node that renders node data: the title, the
    /// message/condition preview labels, and the choice option port labels. Ports
    /// themselves are created once in the constructor — RefreshPreview only rewrites
    /// their portName in place, so fan-in/fan-out wiring survives (design D2).
    public void RefreshPreview()
    {
        foreach(Label label in previewLabels) label.RemoveFromHierarchy();
        previewLabels.Clear();

        switch(NodeData.kind)
        {
            case ConversationNodeKind.Bubble:
                title = "Bubble";
                AddMessagePreview(NodeData.bubble != null ? NodeData.bubble.message : string.Empty);
                break;
            case ConversationNodeKind.Choice:
                title = NodeData.blocksDay ? "Choice (blocks day)" : "Choice";
                break;
            case ConversationNodeKind.Wait:
                title = "Wait";
                AddConditionSummary(NodeData.conditions);
                break;
            case ConversationNodeKind.Entry:
                title = "Entry";
                if(NodeData.exclusive) title += " (exclusive)";
                if(NodeData.isRepeatable) title += " (repeatable)";
                AddConditionSummary(NodeData.conditions);
                break;
            case ConversationNodeKind.End:
            default:
                title = "End";
                break;
        }

        // choice options render as separate output ports — refresh their labels in
        // place without adding or removing ports (design D3)
        if(NodeData.kind == ConversationNodeKind.Choice)
        {
            foreach(Port optionPort in optionPorts)
            {
                if(!(optionPort.userData is string optionGuid)) continue;

                ConversationChoiceOptionData option = null;
                if(NodeData.options != null)
                {
                    foreach(ConversationChoiceOptionData candidate in NodeData.options)
                    {
                        if(candidate != null && candidate.guid == optionGuid)
                        {
                            option = candidate;
                            break;
                        }
                    }
                }

                optionPort.portName = option != null && !string.IsNullOrEmpty(option.previewText)
                    ? option.previewText
                    : "(empty option)";
            }
        }

        RefreshExpandedState();
        RefreshPorts();
    }

    public Port GetOutputPortForOption(string optionGuid)
    {
        foreach(Port optionPort in optionPorts)
        {
            if(optionPort.userData is string guid && guid == optionGuid) return optionPort;
        }
        return null;
    }

    private void AddMessagePreview(string message)
    {
        string preview = string.IsNullOrEmpty(message) ? "(empty message)" : message;
        if(preview.Length > 60) preview = preview.Substring(0, 60) + "...";
        Label label = new Label(preview);
        previewLabels.Add(label);
        mainContainer.Add(label);
    }

    private void AddConditionSummary(List<ConversationConditionClause> conditions)
    {
        if(conditions == null || conditions.Count == 0)
        {
            Label placeholder = new Label("(no conditions — always eligible)");
            previewLabels.Add(placeholder);
            mainContainer.Add(placeholder);
            return;
        }

        foreach(ConversationConditionClause condition in conditions)
        {
            if(condition == null) continue;
            Label label = new Label(DescribeCondition(condition));
            previewLabels.Add(label);
            mainContainer.Add(label);
        }
    }

    private string DescribeCondition(ConversationConditionClause condition)
    {
        switch(condition.kind)
        {
            case ConversationConditionKind.Event: return "event: " + condition.stringValue;
            case ConversationConditionKind.DayMin: return "day >= " + condition.intValue;
            case ConversationConditionKind.DayMax: return "day <= " + condition.intValue;
            case ConversationConditionKind.RequiredFlag: return "flag: " + condition.stringValue;
            default: return string.Empty;
        }
    }
}
