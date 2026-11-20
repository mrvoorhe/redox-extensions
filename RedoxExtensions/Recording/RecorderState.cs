using System;

namespace RedoxExtensions.Recording;

/// <summary>One character's saved session, &lt;install&gt;\Recorder\State\&lt;Char&gt;.json.  Times are local.</summary>
public class RecorderState
{
    public string File { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime TimeoutAt { get; set; }
    public DateTime? StopAt { get; set; }
    public string JournalId { get; set; }
    public long LastSeq { get; set; }
}

public class RecorderAfterStop
{
    public string Program { get; set; }
    public string Args { get; set; }
}

public class RecorderConfig
{
    public string OutputRoot { get; set; }
    public RecorderAfterStop AfterStop { get; set; }
}

/// <summary>A loaded config with <see cref="RecorderConfig.OutputRoot"/> always set.</summary>
public class RecorderConfigResult
{
    public RecorderConfig Config { get; set; }

    /// <summary>True when the configured root was missing or empty and the default was used.</summary>
    public bool UsedFallback { get; set; }

    /// <summary>What to print to the user, or null.</summary>
    public string Warning { get; set; }
}
