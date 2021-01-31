using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using PhotoBank.Dto.View;
using PhotoBank.Services.Api;
using Radzen;

namespace PhotoBank.ServerBlazorApp.Pages
{
    public class PhotoOverviewBase: ComponentBase
    {
        private int? _skip;
        private int? _top;

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
            _skip = args.Skip;
            _top = args.Top;
            var queryResult = await PhotoService.GetAllPhotosAsync(Filter, _skip, _top);
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
            var queryResult = await PhotoService.GetAllPhotosAsync(Filter, _skip, _top);
            Count = queryResult.Count;
            Photos = queryResult.Photos;
        }

        protected void Cancel()
        {
            Filter = new FilterDto();
        }
    }
}
