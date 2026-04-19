using System;

public readonly struct GameStateChanged
{
    public readonly GameState PreviousState;
    public readonly GameState CurrentState;
    public readonly string Reason;
    public readonly string SourceName;
    public readonly int FrameCount;
    public readonly float RealtimeSinceStartup;
    public readonly DateTime UtcTimestamp;

    public GameStateChanged(
        GameState previousState,
        GameState currentState,
        string reason,
        string sourceName,
        int frameCount,
        float realtimeSinceStartup,
        DateTime utcTimestamp)
    {
        PreviousState = previousState;
        CurrentState = currentState;
        Reason = reason;
        SourceName = sourceName;
        FrameCount = frameCount;
        RealtimeSinceStartup = realtimeSinceStartup;
        UtcTimestamp = utcTimestamp;
    }
}
