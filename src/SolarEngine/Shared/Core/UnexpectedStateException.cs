// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace SolarEngine.Shared.Core;

internal sealed class UnexpectedStateException : Exception
{
    public UnexpectedStateException()
    {
    }

    public UnexpectedStateException(string message)
        : base(message)
    {
    }

    public UnexpectedStateException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
