namespace Lis.Core.Lis
{
    public enum LisDfsrEntryType : byte
    {
        Terminator = 0,
        DataRecordType = 1,
        SpecBlockType = 2,
        FrameSize = 3,
        UpDownFlag = 4,
        DepthScaleUnits = 5,
        ReferencePoint = 6,
        ReferencePointUnits = 7,
        Spacing = 8,
        SpacingUnits = 9,
        Undefined = 10,
        MaxFramesPerRecord = 11,
        AbsentValue = 12,
        DepthRecordMode = 13,
        UnitsOfDepth = 14,
        RepresentationCodeOutputDepth = 15,
        SpecBlockSubtype = 16
    }
}
