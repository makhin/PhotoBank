using PhotoBank.Dto.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoBank.MAUI.Blazor.Services
{
    internal interface IRestService
    {
        Task<QueryResult> GetPhotos();
    }
}
