using System;
using System.Collections.Generic;

namespace Lis.Core.Lis
{
    public sealed class LisLogicalFile
    {
        private readonly List<LisRecordInfo> _records;

        /// <summary>
        /// Подробно выполняет операцию «LisLogicalFile» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public LisLogicalFile(
            IReadOnlyList<LisRecordInfo> records,
            LisRecordInfo? fileHeader,
            LisRecordInfo? fileTrailer,
            bool isComplete)
        {
            if (records == null)
            {
                throw new ArgumentNullException(nameof(records));
            }

            _records = new List<LisRecordInfo>(records.Count);
            for (int i = 0; i < records.Count; i++)
            {
                _records.Add(records[i]);
            }

            FileHeader = fileHeader;
            FileTrailer = fileTrailer;
            IsComplete = isComplete;
        }

        public IReadOnlyList<LisRecordInfo> Records
        {
            get { return _records; }
        }

        public LisRecordInfo? FileHeader { get; }

        public LisRecordInfo? FileTrailer { get; }

        public bool IsComplete { get; }

        /// <summary>
        /// Подробно выполняет операцию «OfType» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public IReadOnlyList<LisRecordInfo> OfType(LisRecordType type)
        {
            var result = new List<LisRecordInfo>();
            for (int i = 0; i < _records.Count; i++)
            {
                if (_records[i].Type == type)
                {
                    result.Add(_records[i]);
                }
            }

            return result;
        }
    }
}
