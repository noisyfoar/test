using System;

namespace Dlisio.Core.Lis
{
    public sealed class LisParseException : Exception
    {
        public LisParseException(string message)
            : base(message)
        {
        }
    }
}
