using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Best_Slots
{
    internal class ImageFactory
    {
        private readonly Dictionary<int, string> Images = new()
        {
            [0] = "fit.png",
            [1] = "lid.png",
            [2] = "lhf.png",
            [3] = "htit.png",
            [4] = "tof.png",
            [5] = "ief.png",
            [6] = "pim.png",
        };

        private static double Width = 145;

        private ImageSource Source;

        private int Tag;

        public ImageFactory()
        {
            int index = ChanceSettings.SpawnChance();
            Source = new BitmapImage(new Uri("images/" + Images[index], UriKind.Relative));
            Tag = index;
        }

        public Image CreateImage()
        {
            return new Image() { Tag = Tag, Source = Source, Width = Width, SnapsToDevicePixels = true};
        }

    }
}
