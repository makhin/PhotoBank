using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using PhotoBank.Dto.View;
using PhotoBank.Services.Api;
using Radzen;

namespace PhotoBank.ServerBlazorApp.Components.Pages
{
    public class PhotoOverviewBase: ComponentBase
    {
        [Inject]
        public IPhotoService PhotoService { get; set; }
        [Inject]
        public IAuthorizationService AuthorizationService { get; set; }
        [Inject]
        public AuthenticationStateProvider AuthenticationStateProvider { get; set; }
        public IEnumerable<PhotoItemDto>? Photos { get; set; }
        public IEnumerable<StorageDto>? Storages { get; set; }
        public IEnumerable<PersonDto>? Persons { get; set; }
        public IEnumerable<TagDto>? Tags { get; set; }
        public IEnumerable<PathDto>? Paths { get; set; }
        public FilterDto Filter { get; set; }
        public int Count { get; set; }
        public bool AllowAdultFilter { get; set; }
        public bool AllowRacyFilter { get; set; }

        public PhotoOverviewBase()
        {
            Filter = new FilterDto();
        }

        protected async Task LoadData(LoadDataArgs args)
        {
            var queryResult = await PhotoService.GetAllPhotosAsync(Filter, args.OrderBy, args.Skip, args.Top);
            Count = queryResult.Count;
            Photos = queryResult.Photos;
        }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            Storages = await PhotoService.GetAllStoragesAsync();
            Persons = await PhotoService.GetAllPersonsAsync();
            Tags = await PhotoService.GetAllTagsAsync();
            Paths = await PhotoService.GetAllPathsAsync();

            var authState = await AuthenticationStateProvider
                .GetAuthenticationStateAsync();
            var user = authState.User;

            AllowAdultFilter = (await AuthorizationService.AuthorizeAsync(user, "AllowToSeeAdultContent")).Succeeded;
            AllowRacyFilter = (await AuthorizationService.AuthorizeAsync(user, "AllowToSeeRacyContent")).Succeeded;
        }

        protected async Task ApplyFilter(FilterDto filterDto)
        {
            var queryResult = await PhotoService.GetAllPhotosAsync(Filter, null, 0, 20);
            Count = queryResult.Count;
            Photos = queryResult.Photos;
        }

        protected void Cancel()
        {
            Filter = new FilterDto();
        }
    }
}
