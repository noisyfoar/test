using System;

namespace Dlisio.Core.Parsing
{
    [Flags]
    public enum LogicalRecordSegmentAttributes : byte
    {
        None = 0,
        ExplicitlyFormatted = 1 << 7,
        HasPredecessor = 1 << 6,
        HasSuccessor = 1 << 5,
        Encrypted = 1 << 4,
        HasEncryptionPacket = 1 << 3,
        HasChecksum = 1 << 2,
        HasTrailingLength = 1 << 1,
        HasPadding = 1 << 0
    }
}
