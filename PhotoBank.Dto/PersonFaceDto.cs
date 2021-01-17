using System;

namespace PhotoBank.Dto
{
    public class PersonFaceDto
    {
        public int FaceId { get; set; }
        public int PersonId { get; set; }
        public Guid? ExternalGuid { get; set; }
    }
}