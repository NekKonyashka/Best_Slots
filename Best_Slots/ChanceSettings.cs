using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Best_Slots
{
    internal static class ChanceSettings
    {
        public static readonly Random rnd = new Random();
        public static int SpawnChance()
        {
            int zone = rnd.Next(100);
            if (zone < 35)
            {
                return rnd.Next(5, 7);
            }
            if (zone >= 35 && zone < 70)
            {
                return rnd.Next(3, 5);
            }
            if (zone >= 70 && zone < 90)
            {
                return rnd.Next(1, 3);
            }
            return 0;
        }
        public static int StopChance()
        {
            int zone = rnd.Next(100);
            if (zone < 35)
            {
                return 6;
            }
            if (zone >= 35 && zone < 65)
            {
                return 5;
            }
            if (zone >= 65 && zone < 85)
            {
                return rnd.Next(3, 5);
            }
            if (zone >= 85 && zone < 95)
            {
                return rnd.Next(1, 3);
            }
            return 0;
        }
    }
}
