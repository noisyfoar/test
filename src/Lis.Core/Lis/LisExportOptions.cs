namespace Lis.Core.Lis
{
    /// <summary>
    /// Управляет низкоуровневым поведением записи в <see cref="LisExporter"/>.
    /// </summary>
    public sealed class LisExportOptions
    {
        /// <summary>
        /// Создаёт настройки экспорта с ограничением размера physical record.
        /// </summary>
        /// <param name="maxPhysicalRecordLength">
        /// Максимальная длина physical record, включая PRH и полезную нагрузку.
        /// </param>
        public LisExportOptions(ushort maxPhysicalRecordLength = ushort.MaxValue)
        {
            MaxPhysicalRecordLength = maxPhysicalRecordLength;
        }

        /// <summary>
        /// Максимальная длина physical record, включая 4-байтовый PRH.
        /// </summary>
        public ushort MaxPhysicalRecordLength { get; }
    }
}
