using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using PhotoBank.Services;

namespace PhotoBank.Console
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class App
    {
        private readonly IPhotoProcessor _photoProcessor;

        public App(IPhotoProcessor photoProcessor)
        {
            _photoProcessor = photoProcessor;
        }

        public void Run()
        {
            var files = Directory.GetFiles(@"\\192.168.1.35\Public\Photo\RX100_2\", "*.jpg").Skip(35).Take(200);
            foreach (string file in files)
            {
                Console.WriteLine(file);
                _photoProcessor.AddPhoto(file);
                Task.Delay(3000).Wait();
            }
        }
    }
}
