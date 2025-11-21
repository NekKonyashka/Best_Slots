using System.Diagnostics;
using System.Globalization;
using System.Media;
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
using System.Windows.Media.Effects;
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
        private static readonly ImageFactory factory = new ImageFactory();

        //Создание полей, которые хранят объект цветов нижней панели и заднего фона в соответствии с выбранной тематикой.
        //Сделал, чтобы каждый раз не создавался новый объект.
        private static readonly SolidColorBrush _langBottomPanel = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF000B54"));
        private static readonly SolidColorBrush _facBottomPanel = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF876245"));
        private static readonly SolidColorBrush _langDepPanel = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF202E8C"));
        private static readonly SolidColorBrush _facDepPanel = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFA4724C"));
        private static readonly ImageBrush _langFill = new ImageBrush(new BitmapImage(new Uri("images/back/lang.jpg", UriKind.Relative)));
        private static readonly ImageBrush _facFill = new ImageBrush(new BitmapImage(new Uri("images/back/bstu.jpg", UriKind.Relative)));

        private static SoundPlayer _player = new SoundPlayer();

        //Внутренние изображения у кнопок Play и ThemeChanger для быстрого доступа к полю Source.
        private static Image _templatePlayImage;
        private static Image _templateThemeImage;

        //Поля, которые хранят объекты изображений для кнопок Play И ThemeChanger, которые меняются по клику.
        private static readonly BitmapImage _templateStart = new BitmapImage(new Uri("images/buttons/button-stop.png", UriKind.Relative));
        private static readonly BitmapImage _templateStop = new BitmapImage(new Uri("images/buttons/button-play.png", UriKind.Relative));
        private static readonly BitmapImage _templateFacult = new BitmapImage(new Uri("images/buttons/facultTheme.png", UriKind.Relative));
        private static readonly BitmapImage _templateLang = new BitmapImage(new Uri("images/buttons/langTheme.png", UriKind.Relative));

        //Поле с столбцами
        private static List<ListBox> _columns = new List<ListBox>();

        //Задержка между столбцами во время анимации.
        public static int ColumnsDelay = 200; 

        //кол-во элементов в первом стобце, через которое высчитывается кол-во во втором и третьем
        public const int MAX = 65;

        //Для подкрутки
        private static double _limit;
        private static double _wasteLimit;
        private static double _currentWining = 0;
        private static double _currentWaste = 0;

        //Всякие событийные булевые значения
        private bool isBegin = true;
        private static bool _setHighChance = false;
        private static bool _isStoped = false;
        private static bool _sessionStarted = false;
        private static bool _themeChanged = false;
        private static int _counter;

        private static double wining = 0;

        //Список с тремя выбранными картинками
        private static readonly List<Image> _images = new List<Image>();

        //Список с вложенными списками, в каждом из которых по 3 элемента из каждого столбца,
        //сделан, чтобы при новом вращении 9 видимых элементов оставались на экране и с них начиналась прокрутка.
        private static readonly List<List<Image>> _prevImages = new List<List<Image>>() { new List<Image>(), new List<Image>(), new List<Image>() };

        //Словарь, получающий выигрыш по ставке рубль в зависимости от индекса картинки
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

        //Массив с ставками
        private static readonly string[] Deposits = { "0,20", "0,50", "1,00", "2,00" };
        private int index = Deposits.Length - 1;

        //Список с тасками, которые добавляются во время асинхронных операций(анимация прокрутки)
        //Создан для ожидания завершения анимации
        private static List<Task> _currentTasks = new List<Task>();
        public MainWindow()
        {
            //Жесткая установка локализации, чтобы дробные числа не ломались
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            //Запрет расширения окна,т.к. я не настроил 
            ResizeMode = ResizeMode.NoResize;

            InitializeComponent();
            factory.SetDictionary(ImageLibrary.FacultImages); //загрузка словаря с факультетами

            //Подкрутка
            SetLimit();
            SetWasteLimit();

            ShowDep(); //Показ текущей ставки(самая крайняя)
            CheckButtons(); //Проверка кнопок уменьшения/увеличения депозита

            _columns = FindColumns();
            FillColumns(); //Заполнение картинок

            //Добавление обработчика события, которое срабатывает при показе окна для правильного отображения всех элементов
            ContentRendered += (s, e) =>
            {
                Scrolling();
                ShowColumns();
                _templatePlayImage = (Image)Play.Template.FindName("img", Play);
                _templateThemeImage = (Image)ThemeChanger.Template.FindName("change", ThemeChanger);
            };
            isBegin = false;
        }

        //Метод прокрутки скролла каждого столбца к предпоследнему элементу, чтобы прокручивание при запуске начиналось всегде с нижнего элемента
        private void Scrolling()
        {
            foreach (var col in _columns)
            {
                var item = (Image)col.Items[col.Items.Count - 2];
                item.RenderTransform = null; // сброс анимации расширения элемента

                //Небольшое ожидание после сброса, чтобы координата элемента правильно посчиталась
                col.UpdateLayout();
                Dispatcher.BeginInvoke(() => {},DispatcherPriority.Render);

                var scroll = GetScrollViewer(col);
                double coords = ComputeImageCoords(scroll, item);
                scroll.ScrollToVerticalOffset(scroll.VerticalOffset + Math.Abs(coords)); //Прокрутка с текущей позиции скролла к координате 
            }
        }
        //Изначально столбцы скрыты, поэтому метод их делает видимыми
        private void ShowColumns()
        {
            foreach(var col in _columns)
            {
                col.Visibility = Visibility.Visible;
            }
        }

        //Подкрутка
        private void SetLimit()
        {
            _limit = ChanceSettings.rnd.Next(100, 200);
        }
        //Подкрутка
        private void SetWasteLimit()
        {
            _wasteLimit = ChanceSettings.rnd.Next(20, 40);
        }
        //Метод для поиска всех стобцов внутри сетки Grid по их имени
        private List<ListBox> FindColumns()
        {
            var list = new List<ListBox>();
            for(int i = 1;i <= 3;i++)
            {
                var name = "Column" + i;
                list.Add(MyGrid.FindName(name) as ListBox);
            }
            return list;
        }
        public void FillColumns()
        {
            foreach (var col in _columns)
            {
                var maximum = ComputeMax(col);
                for (var i = 0; i < maximum; i++)
                { 
                    Image img = factory.CreateImage();
                    col.Items.Add(img);
                    if(!isBegin && i >= maximum - 3)
                    {
                        col.Items.RemoveAt(maximum - 3);
                    }
                }
                foreach(var img in _prevImages[GetColumnIndex(col) - 1])
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
                ClearPrevious();
            }
        }
        private void ClearPrevious()
        {
            foreach (var list in _prevImages)
            {
                list.Clear();
            }
        }
        private int GetColumnIndex(ListBox listBox)
        {
            return int.Parse(listBox.Name.Substring(6));
        }
        private int ComputeMax(ListBox listBox)
        {
            return (int)(MAX * Math.Sqrt(GetColumnIndex(listBox)));
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
            int id = GetColumnIndex(list) - 1;
            for(int i = index - 1, j = 0; j < 3; i++, j++)
            {
                _prevImages[id].Add((Image)list.Items[i]);
            }
        }
        private bool CheckSameChance(int sameChance)
        {
            foreach(var col in _columns)
            {
                if(!IsExists(col, sameChance))
                {
                    return false;
                }
            }
            return true;
        }
        private void SetStoppedElements()
        {
            int sameChance = 0;
            if (_setHighChance)
            {
                _counter--;
                if(_counter == 0)
                {
                    do
                    {
                        sameChance = ChanceSettings.StopChance();
                    }
                    while (!CheckSameChance(sameChance));
                }
            }
            foreach (ListBox col in _columns)
            {
                int id;
                do
                {
                    if (_setHighChance)
                    {
                        if (_counter == 0)
                        {
                            id = sameChance;
                            break;
                        }
                    }
                    id = ChanceSettings.StopChance();
                }
                while (!IsExists(col, id));
            }
            AddStopToAllTag();
        }
        private void AddStopToAllTag()
        {
            foreach(var img in _images)
            {
                img.Tag += " stop";
            }
        }
        private bool IsSuitable(ListBox col,Image image,int id)
        {
            var max = ComputeMax(col);
            var cef = 30 * GetColumnIndex(col);
            return string.Equals(image.Tag.ToString(), id.ToString()) && col.Items.IndexOf(image) > 3 && col.Items.IndexOf(image) <= max - cef;
        }
        private bool IsExists(ListBox listBox, int id)
        {
            foreach(Image img in listBox.Items)
            {
                if(IsSuitable(listBox,img,id))
                {
                    _images.Add(img);
                    return true;
                }
            }
            return false;
        }
        private void ResetColumns()
        {
            foreach(var col in _columns)
            {
                col.Items.Clear();
            }
            FillColumns();
        }
        private async void Play_Click(object sender, RoutedEventArgs e)
        {
            if (!_sessionStarted)
            {
                if (SubDepositFromBank())
                {
                    Win.Text = "";
                    _sessionStarted = true;
                    RedLine.Visibility = Visibility.Hidden;
                    await StartSession();
                    EndSession();
                }
                else
                {
                    Win.Text = "Ставку убавь";
                }
            }
            else
            {
                _isStoped = true;
            }
        }

        private void EnabledButtons(bool isEnabled)
        {
            foreach(var button in MyGrid.Children.OfType<Button>())
            {
                button.IsEnabled = isEnabled;
            }
        }
        private void ChangeTemplateImage()
        {
            if (_sessionStarted)
            {
                _templatePlayImage.Source = _templateStart;
            }
            else
            {
                _templatePlayImage.Source = _templateStop;
            }
        }
        private async Task StartSession()
        {
            wining = 0;
            ResetColumns();
            EnabledButtons(false);
            ChangeTemplateImage();
            SetStoppedElements();
            GetStoppedElements();
            await Task.Delay((int)(ColumnsDelay * 3.5));
            Play.IsEnabled = true;
            await Task.WhenAll(_currentTasks);
            if (CompareTags(out int id))
            {
                _player.SoundLocation = "sounds/" + SoundsLibrary.GetSound(id);
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

        private void EndSession()
        {
            EnabledButtons(true);
            CheckButtons();
            _sessionStarted = false;
            _isStoped = false;
            ChangeTemplateImage();
            if (wining != 0)
            {
                EndAnimation();
                _player.Play();
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
            int i = 0;
            foreach(ListBox col in _columns)
            {
                for(;i < _images.Count;)
                {
                    Image img = _images[i];
                    var h = img.ActualHeight;
                    int index = col.Items.IndexOf(img);
                    SetPreviousImages(index, col);
                    Image item = (Image)col.Items[index];
                    ScrollToItem(col, item);
                    img.Tag = img.Tag.ToString().Remove(1);
                    await Task.Delay(ColumnsDelay);
                    break;
                }
                i++;
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
            //col.Items.Refresh();
            Dispatcher.Invoke(() => { }, DispatcherPriority.Render);

            double coords = ComputeImageCoords(scroll, item);
            //double coords = ((ComputeMax(col) - col.Items.IndexOf(item)) * -item.ActualHeight - item.ActualHeight) + scroll.ViewportHeight / 2;

            VirtualizingPanel.SetIsVirtualizing(col, true);
            _currentTasks.Add(ScrollAsync(-125, coords, scroll));
        }

        private double ComputeImageCoords(ScrollViewer scroll, Image item)
        {
            var point = item.TranslatePoint(new Point(0, 0), scroll);
            return point.Y + item.ActualHeight / 2 - (scroll.ViewportHeight + 6) / 2;
        }

        private async Task ScrollAsync(double startSpeed,double coords,ScrollViewer scroll)
        {
            double elapsed = 0;
            double startOffset = scroll.VerticalOffset;
            double offset = startOffset;
            double accelerate = Math.Pow(startSpeed, 2) / (2 * Math.Abs(coords));
            double duration = Math.Abs(startSpeed / accelerate);
            while (elapsed <= duration && !_isStoped)
            {
                offset = startOffset + startSpeed * elapsed + 0.5 * accelerate * elapsed * elapsed;
                scroll.ScrollToVerticalOffset(offset);
                await Task.Delay(16);
                elapsed += 1.00;
            }
            if (_isStoped)
            {
                /*
                double distance = offset - (startOffset + coords) - 60;
                startSpeed = -300;
                elapsed = 0.00;
                duration = distance / Math.Abs(startSpeed) / 60.00;
                while (elapsed <= duration)
                {
                    scroll.ScrollToVerticalOffset(offset);
                    offset += startSpeed;
                    await Task.Delay(16);
                    elapsed += 1.00 / 60.00;
                }
                */
                offset = startOffset + coords;
                elapsed = 0.00;
                startSpeed = 5;
                duration = 20.00 / startSpeed / 60.00;
                while (elapsed <= duration)
                {
                    scroll.ScrollToVerticalOffset(offset);
                    offset += startSpeed;
                    await Task.Delay(16);
                    elapsed += 1.00 / 60.00;
                }
            }
            scroll.ScrollToVerticalOffset(startOffset + coords); //Корректировка
        }

        private void EndAnimation()
        {
            foreach(var image in _images)
            {
                ImageAnimation(image);
            }
        }
        private async Task ImageAnimation(Image image)
        {
            var scale = new ScaleTransform();
            image.RenderTransform = scale;

            double elapsed = 0.0;
            double duration = 1.3;
            double offset = image.ActualWidth;
            double speed = -0.02;
            while (elapsed <= duration && !_sessionStarted)
            {
                elapsed += 1.00 / 60.00;
                if (scale.ScaleX >= 1.25 || scale.ScaleX <= 1)
                {
                    speed *= -1;
                }
                scale.ScaleX += speed;
                scale.ScaleY += speed;
                await Task.Delay(16);
            }
            scale.ScaleX = 1;
            scale.ScaleY = 1;
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
            if(ParseBank() < 1)
            {
                Win.Text = "Недостаточно денег на вывод.";
            }
            else
            {
                Win.Text = "Ты вывел " + Bank.Text;
                Bank.Text = "0,00 BYN";
                EnabledButtons(false);
                await Task.Delay(2000);
                Close();
            }
        }

        private void ThemeChanger_Click(object sender, RoutedEventArgs e)
        {
            _themeChanged = _themeChanged ? false : true;
            Win.Text = "";
            RedLine.Visibility = Visibility.Hidden;
            if (_themeChanged)
            {
                factory.SetDictionary(ImageLibrary.LangImages);
                Background.Fill = _langFill;
                BottomPanel.Fill = _langBottomPanel;
                DepPanel.Fill = _langDepPanel;
                _templateThemeImage.Source = _templateLang;
                Bank.Foreground = new SolidColorBrush(Colors.White);
                ThemeChanger.RenderTransform = new TranslateTransform(0, 45);
            }
            else
            {
                factory.SetDictionary(ImageLibrary.FacultImages);
                Background.Fill = _facFill;
                BottomPanel.Fill = _facBottomPanel;
                DepPanel.Fill = _facDepPanel;
                _templateThemeImage.Source = _templateFacult;
                Bank.Foreground = new SolidColorBrush(Colors.Black);
                ThemeChanger.RenderTransform = new TranslateTransform(0, 0);
            }
            isBegin = true;
            ClearPrevious();
            ResetColumns();
            Scrolling();
            isBegin = false;
        }
    }


}