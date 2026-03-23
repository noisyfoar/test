using System;

namespace Lis.Core.Lis
{
    public sealed class LisParseException : Exception
    {
        public LisParseException(string message)
            : base(message)
        {
        }
    }
}
