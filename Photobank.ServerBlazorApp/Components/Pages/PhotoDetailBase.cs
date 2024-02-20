using Microsoft.AspNetCore.Components;
using PhotoBank.Dto.View;
using PhotoBank.Services.Api;
using Radzen;

namespace PhotoBank.ServerBlazorApp.Components.Pages
{
    public class PhotoDetailBase: ComponentBase
    {
        private readonly TooltipOptions _options = new() { Position = TooltipPosition.Bottom, Duration = 5000 };
        [Inject]
        public IPhotoService PhotoDataService { get; set; }
        [Inject] 
        public TooltipService TooltipService { get; set; }
        [Parameter]
        public string PhotoId { get; set; }
        protected PhotoDto Photo { get; set; }
        protected IEnumerable<PersonDto> Persons { get; set; }
        protected ElementReference[] MemberRef { get; set; }

        protected void ShowTooltipWithHtml(int i, string content) => TooltipService.Open(MemberRef[i], ds =>
        {
            return builder =>
            {
                builder.AddContent(0, (MarkupString)content);
            };
        }, _options);

        protected override async Task OnInitializedAsync()
        {
            Photo = await PhotoDataService.GetPhotoAsync(int.Parse(PhotoId));
            if (Photo != null)
            {
                Persons = await PhotoDataService.GetAllPersonsAsync();
                MemberRef = new ElementReference[Photo.Faces.Count];
            }
        }

        protected string? GetPersonNameById(int? id)
        {
            return !id.HasValue ? string.Empty : Persons.FirstOrDefault(p => p.Id == id)?.Name;
        }

        protected async Task OnChangePersonAsync(int faceId, object personId)
        {
            await PhotoDataService.UpdateFaceAsync(faceId, (int)personId);
        }
    }
}
