using System.Collections.Generic;
using UnityEngine;

/// Plain C# conversation playback engine for one chat log. Owns the
/// thread walk, the entry queue, parking at wait/choice nodes, and the pending
/// choice. The window (ChatLogController) only renders what this runner records,
/// so playback survives the window closing and save/load round trips.
public class ConversationRunner
{
    private enum ThreadPlayState { Playing, ParkedChoice, ParkedWait }

    private class Thread
    {
        public ConversationGraph graph;
        public ConversationNodeData currentNode;
        public ThreadPlayState playState;
        // timing for the currently playing bubble node
        public float phaseTimer;
        public bool isDelayPhase = true;
        public ConversationNodeData pendingChoiceNode;
    }

    private class QueuedEntry
    {
        public ConversationGraph graph;
        public ConversationNodeData entryNode;
    }

    public readonly List<PlayedBubbleRecord> history = new List<PlayedBubbleRecord>();
    private readonly List<ConversationEntryActivation> activations = new List<ConversationEntryActivation>();
    private readonly List<Thread> parkedThreads = new List<Thread>();
    private readonly Queue<QueuedEntry> entryQueue = new Queue<QueuedEntry>();
    // the playing thread or the thread parked at a choice — a pending choice occupies the log, parked wait threads do not
    private Thread activeThread;

    public ChatLog log { get; private set; }
    public bool HasPendingChoice => activeThread != null && activeThread.playState == ThreadPlayState.ParkedChoice;
    public ConversationNodeData PendingChoiceNode => HasPendingChoice ? activeThread.pendingChoiceNode : null;
    public bool HasPendingDayBlockingChoice => PendingChoiceNode != null && PendingChoiceNode.blocksDay;
    public bool IsBusy => activeThread != null;

    public event System.Action<PlayedBubbleRecord> OnBubbleStarted;
    public event System.Action<PlayedBubbleRecord> OnBubblePlayed;
    public event System.Action OnChoicePresented;
    public event System.Action OnChoiceResolved;

    public ConversationRunner(ChatLog log)
    {
        this.log = log;
    }

    #region Entry Evaluation

    /// Evaluates every entry node across the log's graphs for the given event type and
    /// queues the eligible ones. Locked logs receive nothing. Non-repeatable entries that already
    /// activated for this event today are skipped, so a repeat fire of the same event
    /// never re-queues them; repeatable entries may activate every time.
    public void EvaluateEntries(Constants.SequenceEventType eventType)
    {
        if(IsLogLocked()) return;
        if(log == null || log.Graphs == null) return;
        ExpireStaleActivations();
        List<QueuedEntry> eligible = new List<QueuedEntry>();

        foreach(ConversationGraph graph in log.Graphs)
        {
            if(graph == null || graph.isSeed || graph.nodes == null) continue;

            foreach(ConversationNodeData node in graph.nodes)
            {
                if(node == null || node.kind != ConversationNodeKind.Entry) continue;
                if(!ConditionsMatch(node.conditions, eventType)) continue;
                if(!node.isRepeatable && HasActivation(graph.name, node.guid, eventType)) continue;
                eligible.Add(new QueuedEntry { graph = graph, entryNode = node });
            }
        }

        // exclusive entries compete within their group — one random winner per group;
        // additive entries all queue. Activation is recorded only AFTER the winners are
        // selected, so exclusive candidates that lose the draw are never consumed.
        List<QueuedEntry> winners = new List<QueuedEntry>();
        Dictionary<string, List<QueuedEntry>> exclusiveGroups = new Dictionary<string, List<QueuedEntry>>();

        foreach(QueuedEntry entry in eligible)
        {
            if(entry.entryNode.exclusive)
            {
                string groupKey = string.IsNullOrEmpty(entry.entryNode.exclusiveGroupId)
                    ? entry.entryNode.guid
                    : entry.entryNode.exclusiveGroupId;

                if(!exclusiveGroups.ContainsKey(groupKey)) exclusiveGroups.Add(groupKey, new List<QueuedEntry>());
                exclusiveGroups[groupKey].Add(entry);
            }
            else
            {
                winners.Add(entry);
            }
        }

        foreach(List<QueuedEntry> group in exclusiveGroups.Values)
        {
            winners.Add(group[Random.Range(0, group.Count)]);
        }

        foreach(QueuedEntry winner in winners)
        {
            // a queued winner counts as activated even while another thread is busy;
            // repeatable entries are never recorded in the one-shot activation list
            if(!winner.entryNode.isRepeatable)
            {
                RecordActivation(winner.graph != null ? winner.graph.name : string.Empty, winner.entryNode.guid, eventType);
            }

            entryQueue.Enqueue(winner);
        }

        TryStartNextEntry();
    }

