using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PhotoBank.Dto;
using PhotoBank.Dto.View;
using PhotoBank.Services.Api;

namespace PhotoBank.ServerBlazorApp.Pages
{
    public class PhotoDetailBase: ComponentBase
    {
        [Inject]
        public IPhotoService PhotoDataService { get; set; }

        [Parameter]
        public string PhotoId { get; set; }

        public PhotoDto Photo { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Photo = await PhotoDataService.GetAsync(int.Parse(PhotoId));
        }
    }
}
