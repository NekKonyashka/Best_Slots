using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Best_Slots
{
    internal class ScrollBarEventArgs : EventArgs
    {
        //Возможно в будущем заюзаю
        public ScrollBarEventArgs(ListBox listBox, int currentIndex, int finalIndex)
        {
            ListBox = listBox;
            CurrentIndex = currentIndex;
            FinalIndex = finalIndex;
        }

        public ListBox ListBox { get; set; }
        public int CurrentIndex { get; set; }
        public int FinalIndex { get; set; }
    }
}
