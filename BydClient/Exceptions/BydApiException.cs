using System;
using System.Collections.Generic;
using System.Text;

namespace BydClient.Exceptions;

/// <summary>
/// Represents an error that occurred while interacting with the BYD API.
/// </summary>
public class BydApiException : BydException
{
    /// <summary>
    /// Gets the error code returned by the BYD API.
    /// </summary>
    public int ErrorCode { get; }

    /// <summary>
    /// Gets the API endpoint that was called when the exception occurred.
    /// </summary>
    public string Endpoint { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BydApiException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="code">A numeric value that indicates the error code.</param>
    /// <param name="endpoint">The API endpoint that was called.</param>
    /// <param name="previous">The exception that is the cause of the current exception.</param>
    public BydApiException(string message, int code = 0, string endpoint = "", Exception? previous = null)
        : base(message, previous)
    {
        ErrorCode = code;
        Endpoint = endpoint;
        HResult = code;
    }
}