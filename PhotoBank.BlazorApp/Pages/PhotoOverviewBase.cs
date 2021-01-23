using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PhotoBank.BlazorApp.Data;
using PhotoBank.Dto;
using PhotoBank.Dto.View;
using PhotoBank.Services;

namespace PhotoBank.BlazorApp.Pages
{
    public class PhotoOverviewBase: ComponentBase
    {
        [Inject]
        public IPhotoDataService PhotoDataService { get; set; }

        public List<PhotoDto> Photos { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Photos = (await PhotoDataService.GetAllPhotos()).ToList();
        }
    }
}
