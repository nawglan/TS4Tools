/***************************************************************************
 *  Copyright (C) 2009, 2010 by Peter L Jones                              *
 *  pljones@users.sf.net                                                   *
 *                                                                         *
 *  This file is part of the Sims 3 Package Interface (s3pi)               *
 *                                                                         *
 *  s3pi is free software: you can redistribute it and/or modify           *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  s3pi is distributed in the hope that it will be useful,                *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with s3pi.  If not, see <http://www.gnu.org/licenses/>.          *
 ***************************************************************************/

namespace TS4Tools.Core.System;

/// <summary>
/// Represents an error in the length of an argument to a method
/// </summary>
public sealed class ArgumentLengthException : ArgumentException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArgumentLengthException"/> class.
    /// </summary>
    public ArgumentLengthException() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArgumentLengthException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ArgumentLengthException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArgumentLengthException"/> class with a predefined message based on
    /// <paramref name="argumentName"/> and <paramref name="expectedLength"/>.
    /// </summary>
    /// <param name="argumentName">Name of the method argument in error</param>
    /// <param name="expectedLength">Valid length of the argument</param>
    public ArgumentLengthException(string argumentName, int expectedLength)
        : base($"{argumentName} length must be {expectedLength}.", argumentName) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArgumentLengthException"/> class with a formatted error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="paramName">The name of the parameter that caused the current exception.</param>
    public ArgumentLengthException(string message, string paramName) : base(message, paramName) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArgumentLengthException"/> class with a specified error message and the exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public ArgumentLengthException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArgumentLengthException"/> class with a specified error message, the parameter name, and the exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="paramName">The name of the parameter that caused the current exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public ArgumentLengthException(string message, string paramName, Exception innerException) : base(message, paramName, innerException) { }
}
