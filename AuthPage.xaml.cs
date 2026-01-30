using System;
using System.Data.Entity;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace EventsManager
{
    public partial class AuthPage : UserControl
    {
        public Пользователи LoggedUser { get; private set; }

        public event Action<Пользователи> LoginSucceeded;
        public event Action GuestSelected;

        public AuthPage()
        {
            InitializeComponent();
            Loaded += (_, __) => IdBox.Focus();
        }

        private void GuestBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ErrorText.Text = "";
            GuestSelected?.Invoke();
        }

        private void IdBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        private void AnyBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                _ = DoLoginAsync();
        }

        private void LoginBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _ = DoLoginAsync();
        }

        private async Task DoLoginAsync()
        {
            ErrorText.Text = "";

            if (!int.TryParse(IdBox.Text?.Trim(), out int id))
            {
                ErrorText.Text = "Введите корректный ID Number (число).";
                return;
            }

            string pwd = PwdBox.Password ?? "";
            if (string.IsNullOrWhiteSpace(pwd))
            {
                ErrorText.Text = "Введите пароль.";
                return;
            }

            LoginBtn.IsEnabled = false;
            GuestBtn.IsEnabled = false;

            try
            {
                using (var db = new InfoSecEventsEntities())
                {
                    LoggedUser = await db.Пользователи
                        .AsNoTracking()
                        .Include(u => u.Роли)
                        .FirstOrDefaultAsync(u => u.ID == id && u.Password == pwd);
                }

                if (LoggedUser == null)
                {
                    ErrorText.Text = "Неверный ID или пароль.";
                    return;
                }

                LoginSucceeded?.Invoke(LoggedUser);
            }
            catch (Exception ex)
            {
                ErrorText.Text = "Ошибка авторизации: " + ex.Message;
            }
            finally
            {
                LoginBtn.IsEnabled = true;
                GuestBtn.IsEnabled = true;
            }
        }
    }
}
