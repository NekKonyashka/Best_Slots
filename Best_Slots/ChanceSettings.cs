using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Best_Slots
{
    internal static class ChanceSettings
    {
        //Класс с функциями рандома.

        public static readonly Random rnd = new Random();

        //Шанс появления соответсвующей картинки в столбце.
        public static int SpawnChance()
        {
            int zone = rnd.Next(100);
            int num = -1;
            if (zone < 35)
            {
                num = rnd.Next(5, 7);
            }
            else if (zone >= 35 && zone < 65)
            {
                num = rnd.Next(3, 5);
            }
            else if (zone >= 65 && zone < 90)
            {
                num = rnd.Next(1, 3);
            }
            else
            {
                num = 0;
            }
            return num;
        }
        //Шанс выпадения соответствующей картинки.
        public static int StopChance()
        {
            int zone = rnd.Next(100);
            int num = -1;
            if (zone < 33)
            {
                num = 6;
            }
            else if (zone >= 33 && zone < 58)
            {
                num = 5;
            }
            else if (zone >= 58 && zone < 83)
            {
                num = rnd.Next(3, 5);
            }
            else if (zone >= 83 && zone < 95)
            {
                num = rnd.Next(1, 3);
            }
            else
            {
                num = 0;
            }
            return num;
        }
    }
}
