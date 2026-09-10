using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChatLog", menuName = "Scriptable Objects/ChatLog")]
public class ChatLog : ScriptableObject
{
    public string logName;
    private List<ConversationGraph> cachedGraphs;
    public List<ConversationGraph> Graphs => cachedGraphs ??= GetRelatedConversationGraphs();

    private List<ConversationGraph> GetRelatedConversationGraphs()
    {
        List<ConversationGraph> relatedGraphs = new List<ConversationGraph>();
        if(name == null || name.Length == 0) return null;

        AddressableManager.Instance.RetrieveAddressablesByLabel<ConversationGraph>(Constants.AddressableLabels.ConversationGraph).ForEach(graph =>
        {
            if(graph == null || graph.nodes == null || graph.name == null) return;
            if(graph.name.StartsWith(name)) relatedGraphs.Add(graph);
        });

        cachedGraphs = relatedGraphs;
        return relatedGraphs;
    }

    public ConversationNodeData GetNode(string nodeGuid)
    {
        if(Graphs == null || string.IsNullOrEmpty(nodeGuid)) return null;

        foreach(ConversationGraph graph in Graphs)
        {
            if(graph == null) continue;
            ConversationNodeData node = graph.GetNode(nodeGuid);
            if(node != null) return node;
        }

        return null;
    }

    public ConversationNodeData GetNode(string graphName, string nodeGuid)
    {
        if(Graphs == null || string.IsNullOrEmpty(graphName) || string.IsNullOrEmpty(nodeGuid)) return null;
        ConversationGraph graph = Graphs.Find(g => g != null && g.name == graphName);
        return graph != null ? graph.GetNode(nodeGuid) : null;
    }

    /// Resolves a history record to the bubble that actually played — player replies
    /// live on the chosen option, everything else on the node itself.
    public ChatBubble ResolvePlayedBubble(PlayedBubbleRecord record)
    {
        if(record == null) return null;
        ConversationNodeData node = GetNode(record.graphName, record.nodeGuid);
        if(node == null) return null;

        if(!string.IsNullOrEmpty(record.optionGuid) && node.kind == ConversationNodeKind.Choice && node.options != null)
        {
            ConversationChoiceOptionData option = node.options.Find(o => o != null && o.guid == record.optionGuid);
            return option != null ? option.postedBubble : null;
        }

        return node.bubble;
    }

    public ChatBubble GetLastSeedBubble()
    {
        if(Graphs == null) return null;

        for(int i = Graphs.Count - 1; i >= 0; i--)
        {
            ConversationGraph graph = Graphs[i];
            if(graph == null || !graph.isSeed || graph.nodes == null) continue;

            for(int j = graph.nodes.Count - 1; j >= 0; j--)
            {
                ConversationNodeData node = graph.nodes[j];
                if(node != null && node.kind == ConversationNodeKind.Bubble && node.bubble != null)
                {
                    return node.bubble;
                }
            }
        }

        return null;
    }
}
