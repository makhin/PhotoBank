using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PhotoBank.DbContext.Models;
using PhotoBank.Services;

namespace PhotoBank.Console
{
    using System;

    public class App
    {
        private readonly IPhotoProcessor _photoProcessor;

        public App(IPhotoProcessor photoProcessor)
        {
            _photoProcessor = photoProcessor;
        }

        public void Run()
        {
            var storage = new Storage()
            {
                Id = 1,
                Folder = @"\\192.168.1.35\Public\",
                Name = "Photos"
            };

            var files = Directory.GetFiles(@"\\192.168.1.35\Public\Photo\RX100_2\", "*.jpg").Skip(35).Take(200);
            foreach (string file in files)
            {
                Console.WriteLine(file);
                _photoProcessor.AddPhoto(storage, file);
                Task.Delay(3000).Wait();
            }
        }
    }
}
