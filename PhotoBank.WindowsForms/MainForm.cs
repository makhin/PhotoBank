using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PhotoBank.Dto;
using PhotoBank.Services.Api;

namespace PhotoBank.WindowsForms
{
    public partial class MainForm : Form
    {
        private readonly IPhotoService _photoService;

        public MainForm(IPhotoService photoService)
        {
            _photoService = photoService;
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            await using (MemoryStream stream = new MemoryStream())
            {
                var photo = await _photoService.GetAsync((int)numericUpDown1.Value);
                if (photo == null)
                {
                    return;
                }
                stream.Write(photo.PreviewImage, 0, Convert.ToInt32(photo.PreviewImage.Length));
                var image = new Bitmap(stream, false);
                if (photo.Orientation == 8)
                {
                    image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                }
                pictureBoxPreview.Image = image;
            }
        }
    }
}
