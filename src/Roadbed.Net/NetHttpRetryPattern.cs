namespace Roadbed.Net;

/// <summary>
/// Http Retry Pattern entity.
/// </summary>
public class NetHttpRetryPattern
{
    #region Public Properties

    /// <summary>
    /// Gets or sets the multipler to calculate the amount of time to delay between each attempt.
    /// </summary>
    public int DelayMultiplierInSeconds
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the max number of attempts for the retry pattern.
    /// </summary>
    public int MaxAttempts
    {
        get;
        set;
    }

    #endregion Public Properties
}