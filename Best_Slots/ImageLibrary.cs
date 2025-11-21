using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Best_Slots
{
    internal static class ImageLibrary
    {
        //Класс используется для хранения словарей со строками путей к файлам игровых элементов.
        //Доступ по индексам, которые присваиваются в теги элементов Image.

        private static readonly Dictionary<int, string> _facultImages = new()
        {
            [0] = "fac/fit.png",
            [1] = "fac/lid.png",
            [2] = "fac/lhf.png",
            [3] = "fac/htit.png",
            [4] = "fac/tof.png",
            [5] = "fac/ief.png",
            [6] = "fac/pim.png",
        };
        private static readonly Dictionary<int, string> _langImages = new()
        {
            [0] = "lang/cpp.png",
            [1] = "lang/cs.png",
            [2] = "lang/py.png",
            [3] = "lang/java.png",
            [4] = "lang/rust.png",
            [5] = "lang/go.png",
            [6] = "lang/php.png",
        };

        //Свойства для доступа к чтению словарей без возможности изменения.
        public static Dictionary<int, string> FacultImages { get => _facultImages; }
        public static Dictionary<int, string> LangImages { get => _langImages; }
    }
}
