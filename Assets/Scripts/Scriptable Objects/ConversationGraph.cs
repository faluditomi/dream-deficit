using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "ConversationGraph", menuName = "Scriptable Objects/ConversationGraph")]
public class ConversationGraph : ScriptableObject
{
    // a seed graph's bubbles are drawn statically when the log window is created,
    // with no delay and no typing indicator, and are not appended to played history
    public bool isSeed;
    public List<ConversationNodeData> nodes = new List<ConversationNodeData>();
    public List<ConversationEdgeData> edges = new List<ConversationEdgeData>();

    public ConversationNodeData GetNode(string guid)
    {
        if(nodes == null || string.IsNullOrEmpty(guid)) return null;
        return nodes.Find(node => node != null && node.guid == guid);
    }

    public List<ConversationEdgeData> GetEdgesFromOption(string nodeGuid, string optionGuid)
    {
        if(edges == null) return new List<ConversationEdgeData>();
        return edges.FindAll(edge => edge != null && edge.fromNodeGuid == nodeGuid && edge.fromOptionGuid == optionGuid);
    }

    // the target of the first non-choice edge leaving nodeGuid — null if none
    public ConversationNodeData GetNextNode(string nodeGuid)
    {
        if(edges == null) return null;
        ConversationEdgeData edge = edges.Find(e => e != null && e.fromNodeGuid == nodeGuid && string.IsNullOrEmpty(e.fromOptionGuid));
        if(edge == null || string.IsNullOrEmpty(edge.toNodeGuid)) return null;
        return GetNode(edge.toNodeGuid);
    }

#if UNITY_EDITOR
    public ConversationNodeData AddNode(ConversationNodeKind kind)
    {
        if(nodes == null) nodes = new List<ConversationNodeData>();

        ConversationNodeData node = new ConversationNodeData
        {
            guid = System.Guid.NewGuid().ToString(),
            kind = kind
        };

        nodes.Add(node);
        EditorUtility.SetDirty(this);
        return node;
    }

    public void RemoveNode(string guid)
    {
        if(nodes == null || string.IsNullOrEmpty(guid)) return;
        nodes.RemoveAll(node => node != null && node.guid == guid);

        if(edges != null)
        {
            edges.RemoveAll(edge => edge != null && (edge.fromNodeGuid == guid || edge.toNodeGuid == guid));
        }

        EditorUtility.SetDirty(this);
    }

    public void Connect(string fromNodeGuid, string toNodeGuid, string fromOptionGuid = "")
    {
        if(string.IsNullOrEmpty(fromNodeGuid) || string.IsNullOrEmpty(toNodeGuid)) return;
        if(edges == null) edges = new List<ConversationEdgeData>();

        // dedupe identical edges
        bool exists = edges.Exists(edge => edge != null &&
            edge.fromNodeGuid == fromNodeGuid &&
            edge.fromOptionGuid == fromOptionGuid &&
            edge.toNodeGuid == toNodeGuid);
        if(exists) return;

        edges.Add(new ConversationEdgeData
        {
            fromNodeGuid = fromNodeGuid,
            fromOptionGuid = fromOptionGuid,
            toNodeGuid = toNodeGuid
        });

        EditorUtility.SetDirty(this);
    }

    public void Disconnect(ConversationEdgeData edge)
    {
        if(edges == null || edge == null) return;
        edges.Remove(edge);
        EditorUtility.SetDirty(this);
    }
#endif
}
