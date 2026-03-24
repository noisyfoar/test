using System;
using System.Collections.Generic;
using System.IO;

namespace Lis.Core.Lis
{
    public sealed class LisReader
    {
        private readonly byte[] _physicalHeaderBuffer = new byte[LisPhysicalRecordHeader.HeaderLength];
        private readonly byte[] _logicalHeaderProbeBuffer = new byte[LisLogicalRecordHeader.HeaderLength + 4];

        /// <summary>
        /// Подробно выполняет операцию «TrySkipNextLogicalRecord» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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

        /// <summary>
        /// Подробно выполняет операцию «TryReadNextLogicalRecord» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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

        /// <summary>
        /// Подробно выполняет операцию «TrySkipNextImplicitDataRecord» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public bool TrySkipNextImplicitDataRecord(Stream stream, out LisRecordInfo? info)
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

            if (!LooksLikeImplicitRecord(stream))
            {
                info = null;
                return false;
            }

            try
            {
                info = SkipNextImplicitDataRecord(stream);
                return true;
            }
            catch (LisParseException) when (RemainingBytesArePadding(stream, startPosition))
            {
                stream.Seek(0, SeekOrigin.End);
                info = null;
                return false;
            }
        }

        /// <summary>
        /// Подробно выполняет операцию «TryReadNextImplicitDataRecord» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public bool TryReadNextImplicitDataRecord(Stream stream, out LisLogicalRecord? record)
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

            if (!LooksLikeImplicitRecord(stream))
            {
                record = null;
                return false;
            }

