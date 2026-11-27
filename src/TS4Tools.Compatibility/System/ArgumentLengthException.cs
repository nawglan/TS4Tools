// Source: legacy_references/Sims4Tools/CS System Classes/ArgumentLengthException.cs lines 22-53

namespace System;

/// <summary>
/// The exception that is thrown when an argument has an unexpected length.
/// </summary>
public class ArgumentLengthException : Exception
{
    /// <summary>Initializes a new instance.</summary>
    public ArgumentLengthException() { }

    /// <summary>Initializes a new instance with a message.</summary>
    /// <param name="message">The error message.</param>
    public ArgumentLengthException(string message) : base(message) { }

    /// <summary>Initializes a new instance with a message and inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ArgumentLengthException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>Initializes a new instance reporting that <paramref name="argument"/> must have length <paramref name="length"/>.</summary>
    /// <param name="argument">The argument name.</param>
    /// <param name="length">The required length.</param>
    /// <remarks>Source: ArgumentLengthException.cs line 44</remarks>
    public ArgumentLengthException(string argument, int length)
        : base(string.Format(CultureInfo.CurrentCulture, "{0} length must be {1}.", argument, length)) { }

    /// <summary>Initializes a new instance with a formatted message.</summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="formatparams">The format arguments.</param>
    /// <remarks>Source: ArgumentLengthException.cs line 51</remarks>
    public ArgumentLengthException(string format, params object[] formatparams)
        : base(string.Format(CultureInfo.CurrentCulture, format, formatparams)) { }
}
