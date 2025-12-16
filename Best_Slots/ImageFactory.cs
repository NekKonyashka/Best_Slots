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
        //Класс для создания картинок, хранит словарь,
        //в который через метод передается текущий словарь из ImageLibrary в зависимости от выбранной тематики

        private Dictionary<int, BitmapImage> _currentImages = new();

        public void SetDictionary(Dictionary<int,string> dict)
        {
            //Предварительная очистка прошлых элементов и добавление новых по соответствующим ключам из переданного словаря dict
            _currentImages.Clear();
            foreach (var key in dict.Keys)
            {
                _currentImages.Add(key, new BitmapImage(new Uri("images/" + dict[key], UriKind.Relative))
                {
                    DecodePixelHeight = 145,
                    DecodePixelWidth = 145
                });
            }
        }

        //Метод создания картинки, возвращающий готовое изображение
        public Image CreateImage()
        {
            int index = ChanceSettings.SpawnChance(); //Определяет, какое именно изображение будет создано
            return new Image()
            {
                Tag = index,
                Source = _currentImages[index],
                Width = 240,
                Height = 240,
                RenderTransformOrigin = new System.Windows.Point(0.5, 0.5) //Установка точки трансформа в центр, т.к. изначально стоит в (0,0)
            };
        }

    }
}
