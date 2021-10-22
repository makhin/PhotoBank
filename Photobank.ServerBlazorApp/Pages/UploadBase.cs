using Microsoft.AspNetCore.Components;
using Radzen;

namespace PhotoBank.ServerBlazorApp.Pages
{
    public class UploadBase: ComponentBase
    {
        [Inject]
        public NavigationManager NavigationManager { get; set; }

        public int Progress { get; set; }

        protected void OnProgress(UploadProgressArgs args)
        {
            this.Progress = args.Progress;
        }

        protected void OnComplete(UploadCompleteEventArgs args)
        {
            NavigationManager.NavigateTo($"photodetail/{args.RawResponse}");
        }
    }
}
