using System.Runtime.Serialization;

namespace PhotoBank.DbContext.Models
{
    public enum IdentityStatus
    {
        Undefined = 0,
        NotDetected = 1,
        NotIdentified = 2,
        Identified = 3,
        ForReprocessing = 4,
        StopProcessing = 5
    }
}