namespace Lis.Core.Lis
{
    public enum LisRecordType : byte
    {
        NormalData = 0,
        AlternateData = 1,
        JobIdentification = 32,
        WellsiteData = 34,
        ToolStringInfo = 39,
        EncryptedTableDump = 42,
        TableDump = 47,
        DataFormatSpecification = 64,
        DataDescriptor = 65,
        Picture = 85,
        Image = 86,
        Tu10SoftwareBoot = 95,
        BootstrapLoader = 96,
        CpKernelLoader = 97,
        ProgramFileHeader = 100,
        ProgramOverlayHeader = 101,
        ProgramOverlayLoad = 102,
        FileHeader = 128,
        FileTrailer = 129,
        TapeHeader = 130,
        TapeTrailer = 131,
        ReelHeader = 132,
        ReelTrailer = 133,
        LogicalEof = 137,
        LogicalBot = 138,
        LogicalEot = 139,
        LogicalEom = 141,
        OperatorCommandInputs = 224,
        OperatorResponseInputs = 225,
        SystemOutputs = 227,
        FlicComment = 232,
        BlankRecord = 234
    }
}
