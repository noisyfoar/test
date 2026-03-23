using System;
using System.IO;

namespace Dlisio.Core.Parsing
{
    public sealed class DlisReader
    {
        public LogicalRecordSegmentHeader ReadFirstSegmentHeader(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream must be readable.", nameof(stream));
            }

            byte[] headerBytes = new byte[LogicalRecordSegmentHeaderParser.HeaderLength];
            int bytesRead = stream.Read(headerBytes, 0, headerBytes.Length);
            if (bytesRead != headerBytes.Length)
            {
                throw new DlisParseException(
                    "Unexpected end of stream while reading Logical Record Segment Header.");
            }

            return LogicalRecordSegmentHeaderParser.Parse(headerBytes);
        }
    }
}
