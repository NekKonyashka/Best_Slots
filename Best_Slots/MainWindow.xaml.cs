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
        public const int MAX = 55;

        //Для подкрутки
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
            [5] = 7.00,
            [6] = 4.50
        };

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

            //Запрет расширения окна
            ResizeMode = ResizeMode.NoResize;

            InitializeComponent();
            factory.SetDictionary(ImageLibrary.FacultImages); //загрузка словаря с факультетами

            ShowDep(DepositFlow.Current()); //Показ текущей ставки(самая крайняя)
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
        //Метод заполнения каждой колонны
        public void FillColumns()
        {
            foreach (var col in _columns)
            {
                //У каждой высчитывается свое максимальное кол-во.
                //Это сделано, чтобы у колонн была разная продолжительность анимации
                var maximum = ComputeMax(col); 

                for (var i = 0; i < maximum; i++)
                { 
                    Image img = factory.CreateImage();
                    col.Items.Add(img);

                    //Если истинно, то удаляет последние три изображения,
                    //чтобы впоследствии заполнить изображениями после последней прокрутки
                    if (!isBegin && i >= maximum - 3)
                    {
                        col.Items.RemoveAt(maximum - 3);
                    }
                }
                foreach(var img in _prevImages[GetColumnIndex(col) - 1]) //Вот соответственно и заполнение
                {
                    col.Items.Add(img);
                }
                //В самом начале при запуске коллекция пустая, а так как при каждом прокруте все колонны перезаполняются,
                //то нужно запомнить с каких изображений стартует игра
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
        //Очищает изображения после предыдущего прокрута
        private void ClearPrevious()
        {
            foreach (var list in _prevImages)
            {
                list.Clear();
            }
        }

        //Возвращает номер колонны по ее имени
        private int GetColumnIndex(ListBox listBox)
        {
            return int.Parse(listBox.Name.Substring(6));
        }

        //Функция вычисления максимального кол-ва изображений для колонн
        private int ComputeMax(ListBox listBox)
        {
            return (int)(MAX * Math.Sqrt(GetColumnIndex(listBox)));
        }

        //Корректная конвертация double(т.к. в зависимости от локализации появляется . либо ,)
        private double ConvertToDouble(string str)
        {
            return double.Parse(str.Replace(',','.'));
        }

        //Парсинг текста денежной суммы, расположенного в правом верхнем углу
        private double ParseBank()
        {
            string pattern = @"\d+\,\d+";
            var match = Regex.Match(Bank.Text,pattern);
            return ConvertToDouble(match.Value);
        }

        //Метод вычета ставки из суммы, параллельно производит различные манипуляции, необходимые для подкрутки
        private bool SubDepositFromBank()
        {
            var bank = ParseBank();
            var dep = ConvertToDouble(DepositFlow.Current());
            if(dep <= bank)
            {
                Bank.Text = ((bank - dep).ToString("0.00") + " BYN").Replace('.',',');
                _currentWaste += dep;
                AddChance(ref _currentWaste,ComputeWasteLimit(),LimitsHandler.SetWasteLimit);
                return true;
            }
            else
            {
                return false;
            }
        }

        //Функция, которая записывает 9 изображений, которые видны на экране перед начало новой прокрутки
        private void SetPreviousImages(int index,ListBox list)
        {
            int id = GetColumnIndex(list) - 1;
            for(int i = index - 1, j = 0; j < 3; i++, j++)
            {
                _prevImages[id].Add((Image)list.Items[i]);
            }
        }
        //Спец проверка для значения, выпавшего в ходе подкрутки,
        //т.к. оно гарантированно для всех трех столбцов и нужно заранее убедится,
        //что изображение с выпавшим индексом вообще существует в нем)
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

        //Выбирает и устанавливает изображения, на которых остановится прокрут
        private void SetStoppedElements()
        {
            //Зараннее просчитывается общее для всех значение, если должна осуществиться подкрутка
            int sameChance = -1;
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
            //Стандартная выборка индексов изображения для каждой из колонн
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
        }

        //Проверка, будет ли входить это изображение с текущим индексом в диапозон определенного столбца
        private bool IsSuitable(ListBox col,Image image,int id)
        {
            var max = ComputeMax(col);
            var cef = MAX / 2 * GetColumnIndex(col);
            return string.Equals(image.Tag.ToString(), id.ToString()) && col.Items.IndexOf(image) > 3 && col.Items.IndexOf(image) <= max - cef;
        }

        //Проверка, существует ли изображение с текущим индексом с учетом проверки на вхождение в диапозон в столбце
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

        //Очистка всех колллон и заполнение новыми изображениями
        private void ResetColumns()
        {
            foreach(var col in _columns)
            {
                col.Items.Clear();
            }
            FillColumns();
        }

        //Функция, запускаемая при нажатии на кнопку старта
        private async void Play_Click(object sender, RoutedEventArgs e)
        {
            if (!_sessionStarted)
            {
                if (SubDepositFromBank())
                {
                    _sessionStarted = true;
                    Win.Text = "";
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
        //Функция отключения/включения всех кнопок, находящихся на экране
        private void EnabledButtons(bool isEnabled)
        {
            foreach(var button in MyGrid.Children.OfType<Button>())
            {
                button.IsEnabled = isEnabled;
            }
        }

        //Изменение изображения кнопки старта в зависимости от состояния игры
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

        //Старт игровой сессии, где проходят все основные механики, описываемые до
        private async Task StartSession()
        {
            wining = 0;
            ResetColumns();
            EnabledButtons(false);
            ChangeTemplateImage();
            SetStoppedElements();
            PrepareScrollStoppedElements();
            await Task.Delay((int)(ColumnsDelay * 3.5)); //Ожидает старта всех анимаций, чтобы они все были помещены в массив Task-ов
            Play.IsEnabled = true;
            await Task.WhenAll(_currentTasks);//ожидание завершения всех анимаций
            //Выполняется при выпадении 3 одинаковых элементов
            if (CompareTags(out int id))
            {
                _player.SoundLocation = "sounds/" + SoundsLibrary.GetSound(id);
                wining = Winings[id] * ConvertToDouble(DepositFlow.Current());

                //Подкрутка
                var winCoef = wining * 10 * (ConvertToDouble(DepositFlow.Current()) / 0.2);
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
                AddChance(ref _currentWining, ComputeLimit(), LimitsHandler.SetLimit);
            }
        }

        //Завершение игровой сессии
        private void EndSession()
        {
            _sessionStarted = false;
            _isStoped = false;
            EnabledButtons(true);
            CheckButtons();
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

        //Сравнение тегов элементов, которые были выбраны
        private bool CompareTags(out int id)
        {
            id = -1;
            int num = int.Parse(_images[0].Tag.ToString());
            if(_images.All(im => int.Parse(im.Tag.ToString()) == num))
            {
                id = num;
                return true;
            }
            return false;
        }
        //Подкрутка
        private double ComputeLimit()
        {
            return LimitsHandler.Limit * ConvertToDouble(DepositFlow.Current()) / 0.2 * Math.Cbrt(ConvertToDouble(DepositFlow.Current()) / 0.2);
        }
        //Подкрутка
        private double ComputeWasteLimit()
        {
            return LimitsHandler.WasteLimit * Math.Cbrt(ConvertToDouble(DepositFlow.Current()) / 2);
        }
        //Подкрутка
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
        //Получение элементов, которые были помещены в спец. коллекцию элементов
        //Так же тут происходит запуск анимации прокрутки для каждого изображения соответствующего столбца
        private async void PrepareScrollStoppedElements()
        {
            int i = 0;
            foreach(ListBox col in _columns)
            {
                for(;i < _images.Count;)
                {
                    Image img = _images[i];
                    int index = col.Items.IndexOf(img);
                    SetPreviousImages(index, col);

                    ScrollToItem(col, img);
                    await Task.Delay(ColumnsDelay);
                    break;
                }
                i++;
            }
        }
        //Получает объект скролла у каждой колонны элементов
        private ScrollViewer GetScrollViewer(ListBox col)
        {
            return (ScrollViewer)MyGrid.FindName("Scroll" + col.Name);
        }
        //Запускает анимацию прокрутка к выбранному изображению
        private void ScrollToItem(ListBox col, Image item)
        {
            var scroll = GetScrollViewer(col);

            //Надо для корректного отображения координаты изображения, т.к. он находится за пределами окна
            VirtualizingPanel.SetIsVirtualizing(col, false);
            Dispatcher.Invoke(() => { }, DispatcherPriority.Render);

            double coords = ComputeImageCoords(scroll, item); //Вычисление нужной координаты

            VirtualizingPanel.SetIsVirtualizing(col, true);

            _currentTasks.Add(ScrollAsync(-125, coords, scroll));//Добавление Task-а из анимации в массив "текущих анимаций"
        }

        //Получение координаты центра изображения по центру окна казино
        private double ComputeImageCoords(ScrollViewer scroll, Image item)
        {
            var point = item.TranslatePoint(new Point(0, 0), scroll);
            return point.Y + item.ActualHeight / 2 - (scroll.ViewportHeight + 6) / 2;
        }

        //Метод асинхронной прокрутки с ускорением
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
                await Task.Delay(10);
                elapsed += 1.00;
            }
            if (_isStoped) //При остановке меняется логика
            {
                offset = startOffset + coords;
                elapsed = 0.00;
                startSpeed = 5;
                duration = 20.00 / startSpeed / 60.00;
                while (elapsed <= duration)
                {
                    scroll.ScrollToVerticalOffset(offset);
                    offset += startSpeed;
                    await Task.Delay(10);
                    elapsed += 1.00 / 60.00;
                }
            }
            scroll.ScrollToVerticalOffset(startOffset + coords); //Корректировка
        }

        //Запуск конечной анимации выпавших изображений(масштабирование)
        private void EndAnimation()
        {
            foreach(var image in _images)
            {
                ImageAnimation(image);
            }
        }

        //Конечная анимация изображения
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
                await Task.Delay(10);
            }
            scale.ScaleX = 1;
            scale.ScaleY = 1;
        }

        //Обновляет ставку
        private void ShowDep(string dep)
        {
            Deposit.Text = dep;
        }


        //Проверяет на возможность изменения ставки
        //(при достижении одного из краев блокирует кнопку, чтобы нельзя было выйти за границу массива)
        private void CheckButtons()
        {
            if (!DepositFlow.CanMovePrev)
            {
                Decrease.IsEnabled = false;
                Increase.IsEnabled = true;
            }
            else if(!DepositFlow.CanMoveNext)
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

        //Срабатывает при клике на кнопку уменьшения ставки
        private void Decrease_Click(object sender, RoutedEventArgs e)
        {
            ShowDep(DepositFlow.Previous());
            CheckButtons();
        }

        //Срабатывает при клике на кнопку увеличения ставки
        private void Increase_Click(object sender, RoutedEventArgs e)
        {
            ShowDep(DepositFlow.Next());
            CheckButtons();
        }

        //Срабатывает при клике на кнопку додепа
        private void Dodep_Click(object sender, RoutedEventArgs e)
        {
            Bank.Text = ((ParseBank() + 10).ToString("0.00")).Replace('.',',') + " BYN";
            Dodep.Visibility = Visibility.Hidden;
            Play.IsEnabled = true;
            Win.Text = "";
        }

        //Срабатывает при клике на кнопку вывода средств
        private void Earn_Click(object sender, RoutedEventArgs e)
        {
            GetMoney();
        }
        //Запуск асинхронного закрытия окна
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

        //Срабатывает при клике на кнопку смены темы
        private void ThemeChanger_Click(object sender, RoutedEventArgs e)
        {
            _themeChanged = !_themeChanged;
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
                ThemeChanger.RenderTransform = new TranslateTransform(0, 55);
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
            //Обновляет все столбцы так, как будто это начало игры
            isBegin = true;
            ClearPrevious();
            ResetColumns();
            Scrolling();
            isBegin = false;
        }

    }


}