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
                if (!reader.TrySkipNextLogicalRecord(stream, out LisRecordInfo? info))
                {
                    break;
                }
                if (info == null)
                {
                    throw new LisParseException("LIS reader returned no record info after successful skip.");
                }

                records.Add(info);
            }

            return new LisRecordIndex(records);
        }
    }
}
