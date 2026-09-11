using System;
using System.Collections.Generic;
using UnityEngine;

/// Owns one ConversationRunner per chat log. A tickable singleton that
/// bridges sequence events into entry evaluation, holds run-level flags, applies
/// choice effects, and snapshots/restores run-level chat state for the save file.
public class ConversationManager : Singleton<ConversationManager>
{
    private readonly Dictionary<ChatLog, ConversationRunner> runners = new Dictionary<ChatLog, ConversationRunner>();
    private readonly List<string> flags = new List<string>();
    private bool hadDayBlockingChoice = false;
    public List<string> Flags => flags;

    public bool HasUnresolvedDayBlockingChoice
    {
        get
        {
            foreach(ConversationRunner runner in runners.Values)
            {
                if(runner != null && runner.HasPendingDayBlockingChoice) return true;
            }

            return false;
        }
    }

    /// Raised when a day-blocking choice resolves and no other day-blocking choice remains unresolved — GameManager uses it to retry a deferred day end.
    public event Action OnDayBlockingChoiceResolved;

    /// Fired for every sequence event after all runners have evaluated entries and re-checked parked waits. 
    /// This is the single choke point reached by BOTH channel-raised events (via SequenceEventManager) and choice-effect-raised
    /// events (via ApplyEffects) — subscribers that care about day/work progression (e.g. GameManager) must hook here rather 
    /// than SequenceEventManager, so the runners see the event before a subscriber tears down the scene or saves the day.
    public event Action<Constants.SequenceEventType> OnSequenceEventRaised;

    private void Update()
    {
        foreach(ConversationRunner runner in runners.Values) runner.Tick(Time.deltaTime);
    }

    #region Runner Access

    public ConversationRunner GetRunnerForLog(ChatLog chatLog)
    {
        if(chatLog == null) return null;

        if(!runners.TryGetValue(chatLog, out ConversationRunner runner) || runner == null)
        {
            runner = new ConversationRunner(chatLog);
            runner.OnChoicePresented += OnRunnerChoicePresented;
            runner.OnChoiceResolved += OnRunnerChoiceResolved;
            runners[chatLog] = runner;
        }

        return runner;
    }

    private void OnRunnerChoicePresented()
    {
        hadDayBlockingChoice = HasUnresolvedDayBlockingChoice;
    }

    private void OnRunnerChoiceResolved()
    {
        if(hadDayBlockingChoice && !HasUnresolvedDayBlockingChoice)
        {
            hadDayBlockingChoice = false;
            OnDayBlockingChoiceResolved?.Invoke();
        }
        else
        {
            hadDayBlockingChoice = HasUnresolvedDayBlockingChoice;
        }
    }

    #endregion

    #region Events

    /// A sequence event fired — every runner evaluates its entries for it, then parked waits are re-checked (an event can release a parked thread). 
    /// Raised only AFTER the runner loop completes, so DayEnd/WorkEnd-gated entries and parked waits are evaluated before any OnSequenceEventRaised 
    /// subscriber acts on the event.
    public void OnSequenceEvent(Constants.SequenceEventType eventType)
    {
        foreach(ConversationRunner runner in runners.Values)
        {
            if(runner == null) continue;
            runner.EvaluateEntries(eventType);
            runner.ReevaluateParkedThreads(eventType);
        }

        OnSequenceEventRaised?.Invoke(eventType);
    }

    /// The day changed — every active chat log gets a runner (headless playback must not depend on a window being open), 
    /// one-shot entry activation guards for earlier days expire, then day-gated parked waits re-check.
    public void OnDayChanged()
    {
        EnsureRunnersForActiveLogs();

        foreach(ConversationRunner runner in runners.Values)
        {
            if(runner == null) continue;
            runner.ExpireStaleActivations();
            runner.ReevaluateParkedThreads(null);
        }
    }

    /// Creates a runner for every chat log active on the current day — assignment logs and chat client user logs alike — 
    /// so sequence events always reach them even when no window has been opened yet.
    private void EnsureRunnersForActiveLogs()
    {
        DayData dayData = SaveManager.Instance.GetDayData(GameManager.Instance.CurrentDayNumber);
        foreach(ChatLog chatLog in dayData.GetActiveAssignments()) GetRunnerForLog(chatLog);

        foreach(ChatUser chatUser in dayData.GetActiveChatClientUsers())
        {
            if(chatUser == null) continue;

            ChatLog chatLog = AddressableManager.Instance != null
                ? AddressableManager.Instance.RetrieveAddressable<ChatLog>(Constants.AddressablePrefixes.ChatLog + chatUser.name.ToLower())
                : null;

            GetRunnerForLog(chatLog);
        }
    }

    #endregion

    #region Flags & Effects

    private void SetFlag(string flagName)
    {
        if(string.IsNullOrEmpty(flagName)) return;
        if(!flags.Contains(flagName)) flags.Add(flagName);
    }

    /// Applies data-driven choice effects.
    public void ApplyEffects(List<ConversationEffectData> effects)
    {
        foreach(ConversationEffectData effect in effects)
        {
            switch(effect.operation)
            {
                case ConversationEffectOperation.SetFlag:
                    SetFlag(effect.stringValue);
                    break;

                case ConversationEffectOperation.RaiseEvent:
                    if(Enum.TryParse(effect.stringValue, true, out Constants.SequenceEventType eventType))
                    {
                        OnSequenceEvent(eventType);
                    }
                    else
                    {
                        Debug.LogWarning($"ConversationManager: could not parse RaiseEvent value '{effect.stringValue}'.");
                    }
                    break;
            }
        }
    }

    #endregion

    #region Save State

    /// Replaces all runtime chat state from a loaded save. Existing runner instances are
    /// REUSED rather than recreated, so chat log windows that subscribed to them stay wired.
    public void RestoreChatState(ChatRunState state)
    {
        if(state == null) return;
        flags.Clear();
        if(state.flags != null) flags.AddRange(state.flags);
        hadDayBlockingChoice = false;
        if(state.logs == null) return;

        foreach(LogChatState logState in state.logs)
        {
            if(logState == null || string.IsNullOrEmpty(logState.logName)) continue;

            ChatLog chatLog = AddressableManager.Instance != null
                ? AddressableManager.Instance.RetrieveAddressable<ChatLog>(Constants.AddressablePrefixes.ChatLog + logState.logName)
                : null;

            if(chatLog == null)
            {
                Debug.LogWarning($"ConversationManager: could not resolve ChatLog '{logState.logName}' while restoring chat state. Skipping.");
                continue;
            }

            GetRunnerForLog(chatLog)?.RestoreState(logState.history, logState.threads, logState.activations);
        }
    }

    public ChatRunState CaptureChatState()
    {
        ChatRunState state = new ChatRunState();
        if(flags != null) state.flags.AddRange(flags);

        foreach(KeyValuePair<ChatLog, ConversationRunner> pair in runners)
        {
            if(pair.Key == null || pair.Value == null) continue;
            LogChatState logState = new LogChatState { logName = pair.Key.logName };
            logState.history.AddRange(pair.Value.history);
            logState.threads.AddRange(pair.Value.CaptureThreadStates());
            logState.activations.AddRange(pair.Value.CaptureActivations());
            state.logs.Add(logState);
        }

        return state;
    }

    #endregion
}
