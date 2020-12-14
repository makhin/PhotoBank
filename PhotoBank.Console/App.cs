using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Repositories;
using PhotoBank.Services;
using PhotoBank.Services.Api;
using System;
using PhotoBank.Dto;

namespace PhotoBank.Console
{
    using System;

    public class App
    {
        private readonly IPhotoProcessor _photoProcessor;
        private readonly IRepository<Storage> _repository;
        private readonly IPhotoService _photoService;

        public App(IPhotoProcessor photoProcessor, IRepository<Storage> repository, IPhotoService photoService)
        {
            _photoProcessor = photoProcessor;
            _repository = repository;
            _photoService = photoService;
        }

        public void Run()
        {
            var storage = _repository.GetAsync(3, storages => storages).Result;

            var files = Directory.GetFiles(@"\\192.168.1.35\Public\Photo\RX100_2\", "*.*")
                .OrderBy(f => new FileInfo(f).Length).Skip(300).Take(5);
            foreach (var file in files)
            {
                Console.WriteLine(file);
                _photoProcessor.AddPhoto(storage, file);
                Task.Delay(3000).Wait();
            }

            Console.WriteLine("Done");
        }
    }
}
