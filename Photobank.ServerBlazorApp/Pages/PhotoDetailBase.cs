﻿using System;
using System.Collections.Generic;
using System.Linq;
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
        public IEnumerable<PersonDto> Persons { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Photo = await PhotoDataService.GetPhotoAsync(int.Parse(PhotoId));
            Persons = await PhotoDataService.GetAllPersonsAsync();
        }

        protected string GetPersonNameById(int? id)
        {
            return !id.HasValue ? string.Empty : Persons.FirstOrDefault(p => p.Id == id)?.Name;
        }

        protected Action<ChangeEventArgs> OnChangePerson(object item)
        {
            var face = item as FaceDto;

            return async e =>
            {
                if (e.Value == null)
                {
                    return;
                }

                var personId = int.Parse(e.Value.ToString() ?? "0");
                face.PersonId = personId;

                await PhotoDataService.UpdateFaceAsync(face);
            };
        }
    }
}