            try
            {
                record = ReadNextImplicitDataRecord(stream);
                return true;
            }
            catch (LisParseException) when (RemainingBytesArePadding(stream, startPosition))
            {
                stream.Seek(0, SeekOrigin.End);
                record = null;
                return false;
            }
        }

        /// <summary>
        /// Подробно выполняет операцию «ReadNextLogicalRecord» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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

                if (!currentHeader.HasPredecessor)
                {
                    if (payloadLength < LisLogicalRecordHeader.HeaderLength)
                    {
                        throw new LisParseException(
                            "Invalid LIS physical record: first segment does not contain a full LRH.");
                    }

                    int probeCount = Math.Min(payloadLength, _logicalHeaderProbeBuffer.Length);
                    ReadExactly(stream, _logicalHeaderProbeBuffer, 0, probeCount, "LIS logical record header probe");

                    int lrhOffset;
                    if (!TryParseLogicalRecordHeaderFromProbe(_logicalHeaderProbeBuffer, probeCount, payloadLength, out LisLogicalRecordHeader parsedHeader, out lrhOffset))
                    {
                        throw new LisParseException("Invalid LIS logical record type.");
                    }

                    logicalRecordHeader = parsedHeader;
                    int consumedInProbe = lrhOffset + LisLogicalRecordHeader.HeaderLength;
                    int dataBytesInProbe = probeCount - consumedInProbe;
                    if (dataBytesInProbe > 0)
                    {
                        payloadBuilder.Append(_logicalHeaderProbeBuffer, consumedInProbe, dataBytesInProbe);
                    }

                    int remainingPayload = payloadLength - probeCount;
                    if (remainingPayload > 0)
                    {
                        payloadBuilder.AppendFromStream(stream, remainingPayload, "LIS physical record payload");
                    }
                }
                else if (payloadLength > 0)
                {
                    payloadBuilder.AppendFromStream(stream, payloadLength, "LIS physical record payload");
                }

                SkipBytes(stream, currentHeader.TrailerLength, "LIS physical record trailer");

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

        /// <summary>
        /// Подробно выполняет операцию «ReadNextImplicitDataRecord» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public LisLogicalRecord ReadNextImplicitDataRecord(Stream stream)
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
                    "Invalid LIS layout: implicit data record starts with continuation segment.");
            }

            var payloadBuilder = new PayloadBuilder(Math.Max(0, firstPrh.Length - LisPhysicalRecordHeader.HeaderLength));
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

                if (payloadLength > 0)
                {
                    payloadBuilder.AppendFromStream(stream, payloadLength, "LIS implicit data payload");
                }

                SkipBytes(stream, currentHeader.TrailerLength, "LIS physical record trailer");

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

            return new LisLogicalRecord(
                new LisLogicalRecordHeader((byte)LisRecordType.NormalData, 0),
                payloadBuilder.ToArray(),
                recordCount);
        }

        /// <summary>
        /// Подробно выполняет операцию «ReadNextPhysicalRecordHeader» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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

            ReadExactly(
                stream,
                _physicalHeaderBuffer,
                0,
                LisPhysicalRecordHeader.HeaderLength,
                "LIS physical record header");

            // Пропускаем очевидные 4-байтовые блоки паддинга (0x00... или 0x20...).
            while (LisHeaderParser.IsPadBytes(_physicalHeaderBuffer, 0, _physicalHeaderBuffer.Length))
            {
                ReadExactly(
                    stream,
                    _physicalHeaderBuffer,
                    0,
                    LisPhysicalRecordHeader.HeaderLength,
                    "LIS physical record header after padding");
            }

            return LisHeaderParser.ParsePhysicalRecordHeader(_physicalHeaderBuffer, 0);
        }

        /// <summary>
        /// Подробно выполняет операцию «SkipNextLogicalRecord» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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

                    int probeCount = Math.Min(payloadLength, _logicalHeaderProbeBuffer.Length);
                    ReadExactly(stream, _logicalHeaderProbeBuffer, 0, probeCount, "LIS logical record header probe");

                    int lrhOffset;
                    if (!TryParseLogicalRecordHeaderFromProbe(_logicalHeaderProbeBuffer, probeCount, payloadLength, out LisLogicalRecordHeader parsedHeader, out lrhOffset))
                    {
                        throw new LisParseException("Invalid LIS logical record type.");
                    }

                    lrh = parsedHeader;
                    int consumedInProbe = lrhOffset + LisLogicalRecordHeader.HeaderLength;
                    int dataInProbe = probeCount - consumedInProbe;
                    if (dataInProbe > 0)
                    {
                        dataLength += dataInProbe;
                    }

                    int remainingPayload = payloadLength - probeCount;
                    if (remainingPayload > 0)
                    {
                        SkipBytes(stream, remainingPayload, "LIS logical record data");
                        dataLength += remainingPayload;
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

        /// <summary>
        /// Подробно выполняет операцию «SkipNextImplicitDataRecord» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public LisRecordInfo SkipNextImplicitDataRecord(Stream stream)
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
                    "Invalid LIS layout: implicit data record starts with continuation segment.");
            }

            int physicalRecordCount = 0;
            int dataLength = 0;

            while (true)
            {
                physicalRecordCount++;

                int payloadLength = currentHeader.Length - LisPhysicalRecordHeader.HeaderLength - currentHeader.TrailerLength;
                if (payloadLength < 0)
                {
                    throw new LisParseException("Invalid LIS physical record length.");
                }

                if (payloadLength > 0)
                {
                    SkipBytes(stream, payloadLength, "LIS implicit data payload");
                    dataLength += payloadLength;
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

            return new LisRecordInfo(
                startOffset,
                LisRecordType.NormalData,
                headerAttributes: 0,
                physicalRecordCount,
                dataLength);
        }

        /// <summary>
        /// Подробно выполняет операцию «ReadExactly» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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
                int n = stream.Read(buffer, offset + totalRead, count - totalRead);
                if (n == 0)
                {
                    throw new LisParseException("Unexpected end of stream while reading " + componentName + ".");
                }

                totalRead += n;
            }
        }

        /// <summary>
        /// Подробно выполняет операцию «SkipBytes» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private static void SkipBytes(Stream stream, int count, string componentName)
        {
            if (count <= 0)
            {
                return;
            }

            if (stream.CanSeek)
            {
                long remaining = stream.Length - stream.Position;
                if (remaining < count)
                {
                    throw new LisParseException("Unexpected end of stream while reading " + componentName + ".");
                }

                stream.Position += count;
                return;
            }

            var scratch = new byte[Math.Min(count, 4096)];
            int remainingToSkip = count;
            while (remainingToSkip > 0)
            {
                int block = Math.Min(remainingToSkip, scratch.Length);
                ReadExactly(stream, scratch, 0, block, componentName);
                remainingToSkip -= block;
            }
        }

        /// <summary>
        /// Подробно выполняет операцию «RemainingBytesArePadding» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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

        /// <summary>
        /// Подробно выполняет операцию «LooksLikeImplicitRecord» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private bool LooksLikeImplicitRecord(Stream stream)
        {
            long startPosition = stream.Position;
            try
            {
                LisPhysicalRecordHeader firstPrh = ReadNextPhysicalRecordHeader(stream);
                if (firstPrh.HasPredecessor)
                {
                    return false;
                }

                int payloadLength = firstPrh.Length - LisPhysicalRecordHeader.HeaderLength - firstPrh.TrailerLength;
                if (payloadLength < LisLogicalRecordHeader.HeaderLength)
                {
                    return false;
                }

                ReadExactly(
                    stream,
                    _logicalHeaderProbeBuffer,
                    0,
                    LisLogicalRecordHeader.HeaderLength + 4,
                    "LIS logical record header probe");

                return !TryParseLogicalRecordHeaderFromProbe(
                    _logicalHeaderProbeBuffer,
                    LisLogicalRecordHeader.HeaderLength + 4,
                    payloadLength,
                    out _,
                    out _);
            }
            catch (LisParseException)
            {
                return false;
            }
            finally
            {
                stream.Position = startPosition;
            }
        }

        /// <summary>
        /// Подробно выполняет операцию «TryParseLogicalRecordHeaderFromProbe» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private static bool TryParseLogicalRecordHeaderFromProbe(
            byte[] probeBuffer,
            int probeCount,
            int payloadLength,
            out LisLogicalRecordHeader header,
            out int headerOffset)
        {
            if (TryParseLogicalRecordHeaderAt(probeBuffer, probeCount, 0, out header))
            {
                headerOffset = 0;
                return true;
            }

            // Некоторые файлы содержат 4-байтовый vendor-префикс перед LRH.
            if (payloadLength >= LisLogicalRecordHeader.HeaderLength + 4 &&
                TryParseLogicalRecordHeaderAt(probeBuffer, probeCount, 4, out header))
            {
                headerOffset = 4;
                return true;
            }

            headerOffset = 0;
            return false;
        }

        /// <summary>
        /// Подробно выполняет операцию «TryParseLogicalRecordHeaderAt» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private static bool TryParseLogicalRecordHeaderAt(
            byte[] buffer,
            int bufferCount,
            int offset,
            out LisLogicalRecordHeader header)
        {
            header = default!;
            if (offset < 0 || offset + LisLogicalRecordHeader.HeaderLength > bufferCount)
            {
                return false;
            }

            try
            {
                header = LisHeaderParser.ParseLogicalRecordHeader(buffer, offset);
                return true;
            }
            catch (LisParseException)
            {
                return false;
            }
        }

        private sealed class PayloadBuilder
        {
            private byte[] _buffer;
            private int _length;

            /// <summary>
            /// Подробно выполняет операцию «PayloadBuilder» для обработки данных формата LIS.
            /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
            /// </summary>
            public PayloadBuilder(int initialCapacity)
            {
                _buffer = new byte[Math.Max(initialCapacity, 16)];
                _length = 0;
            }

            /// <summary>
            /// Подробно выполняет операцию «Append» для обработки данных формата LIS.
            /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
            /// </summary>
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

            /// <summary>
            /// Подробно выполняет операцию «AppendFromStream» для обработки данных формата LIS.
            /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
            /// </summary>
            public void AppendFromStream(Stream stream, int count, string componentName)
            {
                if (count <= 0)
                {
                    return;
                }

                EnsureCapacity(_length + count);
                int totalRead = 0;
                while (totalRead < count)
                {
                    int n = stream.Read(_buffer, _length + totalRead, count - totalRead);
                    if (n == 0)
                    {
                        throw new LisParseException("Unexpected end of stream while reading " + componentName + ".");
                    }

                    totalRead += n;
                }

                _length += count;
            }

            /// <summary>
            /// Подробно выполняет операцию «ToArray» для обработки данных формата LIS.
            /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
            /// </summary>
            public byte[] ToArray()
            {
                var result = new byte[_length];
                if (_length > 0)
                {
                    Buffer.BlockCopy(_buffer, 0, result, 0, _length);
                }

                return result;
            }

            /// <summary>
            /// Подробно выполняет операцию «EnsureCapacity» для обработки данных формата LIS.
            /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
            /// </summary>
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
