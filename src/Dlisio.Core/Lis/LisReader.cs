using System;
using System.Collections.Generic;
using System.IO;

namespace Dlisio.Core.Lis
{
    public sealed class LisReader
    {
        public bool TrySkipNextLogicalRecord(Stream stream, out LisRecordInfo? info)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream must be readable.", nameof(stream));
            }

            if (!stream.CanSeek)
            {
                throw new ArgumentException("Stream must be seekable for TryRead operations.", nameof(stream));
            }

            long startPosition = stream.Position;
            if (startPosition >= stream.Length)
            {
                info = null;
                return false;
            }

            try
            {
                info = SkipNextLogicalRecord(stream);
                return true;
            }
            catch (LisParseException) when (RemainingBytesArePadding(stream, startPosition))
            {
                stream.Seek(0, SeekOrigin.End);
                info = null;
                return false;
            }
        }

        public bool TryReadNextLogicalRecord(Stream stream, out LisLogicalRecord? record)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream must be readable.", nameof(stream));
            }

            if (!stream.CanSeek)
            {
                throw new ArgumentException("Stream must be seekable for TryRead operations.", nameof(stream));
            }

            long startPosition = stream.Position;
            if (startPosition >= stream.Length)
            {
                record = null;
                return false;
            }

            try
            {
                record = ReadNextLogicalRecord(stream);
                return true;
            }
            catch (LisParseException) when (RemainingBytesArePadding(stream, startPosition))
            {
                stream.Seek(0, SeekOrigin.End);
                record = null;
                return false;
            }
        }

        public LisLogicalRecord ReadNextLogicalRecord(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream must be readable.", nameof(stream));
            }

            LisPhysicalRecordHeader firstPrh = ReadNextPhysicalRecordHeader(stream);
            if (firstPrh.HasPredecessor)
            {
                throw new LisParseException(
                    "Invalid LIS layout: first physical record in a logical record is marked as continuation.");
            }

            var payloadBuilder = new PayloadBuilder(Math.Max(0, firstPrh.Length - LisPhysicalRecordHeader.HeaderLength));
            LisLogicalRecordHeader? logicalRecordHeader = null;
            LisPhysicalRecordHeader currentHeader = firstPrh;
            int recordCount = 0;

            while (true)
            {
                recordCount++;

                int payloadLength = currentHeader.Length - LisPhysicalRecordHeader.HeaderLength - currentHeader.TrailerLength;
                if (payloadLength < 0)
                {
                    throw new LisParseException("Invalid LIS physical record length.");
                }

                byte[] payload = ReadExactly(stream, payloadLength, "LIS physical record payload");
                SkipBytes(stream, currentHeader.TrailerLength, "LIS physical record trailer");

                int payloadOffset = 0;
                if (!currentHeader.HasPredecessor)
                {
                    if (payload.Length < LisLogicalRecordHeader.HeaderLength)
                    {
                        throw new LisParseException(
                            "Invalid LIS physical record: first segment does not contain a full LRH.");
                    }

                    logicalRecordHeader = LisHeaderParser.ParseLogicalRecordHeader(payload, 0);
                    payloadOffset = LisLogicalRecordHeader.HeaderLength;
                }

                if (payload.Length > payloadOffset)
                {
                    payloadBuilder.Append(payload, payloadOffset, payload.Length - payloadOffset);
                }

                if (!currentHeader.HasSuccessor)
                {
                    break;
                }

                currentHeader = ReadNextPhysicalRecordHeader(stream);
                if (!currentHeader.HasPredecessor)
                {
                    throw new LisParseException(
                        "Invalid LIS layout: successor chain broken (missing predecessor bit in continuation record).");
                }
            }

            if (logicalRecordHeader == null)
            {
                throw new LisParseException("Unable to read LIS logical record header.");
            }

            return new LisLogicalRecord(logicalRecordHeader, payloadBuilder.ToArray(), recordCount);
        }

        public LisPhysicalRecordHeader ReadNextPhysicalRecordHeader(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream must be readable.", nameof(stream));
            }

            byte[] headerBytes = ReadExactly(stream, LisPhysicalRecordHeader.HeaderLength, "LIS physical record header");

            // Skip obvious 4-byte pad blocks (0x00... or 0x20...).
            while (LisHeaderParser.IsPadBytes(headerBytes, 0, headerBytes.Length))
            {
                headerBytes = ReadExactly(
                    stream,
                    LisPhysicalRecordHeader.HeaderLength,
                    "LIS physical record header after padding");
            }

            return LisHeaderParser.ParsePhysicalRecordHeader(headerBytes, 0);
        }

        public LisRecordInfo SkipNextLogicalRecord(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream must be readable.", nameof(stream));
            }

            if (!stream.CanSeek)
            {
                throw new ArgumentException("Stream must be seekable.", nameof(stream));
            }

            long startOffset = stream.Position;
            LisPhysicalRecordHeader currentHeader = ReadNextPhysicalRecordHeader(stream);

            if (currentHeader.HasPredecessor)
            {
                throw new LisParseException(
                    "Invalid LIS layout: first physical record in a logical record is marked as continuation.");
            }

            int physicalRecordCount = 0;
            int dataLength = 0;
            LisLogicalRecordHeader? lrh = null;

            while (true)
            {
                physicalRecordCount++;

                int payloadLength = currentHeader.Length - LisPhysicalRecordHeader.HeaderLength - currentHeader.TrailerLength;
                if (payloadLength < 0)
                {
                    throw new LisParseException("Invalid LIS physical record length.");
                }

                if (!currentHeader.HasPredecessor)
                {
                    if (payloadLength < LisLogicalRecordHeader.HeaderLength)
                    {
                        throw new LisParseException(
                            "Invalid LIS physical record: first segment does not contain a full LRH.");
                    }

                    byte[] lrhBytes = ReadExactly(stream, LisLogicalRecordHeader.HeaderLength, "LIS logical record header");
                    lrh = LisHeaderParser.ParseLogicalRecordHeader(lrhBytes, 0);

                    int remainder = payloadLength - LisLogicalRecordHeader.HeaderLength;
                    if (remainder > 0)
                    {
                        SkipBytes(stream, remainder, "LIS logical record data");
                        dataLength += remainder;
                    }
                }
                else
                {
                    if (payloadLength > 0)
                    {
                        SkipBytes(stream, payloadLength, "LIS logical record continuation data");
                        dataLength += payloadLength;
                    }
                }

                if (currentHeader.TrailerLength > 0)
                {
                    SkipBytes(stream, currentHeader.TrailerLength, "LIS physical record trailer");
                }

                if (!currentHeader.HasSuccessor)
                {
                    break;
                }

                currentHeader = ReadNextPhysicalRecordHeader(stream);
                if (!currentHeader.HasPredecessor)
                {
                    throw new LisParseException(
                        "Invalid LIS layout: successor chain broken (missing predecessor bit in continuation record).");
                }
            }

            if (lrh == null)
            {
                throw new LisParseException("Unable to read LIS logical record header.");
            }

            return new LisRecordInfo(
                startOffset,
                (LisRecordType)lrh.Type,
                lrh.Attributes,
                physicalRecordCount,
                dataLength);
        }

        private static byte[] ReadExactly(Stream stream, int count, string componentName)
        {
            var buffer = new byte[count];
            int totalRead = 0;
            while (totalRead < count)
            {
                int n = stream.Read(buffer, totalRead, count - totalRead);
                if (n == 0)
                {
                    throw new LisParseException("Unexpected end of stream while reading " + componentName + ".");
                }

                totalRead += n;
            }

            return buffer;
        }

        private static void SkipBytes(Stream stream, int count, string componentName)
        {
            if (count <= 0)
            {
                return;
            }

            ReadExactly(stream, count, componentName);
        }

        private static bool RemainingBytesArePadding(Stream stream, long fromPosition)
        {
            if (!stream.CanSeek || !stream.CanRead)
            {
                return false;
            }

            long originalPosition = stream.Position;
            try
            {
                stream.Position = fromPosition;
                long remaining = stream.Length - fromPosition;
                if (remaining <= 0)
                {
                    return true;
                }

                int first = stream.ReadByte();
                if (first < 0 || (first != 0x00 && first != 0x20))
                {
                    return false;
                }

                for (long i = 1; i < remaining; i++)
                {
                    int current = stream.ReadByte();
                    if (current != first)
                    {
                        return false;
                    }
                }

                return true;
            }
            finally
            {
                stream.Position = originalPosition;
            }
        }

        private sealed class PayloadBuilder
        {
            private byte[] _buffer;
            private int _length;

            public PayloadBuilder(int initialCapacity)
            {
                _buffer = new byte[Math.Max(initialCapacity, 16)];
                _length = 0;
            }

            public void Append(byte[] source, int offset, int count)
            {
                if (count <= 0)
                {
                    return;
                }

                EnsureCapacity(_length + count);
                Buffer.BlockCopy(source, offset, _buffer, _length, count);
                _length += count;
            }

            public byte[] ToArray()
            {
                var result = new byte[_length];
                if (_length > 0)
                {
                    Buffer.BlockCopy(_buffer, 0, result, 0, _length);
                }

                return result;
            }

            private void EnsureCapacity(int required)
            {
                if (required <= _buffer.Length)
                {
                    return;
                }

                int size = _buffer.Length;
                while (size < required)
                {
                    size *= 2;
                }

                var next = new byte[size];
                if (_length > 0)
                {
                    Buffer.BlockCopy(_buffer, 0, next, 0, _length);
                }

                _buffer = next;
            }
        }
    }
}
