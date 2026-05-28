using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ExchangeOffice.WpfClient
{
    public partial class MainWindow : Window
    {
        private readonly string[] _currencies =
        {
            "PLN", "USD", "EUR", "GBP", "CHF", "JPY", "CAD", "AUD", "NOK", "SEK",
            "DKK", "CZK", "HUF", "RON", "BGN", "TRY", "UAH", "CNY", "HKD", "NZD", "ZAR"
        };
        private readonly string[] _balanceDisplayCurrencies =
        {
            "PLN", "USD", "EUR", "GBP", "CHF", "JPY", "CAD", "AUD", "NOK", "SEK",
            "DKK", "CZK", "HUF", "RON", "BGN", "TRY", "UAH", "CNY", "HKD", "NZD", "ZAR"
        };
        private readonly Dictionary<string, ImageSource> _flagImages = new Dictionary<string, ImageSource>();

        private IExchangeOfficeService _client;
        private ChannelFactory<IExchangeOfficeService> _factory;
        private UserDto _currentUser;
        private int _currentUserId;
        private bool _passwordVisible;
        private bool _syncingPassword;
        private const string FullNamePlaceholder = "Enter your fullname";
        private const string EmailPlaceholder = "example@mail.com";

        public MainWindow()
        {
            InitializeComponent();
            InitializeCurrencyPickers();
            HistoricalDatePicker.SelectedDate = DateTime.Today;
            SetLoggedInView(false);
        }

        private IExchangeOfficeService Client
        {
            get
            {
                if (_client == null)
                {
                    _factory = new ChannelFactory<IExchangeOfficeService>("ExchangeOfficeEndpoint");
                    _client = _factory.CreateChannel();
                }

                return _client;
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await RunAsync(async () =>
            {
                var message = await Task.Run(() => Client.Ping());
                await LoadUsersAsync(_currentUser == null ? null : _currentUser.Email);
                StatusTextBlock.Text = message;
            }, "Connecting to service...");
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            Logout();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ResetClient();
        }

        private async void CreateUser_Click(object sender, RoutedEventArgs e)
        {
            var fullName = GetTextBoxValue(FullNameTextBox, FullNamePlaceholder);
            var email = GetTextBoxValue(EmailTextBox, EmailPlaceholder);
            var password = GetPasswordValue();

            await RunAsync(async () =>
            {
                var user = await Task.Run(() => Client.CreateUser(fullName, email, password));
                SetActiveUser(user);
                await LoadUsersAsync(user.Email);
                await RefreshDataAsync();
                StatusTextBlock.Text = "User account created.";
            }, "Creating account...");
        }

        private async void LoginUser_Click(object sender, RoutedEventArgs e)
        {
            var email = GetTextBoxValue(EmailTextBox, EmailPlaceholder);
            var password = GetPasswordValue();

            await RunAsync(async () =>
            {
                var user = await Task.Run(() => Client.LoginUser(email, password));
                SetActiveUser(user);
                await LoadUsersAsync(user.Email);
                await RefreshDataAsync();
                StatusTextBlock.Text = "Logged in.";
            }, "Logging in...");
        }

        private void UsersComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var user = UsersComboBox.SelectedItem as UserDto;
            if (user == null)
            {
                return;
            }

            SetTextBoxValue(FullNameTextBox, user.FullName);
            SetTextBoxValue(EmailTextBox, user.Email);
            if (_currentUser == null || user.Id != _currentUser.Id)
            {
                _currentUser = null;
                _currentUserId = 0;
                BalancesGrid.ItemsSource = null;
                TransactionsGrid.ItemsSource = null;
                AccountValueTextBlock.Text = "Login to see account value.";
                BalancesHeaderTextBlock.Text = "Balances";
                UserTextBlock.Text = "Selected user: " + user.Email + ". Enter password and click Login.";
                StatusTextBlock.Text = "Password required for login.";
                SetLoggedInView(false);
            }
        }

        private async void CurrentRate_Click(object sender, RoutedEventArgs e)
        {
            var currency = GetCurrencyCode(RateCurrencyComboBox);
            await RunAsync(async () => ShowRate(await Task.Run(() => Client.GetCurrentRate(currency))), "Loading current rate...");
        }

        private async void HistoricalRate_Click(object sender, RoutedEventArgs e)
        {
            var currency = GetCurrencyCode(RateCurrencyComboBox);
            var date = HistoricalDatePicker.SelectedDate ?? DateTime.Today;
            await RunAsync(async () => ShowRate(await Task.Run(() => Client.GetHistoricalRate(currency, date))), "Loading historical rate...");
        }

        private async void TopUp_Click(object sender, RoutedEventArgs e)
        {
            var amount = ReadDecimal(AmountTextBox.Text);
            await RunAsync(async () =>
            {
                EnsureUser();
                await Task.Run(() => Client.TopUpPln(_currentUserId, amount));
                await RefreshDataAsync();
                StatusTextBlock.Text = "PLN balance topped up.";
            }, "Topping up PLN...");
        }

        private async void Buy_Click(object sender, RoutedEventArgs e)
        {
            var currency = GetCurrencyCode(TradeCurrencyComboBox);
            var currencyAmount = ReadDecimal(TradeAmountTextBox.Text);

            await RunAsync(async () =>
            {
                EnsureUser();
                if (currency == "PLN")
                {
                    StatusTextBlock.Text = "PLN is the base currency. Use Top up to add PLN balance.";
                    return;
                }

                var balances = await Task.Run(() => Client.GetBalances(_currentUserId));
                var rate = await Task.Run(() => Client.GetCurrentRate(currency));
                var requiredPln = decimal.Round(currencyAmount * rate.SellRate, 4, MidpointRounding.AwayFromZero);
                var plnBalance = GetBalanceAmount(balances, "PLN");
                if (plnBalance < requiredPln)
                {
                    StatusTextBlock.Text = string.Format("Not enough PLN. Buying {0} {1} requires {2} PLN. Current PLN balance: {3}.", currencyAmount, currency, requiredPln, plnBalance);
                    await ShowBalancesAsync(balances);
                    return;
                }

                var transaction = await Task.Run(() => Client.BuyCurrency(_currentUserId, currency, currencyAmount));
                await RefreshDataAsync();
                StatusTextBlock.Text = string.Format("Bought {0} {1} for {2} PLN.", transaction.CurrencyAmount, transaction.CurrencyCode, transaction.PlnAmount);
            }, "Buying currency...");
        }

        private async void Sell_Click(object sender, RoutedEventArgs e)
        {
            var currency = GetCurrencyCode(TradeCurrencyComboBox);
            var amount = ReadDecimal(TradeAmountTextBox.Text);

            await RunAsync(async () =>
            {
                EnsureUser();
                if (currency == "PLN")
                {
                    StatusTextBlock.Text = "PLN is the base currency. Selling PLN to PLN is not needed.";
                    return;
                }

                var balances = await Task.Run(() => Client.GetBalances(_currentUserId));
                var currencyBalance = GetBalanceAmount(balances, currency);
                if (currencyBalance < amount)
                {
                    StatusTextBlock.Text = string.Format("Not enough {0}. Current {0} balance: {1}. Buy {0} before selling.", currency, currencyBalance);
                    await ShowBalancesAsync(balances);
                    return;
                }

                var transaction = await Task.Run(() => Client.SellCurrency(_currentUserId, currency, amount));
                await RefreshDataAsync();
                StatusTextBlock.Text = string.Format("Sold {0} {1}.", transaction.CurrencyAmount, transaction.CurrencyCode);
            }, "Selling currency...");
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await RunAsync(async () =>
            {
                var result = await RefreshDataAsync();
                StatusTextBlock.Text = string.Format(
                    "Refreshed at {0:HH:mm:ss}. Balances: {1}, transactions: {2}.",
                    DateTime.Now,
                    result.BalanceCount,
                    result.TransactionCount);
            }, "Refreshing data...");
        }

        private async void BalanceCurrencyComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_currentUserId <= 0 || !IsLoaded)
            {
                return;
            }

            var displayCurrency = GetCurrencyCode(BalanceCurrencyComboBox);
            await RunAsync(async () =>
            {
                var balances = await Task.Run(() => Client.GetBalances(_currentUserId));
                await ShowBalancesAsync(balances);
                await UpdateAccountValueAsync(balances, displayCurrency);
                StatusTextBlock.Text = "Account value shown in " + displayCurrency + ".";
            }, "Updating account value...");
        }

        private async Task<RefreshResult> RefreshDataAsync()
        {
            EnsureUser();
            var balances = (await Task.Run(() => Client.GetBalances(_currentUserId))).ToList();
            var transactions = (await Task.Run(() => Client.GetTransactions(_currentUserId))).ToList();
            BalancesGrid.ItemsSource = null;
            TransactionsGrid.ItemsSource = null;
            await ShowBalancesAsync(balances);
            TransactionsGrid.ItemsSource = BuildTransactionRows(transactions);
            await UpdateAccountValueAsync(balances, GetCurrencyCode(BalanceCurrencyComboBox));
            return new RefreshResult
            {
                BalanceCount = balances.Count,
                TransactionCount = transactions.Count
            };
        }

        private void ShowRate(ExchangeRateDto rate)
        {
            RateTextBlock.Text = string.Format("{0} ({1})\nmid: {2}, buy: {3}, sell: {4}\ndate: {5:yyyy-MM-dd}",
                rate.CurrencyCode, rate.CurrencyName, rate.MidRate, rate.BuyRate, rate.SellRate, rate.EffectiveDate);
        }

        private void EnsureUser()
        {
            if (_currentUserId <= 0)
            {
                throw new InvalidOperationException("Create a user or login first.");
            }
        }

        private void SetActiveUser(UserDto user)
        {
            _currentUser = user;
            _currentUserId = user.Id;
            ClearPasswordFields();
            SetPlaceholder(FullNameTextBox, FullNamePlaceholder);
            SetPlaceholder(EmailTextBox, EmailPlaceholder);
            HelloTextBlock.Text = "Hello, " + user.FullName;
            UserTextBlock.Text = string.Empty;
            BalancesHeaderTextBlock.Text = "Balances - " + user.FullName;
            SetLoggedInView(true);
        }

        private async Task LoadUsersAsync(string selectedEmail)
        {
            var users = await Task.Run(() => Client.GetUsers());
            UsersComboBox.SelectionChanged -= UsersComboBox_SelectionChanged;
            UsersComboBox.ItemsSource = users;
            UsersComboBox.SelectedItem = users.FirstOrDefault(x => string.Equals(x.Email, selectedEmail, StringComparison.OrdinalIgnoreCase));
            UsersComboBox.SelectionChanged += UsersComboBox_SelectionChanged;
        }

        private async Task UpdateAccountValueAsync(IEnumerable<BalanceDto> balances, string displayCurrency)
        {
            var balanceList = balances.ToList();
            var totalPln = 0m;

            foreach (var balance in balanceList)
            {
                if (balance.Amount == 0m)
                {
                    continue;
                }

                if (string.Equals(balance.CurrencyCode, "PLN", StringComparison.OrdinalIgnoreCase))
                {
                    totalPln += balance.Amount;
                }
                else
                {
                    var rate = await Task.Run(() => Client.GetCurrentRate(balance.CurrencyCode));
                    totalPln += balance.Amount * rate.BuyRate;
                }
            }

            decimal displayAmount;
            if (displayCurrency == "PLN")
            {
                displayAmount = totalPln;
            }
            else
            {
                var targetRate = await Task.Run(() => Client.GetCurrentRate(displayCurrency));
                displayAmount = totalPln / targetRate.SellRate;
            }

            AccountValueTextBlock.Text = string.Format(
                "Total: {0} {1} (about {2} PLN)",
                decimal.Round(displayAmount, 4, MidpointRounding.AwayFromZero),
                displayCurrency,
                decimal.Round(totalPln, 2, MidpointRounding.AwayFromZero));
        }

        private async Task ShowBalancesAsync(IEnumerable<BalanceDto> balances)
        {
            var rows = new List<BalanceViewModel>();
            foreach (var balance in balances.OrderBy(x => x.CurrencyCode))
            {
                rows.Add(new BalanceViewModel
                {
                    Flag = GetFlagImage(balance.CurrencyCode),
                    CurrencyCode = balance.CurrencyCode,
                    Amount = balance.Amount,
                    PlnEquivalent = await CalculatePlnEquivalentAsync(balance)
                });
            }

            BalancesGrid.ItemsSource = rows;
        }

        private static IList<TransactionViewModel> BuildTransactionRows(IEnumerable<TransactionDto> transactions)
        {
            return transactions
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new TransactionViewModel
                {
                    CreatedAt = x.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    Type = x.Type,
                    CurrencyCode = x.CurrencyCode,
                    CurrencyAmount = x.CurrencyAmount,
                    PlnAmount = x.PlnAmount,
                    Rate = x.Rate
                })
                .ToList();
        }

        private void InitializeCurrencyPickers()
        {
            foreach (var code in _balanceDisplayCurrencies)
            {
                _flagImages[code] = CreateFlagImage(code);
            }

            RateCurrencyComboBox.ItemTemplate = CreateCurrencyTemplate();
            TradeCurrencyComboBox.ItemTemplate = CreateCurrencyTemplate();
            BalanceCurrencyComboBox.ItemTemplate = CreateCurrencyTemplate();

            RateCurrencyComboBox.ItemsSource = BuildCurrencyOptions(_currencies);
            TradeCurrencyComboBox.ItemsSource = BuildCurrencyOptions(_currencies);
            BalanceCurrencyComboBox.ItemsSource = BuildCurrencyOptions(_balanceDisplayCurrencies);

            SelectCurrency(RateCurrencyComboBox, "USD");
            SelectCurrency(TradeCurrencyComboBox, "USD");
            SelectCurrency(BalanceCurrencyComboBox, "USD");
        }

        private IList<CurrencyOption> BuildCurrencyOptions(IEnumerable<string> currencyCodes)
        {
            return currencyCodes.Select(code => new CurrencyOption
            {
                Code = code,
                Flag = GetFlagImage(code)
            }).ToList();
        }

        private static DataTemplate CreateCurrencyTemplate()
        {
            var stack = new FrameworkElementFactory(typeof(StackPanel));
            stack.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            var image = new FrameworkElementFactory(typeof(Image));
            image.SetValue(FrameworkElement.WidthProperty, 24.0);
            image.SetValue(FrameworkElement.HeightProperty, 16.0);
            image.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 0));
            image.SetBinding(Image.SourceProperty, new System.Windows.Data.Binding("Flag"));
            stack.AppendChild(image);

            var text = new FrameworkElementFactory(typeof(TextBlock));
            text.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            text.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Code"));
            stack.AppendChild(text);

            return new DataTemplate { VisualTree = stack };
        }

        private void SelectCurrency(ComboBox comboBox, string currencyCode)
        {
            comboBox.SelectedItem = comboBox.Items.Cast<CurrencyOption>().FirstOrDefault(x => x.Code == currencyCode);
        }

        private ImageSource GetFlagImage(string currencyCode)
        {
            if (!_flagImages.ContainsKey(currencyCode))
            {
                _flagImages[currencyCode] = CreateFlagImage(currencyCode);
            }

            return _flagImages[currencyCode];
        }

        private static ImageSource CreateFlagImage(string code)
        {
            var group = new DrawingGroup();
            DrawRect(group, Brushes.White, 0, 0, 30, 20);
            DrawRect(group, Brushes.LightGray, 0, 0, 30, 20, false);

            switch (code)
            {
                case "PLN":
                    DrawRect(group, Brushes.White, 0, 0, 30, 10);
                    DrawRect(group, Brushes.Crimson, 0, 10, 30, 10);
                    break;
                case "USD":
                    for (var i = 0; i < 7; i++) DrawRect(group, Brushes.Firebrick, 0, i * 3, 30, 1.5);
                    DrawRect(group, Brushes.Navy, 0, 0, 13, 10);
                    break;
                case "EUR":
                    DrawRect(group, Brushes.RoyalBlue, 0, 0, 30, 20);
                    DrawCircle(group, Brushes.Gold, 15, 10, 4);
                    break;
                case "GBP":
                    DrawRect(group, Brushes.MidnightBlue, 0, 0, 30, 20);
                    DrawRect(group, Brushes.White, 12, 0, 6, 20);
                    DrawRect(group, Brushes.White, 0, 7, 30, 6);
                    DrawRect(group, Brushes.Crimson, 13.5, 0, 3, 20);
                    DrawRect(group, Brushes.Crimson, 0, 8.5, 30, 3);
                    break;
                case "CHF":
                    DrawRect(group, Brushes.Red, 0, 0, 30, 20);
                    DrawRect(group, Brushes.White, 13, 4, 4, 12);
                    DrawRect(group, Brushes.White, 9, 8, 12, 4);
                    break;
                case "JPY":
                    DrawRect(group, Brushes.White, 0, 0, 30, 20);
                    DrawCircle(group, Brushes.Crimson, 15, 10, 5);
                    break;
                case "CAD":
                    DrawRect(group, Brushes.Red, 0, 0, 7, 20);
                    DrawRect(group, Brushes.White, 7, 0, 16, 20);
                    DrawRect(group, Brushes.Red, 23, 0, 7, 20);
                    DrawCircle(group, Brushes.Red, 15, 10, 4);
                    break;
                case "AUD":
                case "NZD":
                    DrawRect(group, Brushes.Navy, 0, 0, 30, 20);
                    DrawCircle(group, code == "AUD" ? Brushes.White : Brushes.Red, 22, 7, 2);
                    DrawCircle(group, code == "AUD" ? Brushes.White : Brushes.Red, 25, 13, 2);
                    break;
                case "NOK":
                    DrawRect(group, Brushes.Firebrick, 0, 0, 30, 20);
                    DrawRect(group, Brushes.White, 8, 0, 5, 20);
                    DrawRect(group, Brushes.White, 0, 8, 30, 5);
                    DrawRect(group, Brushes.Navy, 9.5, 0, 2, 20);
                    DrawRect(group, Brushes.Navy, 0, 9.5, 30, 2);
                    break;
                case "SEK":
                    DrawRect(group, Brushes.SteelBlue, 0, 0, 30, 20);
                    DrawRect(group, Brushes.Gold, 8, 0, 4, 20);
                    DrawRect(group, Brushes.Gold, 0, 8, 30, 4);
                    break;
                case "DKK":
                    DrawRect(group, Brushes.Firebrick, 0, 0, 30, 20);
                    DrawRect(group, Brushes.White, 8, 0, 4, 20);
                    DrawRect(group, Brushes.White, 0, 8, 30, 4);
                    break;
                case "CZK":
                    DrawRect(group, Brushes.White, 0, 0, 30, 10);
                    DrawRect(group, Brushes.Firebrick, 0, 10, 30, 10);
                    DrawTriangle(group, Brushes.RoyalBlue);
                    break;
                case "HUF":
                    DrawRect(group, Brushes.Firebrick, 0, 0, 30, 6.7);
                    DrawRect(group, Brushes.White, 0, 6.7, 30, 6.6);
                    DrawRect(group, Brushes.ForestGreen, 0, 13.3, 30, 6.7);
                    break;
                case "RON":
                    DrawRect(group, Brushes.RoyalBlue, 0, 0, 10, 20);
                    DrawRect(group, Brushes.Gold, 10, 0, 10, 20);
                    DrawRect(group, Brushes.Firebrick, 20, 0, 10, 20);
                    break;
                case "BGN":
                    DrawRect(group, Brushes.White, 0, 0, 30, 6.7);
                    DrawRect(group, Brushes.ForestGreen, 0, 6.7, 30, 6.6);
                    DrawRect(group, Brushes.Firebrick, 0, 13.3, 30, 6.7);
                    break;
                case "TRY":
                    DrawRect(group, Brushes.Red, 0, 0, 30, 20);
                    DrawCircle(group, Brushes.White, 12, 10, 5);
                    DrawCircle(group, Brushes.Red, 14, 10, 4);
                    DrawCircle(group, Brushes.White, 20, 10, 2);
                    break;
                case "UAH":
                    DrawRect(group, Brushes.RoyalBlue, 0, 0, 30, 10);
                    DrawRect(group, Brushes.Gold, 0, 10, 30, 10);
                    break;
                case "CNY":
                case "HKD":
                    DrawRect(group, Brushes.Red, 0, 0, 30, 20);
                    DrawCircle(group, Brushes.Gold, 9, 7, 3);
                    break;
                case "ZAR":
                    DrawRect(group, Brushes.ForestGreen, 0, 0, 30, 20);
                    DrawRect(group, Brushes.Gold, 0, 0, 9, 20);
                    DrawRect(group, Brushes.Black, 0, 0, 6, 20);
                    DrawRect(group, Brushes.RoyalBlue, 15, 10, 15, 10);
                    DrawRect(group, Brushes.Firebrick, 15, 0, 15, 10);
                    break;
            }

            group.Freeze();
            return new DrawingImage(group);
        }

        private static void DrawRect(DrawingGroup group, Brush brush, double x, double y, double width, double height, bool fill = true)
        {
            group.Children.Add(new GeometryDrawing(fill ? brush : null, fill ? null : new Pen(brush, 1), new RectangleGeometry(new Rect(x, y, width, height))));
        }

        private static void DrawCircle(DrawingGroup group, Brush brush, double x, double y, double radius)
        {
            group.Children.Add(new GeometryDrawing(brush, null, new EllipseGeometry(new Point(x, y), radius, radius)));
        }

        private static void DrawTriangle(DrawingGroup group, Brush brush)
        {
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(new Point(0, 0), true, true);
                context.LineTo(new Point(14, 10), true, false);
                context.LineTo(new Point(0, 20), true, false);
            }

            geometry.Freeze();
            group.Children.Add(new GeometryDrawing(brush, null, geometry));
        }

        private async Task<decimal> CalculatePlnEquivalentAsync(BalanceDto balance)
        {
            if (string.Equals(balance.CurrencyCode, "PLN", StringComparison.OrdinalIgnoreCase))
            {
                return decimal.Round(balance.Amount, 4, MidpointRounding.AwayFromZero);
            }

            if (balance.Amount == 0m)
            {
                return 0m;
            }

            var rate = await Task.Run(() => Client.GetCurrentRate(balance.CurrencyCode));
            return decimal.Round(balance.Amount * rate.BuyRate, 4, MidpointRounding.AwayFromZero);
        }

        private static decimal ReadDecimal(string text)
        {
            decimal value;
            if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
                && !decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
            {
                throw new InvalidOperationException("Invalid amount.");
            }

            return value;
        }

        private string GetCurrencyCode(ComboBox comboBox)
        {
            var option = comboBox.SelectedItem as CurrencyOption;
            if (option != null)
            {
                return option.Code;
            }

            return GetCurrencyCode(comboBox.Text);
        }

        private static string GetCurrencyCode(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("Choose a currency code.");
            }

            text = text.Trim().ToUpperInvariant();
            if (text.Length != 3)
            {
                throw new InvalidOperationException("Currency code must have three letters, for example USD.");
            }

            return text;
        }

        private static decimal GetBalanceAmount(IEnumerable<BalanceDto> balances, string currencyCode)
        {
            var balance = balances.FirstOrDefault(x => string.Equals(x.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase));
            return balance == null ? 0m : balance.Amount;
        }

        private void PlaceholderTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (IsPlaceholder(textBox))
            {
                textBox.Text = string.Empty;
                textBox.Foreground = Brushes.Black;
            }
        }

        private void PlaceholderTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (textBox == FullNameTextBox && string.IsNullOrWhiteSpace(textBox.Text))
            {
                SetPlaceholder(textBox, FullNamePlaceholder);
            }
            else if (textBox == EmailTextBox && string.IsNullOrWhiteSpace(textBox.Text))
            {
                SetPlaceholder(textBox, EmailPlaceholder);
            }
        }

        private void PasswordToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _passwordVisible = !_passwordVisible;
            if (_passwordVisible)
            {
                VisiblePasswordTextBox.Text = PasswordBox.Password;
                VisiblePasswordTextBox.Visibility = Visibility.Visible;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordToggleButton.Content = "Hide";
            }
            else
            {
                PasswordBox.Password = VisiblePasswordTextBox.Text;
                PasswordBox.Visibility = Visibility.Visible;
                VisiblePasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordToggleButton.Content = "Show";
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_syncingPassword || !_passwordVisible)
            {
                return;
            }

            _syncingPassword = true;
            VisiblePasswordTextBox.Text = PasswordBox.Password;
            _syncingPassword = false;
        }

        private void VisiblePasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_syncingPassword || !_passwordVisible)
            {
                return;
            }

            _syncingPassword = true;
            PasswordBox.Password = VisiblePasswordTextBox.Text;
            _syncingPassword = false;
        }

        private string GetPasswordValue()
        {
            return _passwordVisible ? VisiblePasswordTextBox.Text : PasswordBox.Password;
        }

        private void Logout()
        {
            _currentUser = null;
            _currentUserId = 0;
            BalancesGrid.ItemsSource = null;
            TransactionsGrid.ItemsSource = null;
            AccountValueTextBlock.Text = "Login and choose currency.";
            BalancesHeaderTextBlock.Text = "Balances";
            UserTextBlock.Text = "No user logged in.";
            HelloTextBlock.Text = string.Empty;
            ClearPasswordFields();
            SetPlaceholder(FullNameTextBox, FullNamePlaceholder);
            SetPlaceholder(EmailTextBox, EmailPlaceholder);
            UsersComboBox.SelectedItem = null;
            SetLoggedInView(false);
            StatusTextBlock.Text = "Logged out.";
        }

        private void SetLoggedInView(bool loggedIn)
        {
            var loginVisibility = loggedIn ? Visibility.Collapsed : Visibility.Visible;
            var loggedInVisibility = loggedIn ? Visibility.Visible : Visibility.Collapsed;

            LoggedInPanel.Visibility = loggedInVisibility;
            FullNameLabel.Visibility = loginVisibility;
            FullNameTextBox.Visibility = loginVisibility;
            EmailLabel.Visibility = loginVisibility;
            EmailTextBox.Visibility = loginVisibility;
            PasswordLabel.Visibility = loginVisibility;
            PasswordInputGrid.Visibility = loginVisibility;
            AccountButtonsPanel.Visibility = loginVisibility;
            ExistingUsersLabel.Visibility = loginVisibility;
            UsersComboBox.Visibility = loginVisibility;
            LogoutButton.IsEnabled = loggedIn;
        }

        private void ClearPasswordFields()
        {
            PasswordBox.Password = string.Empty;
            VisiblePasswordTextBox.Text = string.Empty;
            if (_passwordVisible)
            {
                _passwordVisible = false;
                PasswordBox.Visibility = Visibility.Visible;
                VisiblePasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordToggleButton.Content = "Show";
            }
        }

        private static string GetTextBoxValue(TextBox textBox, string placeholder)
        {
            return string.Equals(textBox.Text, placeholder, StringComparison.Ordinal) && textBox.Foreground == Brushes.Gray
                ? string.Empty
                : textBox.Text;
        }

        private static bool IsPlaceholder(TextBox textBox)
        {
            return textBox.Foreground == Brushes.Gray
                || string.Equals(textBox.Text, FullNamePlaceholder, StringComparison.Ordinal)
                || string.Equals(textBox.Text, EmailPlaceholder, StringComparison.Ordinal);
        }

        private static void SetPlaceholder(TextBox textBox, string placeholder)
        {
            textBox.Text = placeholder;
            textBox.Foreground = Brushes.Gray;
        }

        private static void SetTextBoxValue(TextBox textBox, string value)
        {
            textBox.Text = value;
            textBox.Foreground = Brushes.Black;
        }

        private async Task RunAsync(Func<Task> action, string workingMessage)
        {
            try
            {
                IsEnabled = false;
                StatusTextBlock.Text = workingMessage;
                await action();
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Error: " + ex.Message;
                ResetClient();
            }
            finally
            {
                IsEnabled = true;
            }
        }

        private void ResetClient()
        {
            try
            {
                var channel = _client as IClientChannel;
                if (channel != null) channel.Abort();
                if (_factory != null) _factory.Abort();
            }
            catch
            {
            }

            _client = null;
            _factory = null;
        }

    }

    public class BalanceViewModel
    {
        public ImageSource Flag { get; set; }
        public string CurrencyCode { get; set; }
        public decimal Amount { get; set; }
        public decimal PlnEquivalent { get; set; }
    }

    public class CurrencyOption
    {
        public string Code { get; set; }
        public ImageSource Flag { get; set; }
    }

    public class TransactionViewModel
    {
        public string CreatedAt { get; set; }
        public string Type { get; set; }
        public string CurrencyCode { get; set; }
        public decimal CurrencyAmount { get; set; }
        public decimal PlnAmount { get; set; }
        public decimal Rate { get; set; }
    }

    public class RefreshResult
    {
        public int BalanceCount { get; set; }
        public int TransactionCount { get; set; }
    }
}
