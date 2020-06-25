using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PhotoBank.BlazorApp.Data;
using PhotoBank.DbContext.Models;

namespace PhotoBank.BlazorApp.Pages
{
    public class PhotoOverviewBase: ComponentBase
    {
        [Inject]
        public IPhotoDataService PhotoDataService { get; set; }

        public List<Photo> Photos { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Photos = (await PhotoDataService.GetAllPhotos()).ToList();
        }
    }
}
