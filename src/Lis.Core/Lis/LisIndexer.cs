using System;
using System.Collections.Generic;
using System.IO;

namespace Lis.Core.Lis
{
    public sealed class LisIndexer
    {
        /// <summary>
        /// Подробно выполняет операцию «Index» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public LisRecordIndex Index(Stream stream)
        {
            return Index(stream, allowMalformedData: false, metrics: null);
        }

        /// <summary>
        /// Подробно выполняет операцию «Index» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public LisRecordIndex Index(Stream stream, bool allowMalformedData, LisReadMetrics? metrics = null)
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

            var reader = new LisReader();
            var records = new List<LisRecordInfo>();

            while (true)
            {
                long positionBeforeRecord = stream.Position;
                LisRecordInfo? info;
                try
                {
                    if (!reader.TrySkipNextLogicalRecord(stream, out info))
                    {
                        break;
                    }
                }
                catch (LisParseException) when (allowMalformedData)
                {
                    if (!TryAdvanceOneByte(stream, positionBeforeRecord))
                    {
                        break;
                    }

                    metrics?.AddMalformedRecordsSkipped(1);
                    continue;
                }

                if (info == null)
                {
                    if (!allowMalformedData)
                    {
                        throw new LisParseException("LIS reader returned no record info after successful skip.");
                    }

                    if (!TryAdvanceOneByte(stream, positionBeforeRecord))
                    {
                        break;
                    }

                    metrics?.AddMalformedRecordsSkipped(1);
                    continue;
                }

                records.Add(info);
            }

            return new LisRecordIndex(records);
        }

        /// <summary>
        /// Подробно выполняет операцию «TryAdvanceOneByte» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private static bool TryAdvanceOneByte(Stream stream, long fromPosition)
        {
            if (!stream.CanSeek)
            {
                return false;
            }

            long next = fromPosition + 1;
            if (next >= stream.Length)
            {
                return false;
            }

            stream.Position = next;
            return true;
        }
    }
}
