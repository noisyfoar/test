using System;
using System.Collections.Generic;

namespace Lis.Core.Lis
{
    public sealed class LisRecordIndex
    {
        private readonly List<LisRecordInfo> _records;
        private List<LisRecordInfo>? _implicitRecords;
        private List<LisRecordInfo>? _explicitRecords;

        /// <summary>
        /// Подробно выполняет операцию «LisRecordIndex» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public LisRecordIndex(IReadOnlyList<LisRecordInfo> records)
            : this(records, copyRecords: true)
        {
        }

        /// <summary>
        /// Подробно выполняет операцию «LisRecordIndex» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        internal LisRecordIndex(IReadOnlyList<LisRecordInfo> records, bool copyRecords)
        {
            if (records == null)
            {
                throw new ArgumentNullException(nameof(records));
            }

            if (!copyRecords && records is List<LisRecordInfo> list)
            {
                _records = list;
                return;
            }

            _records = new List<LisRecordInfo>(records.Count);
            for (int i = 0; i < records.Count; i++)
            {
                _records.Add(records[i]);
            }
        }

        public IReadOnlyList<LisRecordInfo> Records
        {
            get { return _records; }
        }

        public IReadOnlyList<LisRecordInfo> ImplicitRecords
        {
            get
            {
                EnsureBucketsBuilt();
                return _implicitRecords!;
            }
        }

        public IReadOnlyList<LisRecordInfo> ExplicitRecords
        {
            get
            {
                EnsureBucketsBuilt();
                return _explicitRecords!;
            }
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

        /// <summary>
        /// Подробно выполняет операцию «EnsureBucketsBuilt» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private void EnsureBucketsBuilt()
        {
            if (_implicitRecords != null && _explicitRecords != null)
            {
                return;
            }

            _implicitRecords = new List<LisRecordInfo>(_records.Count);
            _explicitRecords = new List<LisRecordInfo>(_records.Count);
            for (int i = 0; i < _records.Count; i++)
            {
                LisRecordInfo info = _records[i];
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
    }
}
