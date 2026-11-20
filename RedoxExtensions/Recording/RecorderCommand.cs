using System;
using System.Collections.Generic;

namespace RedoxExtensions.Recording;

public interface IRecorderControl
{
    void Start(TimeSpan timeout);

    void Stop(TimeSpan? delay);

    void Status();
}

public static class RecorderCommand
{
    public const string Usage = "Usage : /re recorder|rec start [timeout] | stop [delay] | status   (durations like 30s, 5m, 4.5h)";

    /// <summary>Handles the arguments after "/re recorder".  Bad arguments print usage and do nothing.</summary>
    public static void Handle(IList<string> arguments, IRecorderControl recorder, Action<string> chat)
    {
        var sub = arguments.Count > 0 ? arguments[0].Trim().ToLowerInvariant() : "status";

        switch (sub)
        {
            case "start":
                {
                    var timeout = RecorderRules.DefaultTimeout;
                    if (arguments.Count > 2 || (arguments.Count == 2 && !DurationParser.TryParse(arguments[1], out timeout)))
                    {
                        chat(Usage);
                        return;
                    }

                    recorder.Start(timeout);
                    return;
                }

            case "stop":
                {
                    TimeSpan? delay = null;
                    if (arguments.Count == 2)
                    {
                        if (!DurationParser.TryParse(arguments[1], out var parsed))
                        {
                            chat(Usage);
                            return;
                        }

                        delay = parsed;
                    }
                    else if (arguments.Count > 2)
                    {
                        chat(Usage);
                        return;
                    }

                    recorder.Stop(delay);
                    return;
                }

            case "status":
                if (arguments.Count > 1)
                {
                    chat(Usage);
                    return;
                }

                recorder.Status();
                return;

            default:
                chat(Usage);
                return;
        }
    }
}
