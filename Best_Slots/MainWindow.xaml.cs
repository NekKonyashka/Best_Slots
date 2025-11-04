using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;

namespace Best_Slots
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
   
    public partial class MainWindow : Window
    {
        private bool isBegin = true;

        public static int ColumnsDelay = 200; 

        public const int MAX = 65;

        private static double _limit;
        private static double _wasteLimit;
        private static double _currentWining = 0;
        private static double _currentWaste = 0;

        private static bool _setHighChance = false;
        private static int _counter;

        private static double wining = 0;

        private static readonly List<Image> _images = new List<Image>();

        private static readonly List<List<Image>> _prevImages = new List<List<Image>>() { new List<Image>(), new List<Image>(), new List<Image>() };

        private static readonly Dictionary<int, double> Winings = new() 
        {
            [0] = 100.00,
            [1] = 50.00,
            [2] = 25.00,
            [3] = 17.50,
            [4] = 12.50,
            [5] = 6.00,
            [6] = 4.50
        };


        private static readonly string[] Deposits = { "0,20", "0,50", "1,00", "2,00" };
        private int index = Deposits.Length - 1;

        private static List<Task> _currentTasks = new List<Task>();
        public MainWindow()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            ResizeMode = ResizeMode.NoResize;

            InitializeComponent();
            SetLimit();
            SetWasteLimit();
            ShowDep();
            CheckButtons();
            FillColumns();
            ContentRendered += (s, e) =>
            {
                Scrolling();
            };
            isBegin = false;
        }

        private void Scrolling()
        {
            foreach (var col in FindColumns())
            {
                col.ScrollIntoView(col.Items[col.Items.Count - 1]);
                col.UpdateLayout();
                UpdateLayout();
                var scroll = GetScrollViewer(col);
                var off = scroll.VerticalOffset;
                scroll.ScrollToVerticalOffset(off - 51.5);
            }
        }

        private void SetLimit()
        {
            _limit = ChanceSettings.rnd.Next(100, 200);
        }
        private void SetWasteLimit()
        {
            _wasteLimit = ChanceSettings.rnd.Next(20, 40);
        }
        private IEnumerable<ListBox> FindColumns()
        {
            for(int i = 1;i <= 3;i++)
            {
                var name = "Column" + i;
                yield return MyGrid.FindName(name) as ListBox;
            }
        }
        public void FillColumns()
        {
            foreach (var col in FindColumns())
            {
                var maximum = ComputeMax(col);
                for (var i = 0; i < maximum; i++)
                { 
                    var factory = new ImageFactory();
                    Image img = factory.CreateImage();
                    col.Items.Add(img);
                    if(!isBegin && i >= maximum - 3)
                    {
                        col.Items.RemoveAt(maximum - 3);
                    }
                }
                foreach(var img in _prevImages[int.Parse(col.Name.Substring(6)) - 1])
                {
                    col.Items.Add(img);
                }
                if (isBegin)
                {
                    SetPreviousImages(maximum - 2, col);
                }
            }
            if (!isBegin)
            {
                Scrolling();
                foreach (var list in _prevImages)
                {
                    list.Clear();
                }
            }

        }
        private int ComputeMax(ListBox list)
        {
            return (int)(MAX * Math.Sqrt(int.Parse(list.Name.Substring(6))));
        }
        private double ConvertToDouble(string str)
        {
            return double.Parse(str.Replace(',','.'));
        }
        private double ParseBank()
        {
            string pattern = @"\d+\,\d+";
            var match = Regex.Match(Bank.Text,pattern);
            return ConvertToDouble(match.Value);
        }
        private bool SubDepositFromBank()
        {
            var bank = ParseBank();
            var dep = ConvertToDouble(Deposits[index]);
            if(dep <= bank)
            {
                Bank.Text = ((bank - dep).ToString("0.00") + " BYN").Replace('.',',');
                _currentWaste += dep;
                AddChance(ref _currentWaste,ComputeWasteLimit(),SetWasteLimit);
                return true;
            }
            else
            {
                return false;
            }
        }
        private void SetPreviousImages(int index,ListBox list)
        {
            int id = int.Parse(list.Name.Substring(6)) - 1;
            for(int i = index - 1, j = 0; j < 3; i++, j++)
            {
                _prevImages[id].Add((Image)list.Items[i]);
            }
        }
        private void SetStoppedElements()
        {
            int sameChance = 0;
            if (_setHighChance)
            {
                _counter--;
                if(_counter == 0)
                {
                    sameChance = ChanceSettings.StopChance();
                }
            }
            foreach (ListBox col in FindColumns())
            {
                int id = -1;
                while(!IsExists(col, id))
                {
                    if (_setHighChance)
                    {
                        if(_counter == 0)
                        {
                            id = sameChance;
                            break;
                        }
                    }
                    id = ChanceSettings.StopChance();
                }
                foreach (Image image in col.Items)
                {
                    if(IsSuitable(col,image,id))
                    {
                        image.Tag += " stop";
                        SetPreviousImages(col.Items.IndexOf(image), col);
                        break;
                    }
                }
            }
        }
        private bool IsSuitable(ListBox col,Image image,int id)
        {
            var max = ComputeMax(col);
            var cef = 30 * int.Parse(col.Name.Substring(6));
            return string.Equals(image.Tag.ToString(), id.ToString()) && col.Items.IndexOf(image) > 3 && col.Items.IndexOf(image) <= max - cef;
        }
        private bool IsExists(ListBox listBox, int id)
        {
            foreach(Image img in listBox.Items)
            {
                if(IsSuitable(listBox,img,id))
                {
                    return true;
                }
            }
            return false;
        }
        private void ResetColumns()
        {
            foreach(var col in FindColumns())
            {
                col.Items.Clear();
            }
            FillColumns();
        }
        private async void Play_Click(object sender, RoutedEventArgs e)
        {
            await StartSession();
            EndSession();
        }

        private async Task StartSession()
        {
            wining = 0;
            Play.IsEnabled = false;
            Earn.IsEnabled = false;
            Decrease.IsEnabled = false;
            Increase.IsEnabled = false;
            if (RedLine.Visibility == Visibility.Visible)
            {
                RedLine.Visibility = Visibility.Hidden;
            }
            if (SubDepositFromBank())
            {
                ResetColumns();
                Win.Text = "";
                SetStoppedElements();
                GetStoppedElements();
                await Task.Delay((int)(ColumnsDelay * 3));
                await Task.WhenAll(_currentTasks);
                if (CompareTags(out int id))
                {
                    wining = Winings[id] * ConvertToDouble(Deposits[index]);
                    var winCoef = wining * 10 * (ConvertToDouble(Deposits[index]) / 0.2);
                    _currentWining += winCoef;
                    if (_setHighChance)
                    {
                        if (_counter == 0)
                        {
                            _currentWining -= winCoef;
                            _setHighChance = false;
                        }
                    }
                    _currentWaste -= wining / Math.Sqrt(2);
                    AddChance(ref _currentWining, ComputeLimit(), SetLimit);
                }
            }
            else
            {
                Win.Text = "Ставку убавь";
            }
        }

        private void EndSession()
        {
            CheckButtons();
            Play.IsEnabled = true;
            Earn.IsEnabled = true;
            if(wining != 0)
            {
                Win.Text = $"Ваш выигрыш:  {wining.ToString("0.00").Replace('.',',')} BYN";
                Bank.Text = (ParseBank() + wining).ToString("0.00").Replace('.',',') + " BYN";
                RedLine.Visibility = Visibility.Visible;
            }
            if (ParseBank() <= 0.15)
            {
                Play.IsEnabled = false;
                Win.Text = "Нужен додепчик";
                Dodep.Visibility = Visibility.Visible;
            }
            _images.Clear();
        }

        private bool CompareTags(out int id)
        {
            id = -1;
            List<int> ints = new List<int>();
            foreach(Image item in _images)
            {
                ints.Add(int.Parse(item.Tag.ToString()));
            }
            if(ints[0] == ints[1] && ints[0] == ints[2])
            {
                id = ints[0];
                return true;
            }
            return false;
        }
        private double ComputeLimit()
        {
            return _limit * ConvertToDouble(Deposits[index]) / 0.2 * Math.Cbrt(ConvertToDouble(Deposits[index]) / 0.2);
        }
        private double ComputeWasteLimit()
        {
            return _wasteLimit * Math.Cbrt(0.4 / ConvertToDouble(Deposits[index]));
        }
        private void AddChance(ref double value,double limit,Action setLimit)
        {
            if (value >= limit)
            {
                value = 0;
                setLimit();
                _counter = ChanceSettings.rnd.Next(2, 7);
                _setHighChance = true;
            }
        }
        private async void GetStoppedElements()
        {
            foreach(ListBox col in FindColumns())
            {
                foreach(Image img in col.Items)
                {
                    if(img.Tag.ToString().Substring(1) == " stop")
                    {
                        int index = col.Items.IndexOf(img) - 1;
                        Image item = (Image)col.Items[index];
                        ScrollToItem(col, item);
                        img.Tag = img.Tag.ToString().Remove(1);
                        _images.Add(img);
                        await Task.Delay(ColumnsDelay);
                        break;
                    }
                }
            }
        }
        private ScrollViewer GetScrollViewer(ListBox col)
        {
            return (ScrollViewer)MyGrid.FindName("Scroll" + col.Name);
        }

        private void ScrollToItem(ListBox col, Image item)
        {
            var scroll = GetScrollViewer(col);

            VirtualizingPanel.SetIsVirtualizing(col, false);
            col.Items.Refresh();
            Dispatcher.Invoke(() => { }, DispatcherPriority.Render);

            var point = item.TranslatePoint(new Point(0, 0), scroll);
            double coords = 50.00 + point.Y + item.ActualHeight - scroll.ViewportHeight / 2;

            VirtualizingPanel.SetIsVirtualizing(col, true);
            _currentTasks.Add(ScrollAsync(-100, coords, scroll));
        }

        private async Task ScrollAsync(double startSpeed,double coords,ScrollViewer scroll)
        {
            double elapsed = 0.00;
            double startOffset = scroll.VerticalOffset;
            double offset = startOffset + 59.00;
            double accelerate = Math.Pow(startSpeed, 2) / (2 * Math.Abs(coords));
            double duration = -startSpeed / accelerate / 60;
            while (elapsed <= duration)
            {
                double speed = startSpeed + accelerate * elapsed * 60;
                offset += speed;
                scroll.ScrollToVerticalOffset(offset);
                await Task.Delay(16);
                elapsed += 1.00 / 60.00;
            }
            scroll.ScrollToVerticalOffset(startOffset + coords); //Корректировка
        }



        private void ShowDep()
        {
            Deposit.Text = Deposits[index];
        }

        private void CheckButtons()
        {
            if (index == 0)
            {
                Decrease.IsEnabled = false;
                Increase.IsEnabled = true;
            }
            else if(index == Deposits.Length - 1)
            {
                Increase.IsEnabled = false;
                Decrease.IsEnabled = true;
            }
            else
            {
                Decrease.IsEnabled = true;
                Increase.IsEnabled = true;
            }
        }
        private void Decrease_Click(object sender, RoutedEventArgs e)
        {
            index--;
            CheckButtons();
            ShowDep();

        }


        private void Increase_Click(object sender, RoutedEventArgs e)
        {
            index++;
            CheckButtons();
            ShowDep();
        }

        private void Dodep_Click(object sender, RoutedEventArgs e)
        {
            Bank.Text = ((ParseBank() + 10).ToString("0.00")).Replace('.',',') + " BYN";
            Dodep.Visibility = Visibility.Hidden;
            Play.IsEnabled = true;
            Win.Text = "";
        }

        private void Earn_Click(object sender, RoutedEventArgs e)
        {
            GetMoney();
        }
        private async void GetMoney()
        {
            Win.Text = "Ты вывел " + Bank.Text;
            Earn.IsEnabled = false;
            Play.IsEnabled = false;
            Bank.Text = "0,00 BYN";
            await Task.Delay(2000);
            Close();
        }
    }


}