    /// Drops one-shot activation records belonging to an earlier day. The stored
    /// dayNumber is the source of truth, so a runner reused during StartDay keeps
    /// same-day guards — they only expire once the current day actually differs.
    public void ExpireStaleActivations()
    {
        if(activations.Count == 0) return;
        int currentDay = GameManager.Instance.CurrentDayNumber;
        activations.RemoveAll(activation => activation == null || activation.dayNumber != currentDay);
    }

    private bool HasActivation(string graphName, string nodeGuid, Constants.SequenceEventType eventType)
    {
        int currentDay = GameManager.Instance.CurrentDayNumber;

        foreach(ConversationEntryActivation activation in activations)
        {
            if(activation == null) continue;
            if(activation.dayNumber != currentDay) continue;
            if(activation.graphName != graphName) continue;
            if(activation.nodeGuid != nodeGuid) continue;
            if(activation.eventType != eventType.ToString()) continue;
            return true;
        }

        return false;
    }

    private void RecordActivation(string graphName, string nodeGuid, Constants.SequenceEventType eventType)
    {
        if(string.IsNullOrEmpty(graphName) || string.IsNullOrEmpty(nodeGuid)) return;
        if(HasActivation(graphName, nodeGuid, eventType)) return;

        int currentDay = GameManager.Instance != null ? GameManager.Instance.CurrentDayNumber : 1;
        activations.Add(new ConversationEntryActivation
        {
            graphName = graphName,
            nodeGuid = nodeGuid,
            eventType = eventType.ToString(),
            dayNumber = currentDay
        });
    }

    private bool HasExactActivation(List<ConversationEntryActivation> source, ConversationEntryActivation candidate)
    {
        if(source == null || candidate == null) return false;

        foreach(ConversationEntryActivation activation in source)
        {
            if(activation == null) continue;
            if(activation.graphName != candidate.graphName) continue;
            if(activation.nodeGuid != candidate.nodeGuid) continue;
            if(activation.eventType != candidate.eventType) continue;
            if(activation.dayNumber != candidate.dayNumber) continue;
            return true;
        }
        return false;
    }

    /// Re-checks threads parked at wait nodes. Locked logs never resume parked threads. The triggering 
    /// event type is passed through so a wait gated on a specific event can only resume when that event fires.
    public void ReevaluateParkedThreads(Constants.SequenceEventType? eventType = null)
    {
        if(IsLogLocked()) return;

        for(int i = parkedThreads.Count - 1; i >= 0; i--)
        {
            Thread thread = parkedThreads[i];
            if(thread == null) { parkedThreads.RemoveAt(i); continue; }
            ConversationNodeData waitNode = thread.currentNode;

            if(waitNode == null || waitNode.kind != ConversationNodeKind.Wait)
            {
                parkedThreads.RemoveAt(i);
                continue;
            }

            if(!ConditionsMatch(waitNode.conditions, eventType)) continue;
            if(activeThread != null) continue; // log is busy — re-check on the next event/day change
            parkedThreads.RemoveAt(i);
            activeThread = thread;
            AdvanceToNext(thread);
            ProcessCurrentNode();
        }
    }

    #endregion

    #region Choice Resolution

    /// The player picked a draft option: apply its effects, post the player's bubble
    /// instantly, then continue the thread down the chosen edge.
    public void ResolveChoice(string optionGuid)
    {
        if(!HasPendingChoice) return;
        Thread thread = activeThread;
        ConversationNodeData choiceNode = thread.pendingChoiceNode;
        if(choiceNode == null || choiceNode.options == null) return;
        ConversationChoiceOptionData option = choiceNode.options.Find(o => o != null && o.guid == optionGuid);
        if(option == null) return;

        // 1) effects first — an EndDay effect fires while this choice is still pending,
        //    which makes GameManager defer the day end until the choice clears
        if(ConversationManager.Instance != null) ConversationManager.Instance.ApplyEffects(option.effects);
        // 2) the player's reply posts instantly — no delay, no typing phase
        string graphName = thread.graph != null ? thread.graph.name : string.Empty;

        PlayedBubbleRecord record = new PlayedBubbleRecord
        {
            graphName = graphName,
            nodeGuid = choiceNode.guid,
            optionGuid = optionGuid
        };

        history.Add(record);
        OnBubblePlayed?.Invoke(record);
        // 3) clear the pending choice before continuing
        thread.playState = ThreadPlayState.Playing;
        thread.pendingChoiceNode = null;
        // 4) resolve event after the state is cleared
        OnChoiceResolved?.Invoke();
        // 5) continue down the chosen edge
        ConversationNodeData target = GetOptionTarget(thread, choiceNode, optionGuid);

        if(target == null)
        {
            FinishActiveThread();
            return;
        }

        thread.currentNode = target;
        ProcessCurrentNode();
    }

