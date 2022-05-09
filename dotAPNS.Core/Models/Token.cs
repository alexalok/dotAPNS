using dotAPNS.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace dotAPNS.Core.Models
{
#if NET5_0_OR_GREATER
    public record Token : IToken
    {
        public Token(string? value, ApplePushType type, bool isSandbox)
        {
            Value = value;
            Type = type;
            IsSandbox = isSandbox;
        }

        public string? Value { get; }

        public ApplePushType Type { get; }

        public bool IsSandbox { get; }
    }
#else
    public class Token : IToken
    {
        public Token(string? value, ApplePushType type, bool isSandbox)
        {
            Value = value;
            Type = type;
            IsSandbox = isSandbox;
        }

        public string? Value { get; }

        public ApplePushType Type { get; }

        public bool IsSandbox { get; }
    }

#endif
}
