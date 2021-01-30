using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
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
        public List<StorageDto> Storages { get; set; }
        public List<PersonDto> Persons { get; set; }
        public List<TagDto> Tags { get; set; }
        public FilterDto Filter { get; set; }

        public PhotoOverviewBase()
        {
            Filter = new FilterDto();
        }

        protected override void OnInitialized()
        {
            Photos = PhotoService.GetAllPhotos(Filter);
        }

        protected override async Task OnInitializedAsync()
        {
            Storages = await PhotoService.GetAllStoragesAsync();
            Persons = await PhotoService.GetAllPersonsAsync();
            Tags = await PhotoService.GetAllTagsAsync();
        }

        protected void ApplyFilter(FilterDto filterDto)
        {
            Photos = PhotoService.GetAllPhotos(Filter);
        }

        protected void Cancel()
        {
            Filter = new FilterDto();
            PhotoService.GetAllPhotos(Filter);
        }
    }
}
