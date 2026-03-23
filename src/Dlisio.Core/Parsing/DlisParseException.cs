using System;

namespace Dlisio.Core.Parsing
{
    public sealed class DlisParseException : Exception
    {
        public DlisParseException(string message)
            : base(message)
        {
        }
    }
}
