using System.Runtime.Serialization;

namespace PhotoBank.Services.Models;

public enum IdentityStatusDto
{
    [EnumMember(Value = "Undefined")]
    Undefined = 0,
    [EnumMember(Value = "NotDetected")]
    NotDetected = 1,
    [EnumMember(Value = "NotIdentified")]
    NotIdentified = 2,
    [EnumMember(Value = "Identified")]
    Identified = 3,
    [EnumMember(Value = "ForReprocessing")]
    ForReprocessing = 4,
    [EnumMember(Value = "StopProcessing")]
    StopProcessing = 5
}