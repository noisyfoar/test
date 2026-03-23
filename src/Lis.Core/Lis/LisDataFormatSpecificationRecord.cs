using System.Collections.Generic;

namespace Lis.Core.Lis
{
    public sealed class LisDataFormatSpecificationRecord
    {
        /// <summary>
        /// Подробно выполняет операцию «LisDataFormatSpecificationRecord» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public LisDataFormatSpecificationRecord(
            IReadOnlyList<LisDfsrEntryBlock> entries,
            IReadOnlyList<LisDfsrSpecBlock> specBlocks,
            byte subtype)
        {
            Entries = entries;
            SpecBlocks = specBlocks;
            Subtype = subtype;
        }

        public IReadOnlyList<LisDfsrEntryBlock> Entries { get; }

        public IReadOnlyList<LisDfsrSpecBlock> SpecBlocks { get; }

        public byte Subtype { get; }
    }
}
