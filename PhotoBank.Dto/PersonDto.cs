using System;
using System.Collections.Generic;

namespace PhotoBank.Dto
{
    public class PersonDto
    {
        public int PersonId { get; set; }
        public Guid ExternalGuid { get; set; }
        public List<PersonFaceDto> Faces { get; set; }
    }
}