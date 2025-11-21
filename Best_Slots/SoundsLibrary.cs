using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Best_Slots
{
    internal static class SoundsLibrary
    {
        //Аналогичный класс с словарем, хранящий пути к звукам, которые соответсвуют индексам игровых элементов.

        private static readonly Dictionary<int, string> _sounds = new()
        {
            [0] = "unhealthy-zoglin-danika-house_zNSVRELv.wav",
            [1] = "mi-bombo-duolingo.wav",
            [2] = "mi-bombo-duolingo.wav",
            [3] = "lucky-lucky.wav",
            [4] = "lucky-lucky.wav",
            [5] = "am-am-am-_1_.wav",
            [6] = "am-am-am-_1_.wav",
        };

        //Метод для получения строки из словаря по id
        public static string GetSound(int id)
        {
            return _sounds[id];
        }
    }
}
