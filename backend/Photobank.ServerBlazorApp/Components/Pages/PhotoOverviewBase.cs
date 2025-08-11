using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using PhotoBank.Services.Api;
using PhotoBank.ViewModel.Dto;
using Radzen;
using Radzen.Blazor;

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
        public bool IsLoading { get; set; }

        protected RadzenDataGrid<PhotoItemDto> grid;

        public PhotoOverviewBase()
        {
            Filter = new FilterDto();
            IsLoading = false;
        }

        protected async Task LoadData(LoadDataArgs args)
        {
            IsLoading = true;
            await Task.Yield();
            if (Filter.IsNotEmpty())
            {
                Filter.OrderBy = args.OrderBy;
                if (args.Top.HasValue)
                {
                    Filter.PageSize = args.Top.Value;
                    Filter.Page = args.Skip / args.Top.Value + 1;
                }
                var queryResult = await PhotoService.GetAllPhotosAsync(Filter);
                Count = queryResult.TotalCount;
                Photos = queryResult.Items;
            }
            else
            {
                Photos = Enumerable.Empty<PhotoItemDto>();
                Count = 0;
            }
            IsLoading = false;
        }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            Photos = Enumerable.Empty<PhotoItemDto>();
            Count = 0;

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
            grid.Reset(true);
            await grid.FirstPage(true);
        }

        protected async void Cancel()
        {
            Filter = new FilterDto();
            grid.Reset(true);
            await grid.FirstPage(true);
        }
    }
}
