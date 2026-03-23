using System;
using System.IO;

namespace Lis.Core.Lis
{
    /// <summary>
    /// Выполняет запись логических записей обратно в транспортный LIS-формат.
    /// </summary>
    public sealed class LisExporter
    {
        /// <summary>
        /// Экспортирует документ LIS в файл по указанному пути.
        /// </summary>
        public void Export(string path, LisDocument document, LisExportOptions? options = null)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (path.Trim().Length == 0)
            {
                throw new ArgumentException("Path must not be empty.", nameof(path));
            }

            using var stream = File.Create(path);
            Export(stream, document, options);
        }

        /// <summary>
        /// Экспортирует документ LIS в поток с поддержкой записи.
        /// </summary>
        public void Export(Stream stream, LisDocument document, LisExportOptions? options = null)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (!stream.CanWrite)
            {
                throw new ArgumentException("Stream must be writable.", nameof(stream));
            }

            options = options ?? new LisExportOptions();

            int maxPhysicalRecordLength = options.MaxPhysicalRecordLength;
            int maxPayloadLength = maxPhysicalRecordLength - LisPhysicalRecordHeader.HeaderLength;
            if (maxPayloadLength <= LisLogicalRecordHeader.HeaderLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(options),
                    "MaxPhysicalRecordLength must allow at least one byte after PRH + LRH.");
            }

            for (int i = 0; i < document.Records.Count; i++)
            {
                WriteLogicalRecord(stream, document.Records[i], maxPayloadLength);
            }
        }

        /// <summary>
        /// Подробно выполняет операцию «WriteLogicalRecord» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private static void WriteLogicalRecord(Stream stream, LisLogicalRecord record, int maxPayloadLength)
        {
            int dataOffset = 0;
            int dataRemaining = record.Data.Length;
            bool firstSegment = true;

            while (firstSegment || dataRemaining > 0)
            {
                // Только первый сегмент содержит LRH, поэтому его полезная ёмкость меньше.
                int segmentOverhead = firstSegment ? LisLogicalRecordHeader.HeaderLength : 0;
                int maxDataInSegment = maxPayloadLength - segmentOverhead;
                int segmentDataLength = Math.Min(dataRemaining, maxDataInSegment);
                bool hasSuccessor = dataRemaining > segmentDataLength;

                ushort attributes = 0;
                if (!firstSegment)
                {
                    attributes |= LisPhysicalRecordHeader.AttributePredecessor;
                }

                if (hasSuccessor)
                {
                    attributes |= LisPhysicalRecordHeader.AttributeSuccessor;
                }

                ushort length = (ushort)(LisPhysicalRecordHeader.HeaderLength + segmentOverhead + segmentDataLength);
                WriteUInt16BigEndian(stream, length);
                WriteUInt16BigEndian(stream, attributes);

                if (firstSegment)
                {
                    stream.WriteByte(record.Header.Type);
                    stream.WriteByte(record.Header.Attributes);
                }

                if (segmentDataLength > 0)
                {
                    stream.Write(record.Data, dataOffset, segmentDataLength);
                }

                dataOffset += segmentDataLength;
                dataRemaining -= segmentDataLength;
                firstSegment = false;

                if (!hasSuccessor)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Подробно выполняет операцию «WriteUInt16BigEndian» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private static void WriteUInt16BigEndian(Stream stream, ushort value)
        {
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)(value & 0xFF));
        }
    }
}
