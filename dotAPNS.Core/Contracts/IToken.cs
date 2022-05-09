using System;
using System.Collections.Generic;
using System.Text;

namespace dotAPNS.Core.Contracts
{
    public interface IToken
    {
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        string? Value { get; }
#else
        string Value { get; }
#endif
        ApplePushType Type { get; }
        bool IsSandbox { get; }
    }
}
