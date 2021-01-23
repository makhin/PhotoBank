using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PhotoBank.BlazorApp.Data;
using PhotoBank.Dto;
using PhotoBank.Dto.View;
using PhotoBank.Services;

namespace PhotoBank.BlazorApp.Pages
{
    public class PhotoDetailBase: ComponentBase
    {
        [Inject]
        public IPhotoDataService PhotoDataService { get; set; }

        [Parameter]
        public string PhotoId { get; set; }

        public PhotoDto Photo { get; set; }
        protected override async Task OnInitializedAsync()
        {
            Photo = await PhotoDataService.GetPhotoById(int.Parse(PhotoId));
        }
    }
}
