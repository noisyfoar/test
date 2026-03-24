using System;
using System.Collections.Generic;

namespace Lis.Core.Lis
{
    public sealed class LisRecordIndex
    {
        private readonly List<LisRecordInfo> _records;
        private readonly List<LisRecordInfo> _implicitRecords;
        private readonly List<LisRecordInfo> _explicitRecords;

        /// <summary>
        /// Подробно выполняет операцию «LisRecordIndex» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public LisRecordIndex(IReadOnlyList<LisRecordInfo> records)
        {
            if (records == null)
            {
                throw new ArgumentNullException(nameof(records));
            }

            _records = new List<LisRecordInfo>(records.Count);
            _implicitRecords = new List<LisRecordInfo>();
            _explicitRecords = new List<LisRecordInfo>();

            for (int i = 0; i < records.Count; i++)
            {
                LisRecordInfo info = records[i];
                _records.Add(info);
                if (info.IsImplicitRecord)
                {
                    _implicitRecords.Add(info);
                }
                else
                {
                    _explicitRecords.Add(info);
                }
            }
        }

        public IReadOnlyList<LisRecordInfo> Records
        {
            get { return _records; }
        }

        public IReadOnlyList<LisRecordInfo> ImplicitRecords
        {
            get { return _implicitRecords; }
        }

        public IReadOnlyList<LisRecordInfo> ExplicitRecords
        {
            get { return _explicitRecords; }
        }

        public int Count
        {
            get { return _records.Count; }
        }

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
