using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PhotoBank.Dto;
using PhotoBank.Dto.View;
using PhotoBank.Services;
using PhotoBank.Services.Api;

namespace PhotoBank.ServerBlazorApp.Pages
{
    public class PhotoOverviewBase: ComponentBase
    {
        [Inject]
        public IPhotoService PhotoService { get; set; }

        public IEnumerable<PhotoItemDto> Photos { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Photos = await PhotoService.GetAllAsync();
        }
    }
}
