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

    public ConversationNodeView(ConversationNodeData nodeData)
    {
        NodeData = nodeData;

        switch(nodeData.kind)
        {
            case ConversationNodeKind.Bubble:
                title = "Bubble";
                AddMessagePreview(nodeData.bubble != null ? nodeData.bubble.message : string.Empty);
                break;
            case ConversationNodeKind.Choice:
                title = nodeData.blocksDay ? "Choice (blocks day)" : "Choice";
                break;
            case ConversationNodeKind.Wait:
                title = "Wait";
                AddConditionSummary(nodeData.conditions);
                break;
            case ConversationNodeKind.Entry:
                title = "Entry";
                if(nodeData.exclusive) title += " (exclusive)";
                if(nodeData.isRepeatable) title += " (repeatable)";
                AddConditionSummary(nodeData.conditions);
                break;
            case ConversationNodeKind.End:
            default:
                title = "End";
                break;
        }

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
        mainContainer.Add(new Label(preview));
    }

    private void AddConditionSummary(List<ConversationConditionClause> conditions)
    {
        if(conditions == null || conditions.Count == 0)
        {
            mainContainer.Add(new Label("(no conditions — always eligible)"));
            return;
        }

        foreach(ConversationConditionClause condition in conditions)
        {
            if(condition == null) continue;
            mainContainer.Add(new Label(DescribeCondition(condition)));
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
