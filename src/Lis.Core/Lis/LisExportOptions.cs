namespace Lis.Core.Lis
{
    /// <summary>
    /// Controls low-level write behavior for <see cref="LisExporter"/>.
    /// </summary>
    public sealed class LisExportOptions
    {
        /// <summary>
        /// Creates export options with an optional physical record size cap.
        /// </summary>
        /// <param name="maxPhysicalRecordLength">
        /// Maximum physical record length including PRH and payload.
        /// </param>
        public LisExportOptions(ushort maxPhysicalRecordLength = ushort.MaxValue)
        {
            MaxPhysicalRecordLength = maxPhysicalRecordLength;
        }

        /// <summary>
        /// Maximum physical record length including the 4-byte PRH.
        /// </summary>
        public ushort MaxPhysicalRecordLength { get; }
    }
}
