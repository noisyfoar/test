using System;
using System.Collections.Generic;
using System.IO;

namespace Dlisio.Core.Parsing
{
    public sealed class DlisReader
    {
        public LogicalRecord ReadNextLogicalRecord(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream must be readable.", nameof(stream));
            }

            var segments = new List<LogicalRecordSegment>();

            LogicalRecordSegment first = ReadNextSegment(stream);
            if (!first.Header.IsFirstSegment)
            {
                throw new DlisParseException(
                    "Invalid logical record sequence: first segment in stream is not marked as first.");
            }

            segments.Add(first);

            while (!segments[segments.Count - 1].Header.IsLastSegment)
            {
                LogicalRecordSegment next = ReadNextSegment(stream);
                segments.Add(next);
            }

            return LogicalRecordAssembler.Assemble(segments);
        }

        public LogicalRecordSegment ReadNextSegment(Stream stream)
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
            ReadExactly(stream, headerBytes, "Logical Record Segment Header");

            LogicalRecordSegmentHeader header = LogicalRecordSegmentHeaderParser.Parse(headerBytes);

            byte[] segmentBytes = new byte[header.SegmentLength];
            Buffer.BlockCopy(headerBytes, 0, segmentBytes, 0, headerBytes.Length);

            int remainingBytes = segmentBytes.Length - headerBytes.Length;
            if (remainingBytes > 0)
            {
                ReadExactly(
                    stream,
                    segmentBytes,
                    headerBytes.Length,
                    remainingBytes,
                    "Logical Record Segment body/trailer");
            }

            return LogicalRecordSegmentParser.Parse(segmentBytes);
        }

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
            ReadExactly(stream, headerBytes, "Logical Record Segment Header");

            return LogicalRecordSegmentHeaderParser.Parse(headerBytes);
        }

        private static void ReadExactly(
            Stream stream,
            byte[] buffer,
            string componentName)
        {
            ReadExactly(stream, buffer, 0, buffer.Length, componentName);
        }

        private static void ReadExactly(
            Stream stream,
            byte[] buffer,
            int offset,
            int count,
            string componentName)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int bytesRead = stream.Read(buffer, offset + totalRead, count - totalRead);
                if (bytesRead == 0)
                {
                    throw new DlisParseException(
                        "Unexpected end of stream while reading " + componentName + ".");
                }

                totalRead += bytesRead;
            }
        }
    }
}