    #endregion

    #region Playback

    public void Tick(float deltaTime)
    {
        if(activeThread == null || activeThread.playState != ThreadPlayState.Playing) return;
        Thread thread = activeThread;
        ConversationNodeData node = thread.currentNode;
        if(node == null || node.kind != ConversationNodeKind.Bubble) return;
        thread.phaseTimer -= deltaTime;
        if(thread.phaseTimer > 0f) return;

        if(thread.isDelayPhase)
        {
            // pre-message delay elapsed — typing indicator phase begins
            thread.isDelayPhase = false;
            thread.phaseTimer = Mathf.Max(0f, node.bubble != null ? node.bubble.typingFlagLength : 0f);
            OnBubbleStarted?.Invoke(new PlayedBubbleRecord { graphName = thread.graph != null ? thread.graph.name : string.Empty, nodeGuid = node.guid });
        }
        else
        {
            // typing phase elapsed — the message posts. History is appended BEFORE the
            // thread advances, so a persisted Playing thread is always at an unposted node
            string graphName = thread.graph != null ? thread.graph.name : string.Empty;
            PlayedBubbleRecord record = new PlayedBubbleRecord { graphName = graphName, nodeGuid = node.guid };
            history.Add(record);
            OnBubblePlayed?.Invoke(record);
            AdvanceToNext(thread);
            ProcessCurrentNode();
        }
    }

    private void ProcessCurrentNode()
    {
        Thread thread = activeThread;
        if(thread == null) return;
        ConversationNodeData node = thread.currentNode;

        if(node == null)
        {
            FinishActiveThread();
            return;
        }

        switch(node.kind)
        {
            case ConversationNodeKind.Bubble:
                thread.isDelayPhase = true;
                thread.phaseTimer = Mathf.Max(0f, node.bubble != null ? node.bubble.delayLength : 0f);
                break;

            case ConversationNodeKind.Choice:
                thread.playState = ThreadPlayState.ParkedChoice;
                thread.pendingChoiceNode = node;
                OnChoicePresented?.Invoke();
                break;

            case ConversationNodeKind.Wait:
                if(ConditionsMatch(node.conditions, null))
                {
                    AdvanceToNext(thread);
                    ProcessCurrentNode();
                }
                else
                {
                    thread.playState = ThreadPlayState.ParkedWait;
                    parkedThreads.Add(thread);
                    activeThread = null;
                    TryStartNextEntry();
                }
                break;

            case ConversationNodeKind.Entry:
                // entries are thread starting points — never reached mid-walk normally
                AdvanceToNext(thread);
                ProcessCurrentNode();
                break;

            case ConversationNodeKind.End:
            default:
                FinishActiveThread();
                break;
        }
    }

    private void FinishActiveThread()
    {
        activeThread = null;
        TryStartNextEntry();
    }

    private void TryStartNextEntry()
    {
        if(activeThread != null || entryQueue.Count == 0) return;
        QueuedEntry next = entryQueue.Dequeue();
        activeThread = new Thread { graph = next.graph, currentNode = next.entryNode, playState = ThreadPlayState.Playing };
        ProcessCurrentNode();
    }

    private void AdvanceToNext(Thread thread)
    {
        if(thread == null || thread.graph == null || thread.currentNode == null)
        {
            if(thread != null) thread.currentNode = null;
            return;
        }

        ConversationNodeData next = thread.graph.GetNextNode(thread.currentNode.guid);
        thread.currentNode = next;
    }

    private ConversationNodeData GetOptionTarget(Thread thread, ConversationNodeData choiceNode, string optionGuid)
    {
        if(thread == null || thread.graph == null || choiceNode == null) return null;
        List<ConversationEdgeData> edges = thread.graph.GetEdgesFromOption(choiceNode.guid, optionGuid);
        if(edges == null || edges.Count == 0) return null;
        return thread.graph.GetNode(edges[0].toNodeGuid);
    }

    #endregion

    #region Save State

