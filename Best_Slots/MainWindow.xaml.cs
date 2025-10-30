using System.Diagnostics;
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
        public const int MAX = 65;

        private static Task animation;

        private static double _limit;
        private static double _wasteLimit;
        private static double _currentWining = 0;
        private static double _currentWaste = 0;

        private static bool _setHighChance = false;
        private static int _counter = 0;

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

        public static readonly Random rnd = new Random();

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

        private static readonly string[] Deposits = { "0,20", "0,50", "1,00", "2,00" };
        private int index = Deposits.Length - 1;
        public MainWindow()
        {
            InitializeComponent();
            SetLimit();
            SetWasteLimit();
            ShowDep();
            CheckButtons();
            FillColumns();
        }
        private void SetLimit()
        {
            _limit = rnd.Next(100, 200);
        }
        private void SetWasteLimit()
        {
            _wasteLimit = rnd.Next(20, 40);
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
                for(var i = 0; i < MAX; i++)
                {
                    int index = SpawnChance();
                    Image img = new Image()
                    {
                        Source = new BitmapImage(new Uri("images/" + Images[index], UriKind.Relative)),
                        Width = 145,
                        Tag = index,
                    };
                    col.Items.Add(img);
                    if (i == MAX)
                    {
                        col.ScrollIntoView(img);
                    }
                }
            }
        }
        private int SpawnChance()
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
        private int StopChance()
        {
            int zone = rnd.Next(100);
            if (zone < 30)
            {
                return 6;
            }
            if(zone >= 30 && zone < 60)
            {
                return 5;
            }
            if (zone >= 60 && zone < 80)
            {
                return rnd.Next(3, 5);
            }
            if (zone >= 80 && zone < 95)
            {
                return rnd.Next(1, 3);
            }
            return 0;
        }
        private double ConvertToDouble(string str)
        {
            return double.Parse(str);
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
                Bank.Text = (bank - dep).ToString("0.00") + " BYN";
                _currentWaste += dep;
                AddChance(ref _currentWaste,ComputeWasteLimit(),SetWasteLimit);
                return true;
            }
            else
            {
                return false;
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
                    sameChance = StopChance();
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
                    id = StopChance();
                }
                foreach (Image image in col.Items)
                {
                    if(IsSuitable(col,image,id))
                    {
                        image.Tag += " stop";
                        break;
                    }
                }
            }
        }
        private bool IsSuitable(ListBox col,Image image,int id)
        {
            return string.Equals(image.Tag.ToString(), id.ToString()) && col.Items.IndexOf(image) > 20 && col.Items.IndexOf(image) <= MAX - 5;
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
            StartSession(out double wining);
            await animation;
            EndSession(wining);
        }

        private void StartSession(out double wining)
        {
            wining = 0;
            Play.IsEnabled = false;
            Earn.IsEnabled = false;
            if (RedLine.Visibility == Visibility.Visible)
            {
                RedLine.Visibility = Visibility.Hidden;
            }
            if (SubDepositFromBank())
            {
                ResetColumns();
                Win.Text = "";
                SetStoppedElements();
                if (CompareTags(GetStoppedElements(), out int id))
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

        private void EndSession(double wining)
        {
            Play.IsEnabled = true;
            Earn.IsEnabled = true;
            if(wining != 0)
            {
                Win.Text = $"Ваш выигрыш:  {wining.ToString("0.00")} BYN";
                Bank.Text = (ParseBank() + wining).ToString("0.00") + " BYN";
                RedLine.Visibility = Visibility.Visible;
            }
            if (ParseBank() <= 0.15)
            {
                Play.IsEnabled = false;
                Win.Text = "Нужен додепчик";
                Dodep.Visibility = Visibility.Visible;
            }
        }

        private bool CompareTags(IEnumerable<Image> items, out int id)
        {
            id = -1;
            List<int> ints = new List<int>();
            foreach(Image item in items)
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
                _counter = rnd.Next(2, 7);
                _setHighChance = true;
            }
        }
        private IEnumerable<Image> GetStoppedElements()
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

                        yield return img;
                        break;
                    }
                }
            }
        }

        private void ScrollToItem(ListBox col, Image item)
        {
            var scroll = (ScrollViewer)MyGrid.FindName("Scroll" + col.Name);
            scroll.ScrollToBottom();

            VirtualizingPanel.SetIsVirtualizing(col, false);
            col.Items.Refresh();
            Dispatcher.Invoke(() => { }, DispatcherPriority.Render);

            var point = item.TranslatePoint(new Point(0, 0), scroll);
            var coords = 50.00 + point.Y + (item.ActualHeight - scroll.ViewportHeight / 2);
            animation = ScrollAsync(1, coords, scroll);

            VirtualizingPanel.SetIsVirtualizing(col, true);
        }

        private async Task ScrollAsync(double duration,double coords,ScrollViewer scroll)
        {
            double elapsed = 0.00;
            double distance = (coords / duration) / 60.00;
            while(elapsed <= duration)
            {
                elapsed += 1.00 / 60.00;
                scroll.ScrollToVerticalOffset(scroll.VerticalOffset + distance);
                await Task.Delay(16);
            }
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
            }
            else if(index == Deposits.Length - 1)
            {
                Increase.IsEnabled = false;
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
            Bank.Text = (ParseBank() + 10).ToString("0.00") + " BYN";
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
            Play.IsEnabled = false;
            Bank.Text = "0.00 BYN";
            await Task.Delay(2000);
            Close();
        }
    }


}