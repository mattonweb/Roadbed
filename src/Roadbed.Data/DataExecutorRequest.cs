namespace Roadbed.Data;

using System;

/// <summary>
/// Request entity for data executor operations.
/// </summary>
public class DataExecutorRequest
{
    #region Private Fields

    private int? _commandTimeoutInSeconds;
    private TimeSpan _delayBetweenRetries = TimeSpan.FromMilliseconds(100);
    private int _maxRetries = 3;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="DataExecutorRequest"/> class.
    /// </summary>
    /// <param name="query">SQL query to be executed or processed.</param>
    public DataExecutorRequest(string query)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        this.Query = query;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets or sets an optional per-execution SQL command timeout, in seconds.
    /// </summary>
    /// <value>
    /// <see langword="null"/> (the default) falls back to the connection's
    /// <see cref="DataConnecionString.CommandTimeoutInSeconds"/>. <c>0</c> means no
    /// timeout (wait indefinitely) — reserve it for known-long ETL operations.
    /// </value>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is negative.</exception>
    /// <remarks>
    /// Set this only with a code comment explaining why. The connection default is
    /// deliberately short so that a slow query prompts SQL optimization or a design
    /// conversation rather than a silently extended timeout.
    /// </remarks>
    public int? CommandTimeoutInSeconds
    {
        get => this._commandTimeoutInSeconds;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Command timeout cannot be negative. Use null for the connection default, or 0 for no timeout.");
            }

            this._commandTimeoutInSeconds = value;
        }
    }

    /// <summary>
    /// Gets or sets the delay between retry attempts.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is negative.</exception>
    public TimeSpan DelayBetweenRetries
    {
        get => this._delayBetweenRetries;
        set
        {
            if (value < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Delay cannot be negative.");
            }

            this._delayBetweenRetries = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the delay should increase exponentially with each retry attempt (exponential backoff).
    /// </summary>
    /// <remarks>
    /// When enabled, the delay is multiplied by the attempt number. For example, with a 100ms base delay:
    /// Attempt 1: 100ms, Attempt 2: 200ms, Attempt 3: 300ms.
    /// </remarks>
    public bool DelayMultiplierEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of retries.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is negative.</exception>
    public int MaxRetries
    {
        get => this._maxRetries;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Max retries cannot be negative.");
            }

            this._maxRetries = value;
        }
    }

    /// <summary>
    /// Gets or sets the parameters associated with the current operation.
    /// </summary>
    public object? Parameters { get; set; }

    /// <summary>
    /// Gets the SQL query to be executed or processed.
    /// </summary>
    public string Query { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether retries are enabled.
    /// </summary>
    public bool RetriesEnabled { get; set; } = true;

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Resolves the effective command timeout, in seconds: the per-execution
    /// <see cref="CommandTimeoutInSeconds"/> override when set, otherwise the
    /// supplied connection default.
    /// </summary>
    /// <param name="connectionDefaultInSeconds">The connection's default command timeout, typically <see cref="DataConnecionString.CommandTimeoutInSeconds"/>.</param>
    /// <returns>The command timeout to apply to this execution.</returns>
    public int ResolveCommandTimeoutInSeconds(int connectionDefaultInSeconds)
    {
        return this.CommandTimeoutInSeconds ?? connectionDefaultInSeconds;
    }

    #endregion Public Methods
}