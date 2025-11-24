using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Best_Slots
{
    internal static class DepositFlow
    {
        //Класс, для хранения информации о ставках. Также хранит методы для перемещения индекса и получения текущего значения

        private static string[] _deposits = { "0,20", "0,50", "1,00", "2,00" };
        public static int Count => _deposits.Length;

        private static int _index = _deposits.Length - 1; //Изначально указывает на последний элемент
        public static int Index => _index;

        //Поля проверки возможности перемещения индекса по массиву
        public static bool CanMovePrev => _index != 0; 
        public static bool CanMoveNext => _index != _deposits.Length - 1;

        public static string Next()
        {
            _index++;
            return Current();
        }
        public static string Previous()
        {
            _index--;
            return Current();
        }
        public static string Current()
        {
            return _deposits[_index];
        }
    }
}
