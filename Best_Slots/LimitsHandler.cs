using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Best_Slots
{
    internal static class LimitsHandler
    {
        //Приватные поля для подкрутки
        private static int _limit;
        private static int _wasteLimit;

        //Свойства для чтения полей для подкрутки
        public static int Limit => _limit;
        public static int WasteLimit => _wasteLimit;

        //Конструктор, который вызывается до первого обращения к любому члену класса
        static LimitsHandler()
        {
            SetLimit();
            SetWasteLimit();
        }

        //Методы для подкрутки
        public static void SetLimit()
        {
            _limit = ChanceSettings.rnd.Next(85, 190);
        }
        //Подкрутка
        public static void SetWasteLimit()
        {
            _wasteLimit = ChanceSettings.rnd.Next(18, 42);
        }
    }
}
