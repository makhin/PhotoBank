using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PhotoBank.Dto.View;
using PhotoBank.Services.Api;
using Radzen;

namespace PhotoBank.ServerBlazorApp.Pages
{
    public class PhotoOverviewBase: ComponentBase
    {
        [Inject]
        public IPhotoService PhotoService { get; set; }
        public IEnumerable<PhotoItemDto> Photos { get; set; }
        public IEnumerable<StorageDto> Storages { get; set; }
        public IEnumerable<PersonDto> Persons { get; set; }
        public IEnumerable<TagDto> Tags { get; set; }
        public IEnumerable<PathDto> Paths { get; set; }
        public FilterDto Filter { get; set; }
        public int Count { get; set; }

        public PhotoOverviewBase()
        {
            Filter = new FilterDto();
        }

        protected async Task LoadData(LoadDataArgs args)
        {
            var queryResult = await PhotoService.GetAllPhotosAsync(Filter, args.Skip, args.Top);
            Count = queryResult.Count;
            Photos = queryResult.Photos;
        }

        protected override async Task OnInitializedAsync()
        {
            Storages = await PhotoService.GetAllStoragesAsync();
            Persons = await PhotoService.GetAllPersonsAsync();
            Tags = await PhotoService.GetAllTagsAsync();
            Paths = await PhotoService.GetAllPathsAsync();
        }

        protected async Task ApplyFilter(FilterDto filterDto)
        {
            var queryResult = await PhotoService.GetAllPhotosAsync(Filter, 0, 20);
            Count = queryResult.Count;
            Photos = queryResult.Photos;
        }

        protected void Cancel()
        {
            Filter = new FilterDto();
        }
    }
}