    public void RestoreState(List<PlayedBubbleRecord> historyToRestore, List<ConversationThreadState> threadsToRestore, List<ConversationEntryActivation> activationsToRestore)
    {
        history.Clear();
        parkedThreads.Clear();
        activeThread = null;
        while(entryQueue.Count > 0) entryQueue.Dequeue();
        activations.Clear();
        if(historyToRestore != null) history.AddRange(historyToRestore);
        RestoreActivations(activationsToRestore);
        if(threadsToRestore == null) return;

        foreach(ConversationThreadState state in threadsToRestore)
        {
            if(state == null) continue;
            ConversationGraph graph = FindGraphByName(state.graphName);
            ConversationNodeData node = graph != null ? graph.GetNode(state.nodeGuid) : null;

            if(graph == null || node == null)
            {
                Debug.LogWarning($"ConversationRunner: could not restore thread at graph '{state.graphName}' node '{state.nodeGuid}'. Skipping.");
                continue;
            }

            Thread thread = new Thread { graph = graph, currentNode = node, playState = ThreadPlayState.Playing };

            switch(state.stateKind)
            {
                case ConversationThreadStateKind.Playing:
                    // the unposted-node invariant makes resuming from the saved node safe
                    if(activeThread == null)
                    {
                        activeThread = thread;
                        ProcessCurrentNode();
                    }
                    break;

                case ConversationThreadStateKind.ParkedChoice:
                    if(activeThread == null && node.kind == ConversationNodeKind.Choice)
                    {
                        activeThread = thread;
                        thread.playState = ThreadPlayState.ParkedChoice;
                        thread.pendingChoiceNode = node;
                        OnChoicePresented?.Invoke();
                    }
                    break;

                case ConversationThreadStateKind.ParkedWait:
                    thread.playState = ThreadPlayState.ParkedWait;
                    parkedThreads.Add(thread);
                    break;
            }
        }
    }

    public List<ConversationThreadState> CaptureThreadStates()
    {
        List<ConversationThreadState> states = new List<ConversationThreadState>();

        if(activeThread != null)
        {
            ConversationThreadStateKind kind = activeThread.playState == ThreadPlayState.ParkedChoice
                ? ConversationThreadStateKind.ParkedChoice
                : activeThread.playState == ThreadPlayState.ParkedWait
                    ? ConversationThreadStateKind.ParkedWait
                    : ConversationThreadStateKind.Playing;

            states.Add(new ConversationThreadState
            {
                graphName = activeThread.graph != null ? activeThread.graph.name : string.Empty,
                nodeGuid = activeThread.currentNode != null ? activeThread.currentNode.guid : string.Empty,
                stateKind = kind
            });
        }

        foreach(Thread parked in parkedThreads)
        {
            if(parked == null) continue;

            states.Add(new ConversationThreadState
            {
                graphName = parked.graph != null ? parked.graph.name : string.Empty,
                nodeGuid = parked.currentNode != null ? parked.currentNode.guid : string.Empty,
                stateKind = ConversationThreadStateKind.ParkedWait
            });
        }

        return states;
    }

    public List<ConversationEntryActivation> CaptureActivations()
    {
        List<ConversationEntryActivation> captured = new List<ConversationEntryActivation>();

        foreach(ConversationEntryActivation activation in activations)
        {
            if(activation == null) continue;
            if(HasExactActivation(captured, activation)) continue;
            captured.Add(activation);
        }

        return captured;
    }

    private void RestoreActivations(List<ConversationEntryActivation> activationsToRestore)
    {
        if(activationsToRestore == null) return;

        foreach(ConversationEntryActivation activation in activationsToRestore)
        {
            if(activation == null) continue;
            if(string.IsNullOrEmpty(activation.graphName) || string.IsNullOrEmpty(activation.nodeGuid) || string.IsNullOrEmpty(activation.eventType)) continue;
            if(HasExactActivation(activations, activation)) continue;
            activations.Add(activation);
        }
    }

    #endregion

    #region Helpers

    private bool ConditionsMatch(List<ConversationConditionClause> conditions, Constants.SequenceEventType? eventType)
    {
        if(conditions == null || conditions.Count == 0) return true;
        int currentDay = GameManager.Instance != null ? GameManager.Instance.CurrentDayNumber : 1;
        List<string> flags = ConversationManager.Instance != null ? ConversationManager.Instance.Flags : null;

        foreach(ConversationConditionClause clause in conditions)
        {
            if(clause == null) continue;
            if(!clause.Matches(eventType, currentDay, flags)) return false;
        }

        return true;
    }

    private bool IsLogLocked()
    {
        if(log == null || SaveManager.Instance == null || GameManager.Instance == null) return false;
        DayData dayData = SaveManager.Instance.GetDayData(GameManager.Instance.CurrentDayNumber);
        return dayData != null && dayData.IsLogLocked(log.logName);
    }

    private ConversationGraph FindGraphByName(string graphName)
    {
        if(log == null || log.Graphs == null || string.IsNullOrEmpty(graphName)) return null;
        return log.Graphs.Find(g => g != null && g.name == graphName);
    }

    #endregion
}
