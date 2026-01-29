using System.Threading.Tasks;
using System.Windows;

namespace EventsManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            Auth.LoginSucceeded += OnLoginSucceeded;
            Auth.GuestSelected += OnGuestSelected;

            // стартуем гостем, авторизация показывается оверлеем
            Session.CurrentUser = null;
            ApplyAccess();

            MainContent.Content = null;
            Auth.Visibility = Visibility.Visible;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Delay(1200);
            LogoOverlay.Visibility = Visibility.Collapsed;
        }

        private void OnGuestSelected()
        {
            Session.CurrentUser = null;
            ApplyAccess();

            Auth.Visibility = Visibility.Collapsed;
            MainContent.Content = new PublicEventsPage();
        }

        private void OnLoginSucceeded(Пользователи user)
        {
            Session.CurrentUser = user;

            MessageBox.Show(
                $"Вход выполнен.\n{user.FIO}\nРоль: {Session.RoleName}",
                "Успешно",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Auth.Visibility = Visibility.Collapsed;
            ApplyAccess();
            MainContent.Content = new ProfilePage();
        }

        private string RoleKey()
        {
            return (Session.RoleName ?? "").Trim().ToLower();
        }

        private bool IsOrganizer()
        {
            var role = RoleKey();
            return role == "организатор" || role == "организаторы";
        }

        private void ApplyAccess()
        {
            // Видимость: мероприятия доступны всегда (в т.ч. гостю)
            BtnPublicEvents.Visibility = Visibility.Visible;

            // Остальное — только после входа
            BtnProfile.Visibility = Session.IsAuth ? Visibility.Visible : Visibility.Collapsed;

            var role = RoleKey();

            BtnParticipantGroup.Visibility = (Session.IsAuth && (role == "участник" || role == "участники"))
                ? Visibility.Visible : Visibility.Collapsed;

            BtnModeration.Visibility = (Session.IsAuth && (role == "модератор" || role == "модераторы"))
                ? Visibility.Visible : Visibility.Collapsed;

            BtnJury.Visibility = (Session.IsAuth && role == "жюри")
                ? Visibility.Visible : Visibility.Collapsed;

            // НОВОЕ: Активности — только организатор
            BtnActivities.Visibility = (Session.IsAuth && (role == "организатор" || role == "организаторы"))
                ? Visibility.Visible : Visibility.Collapsed;

            // Enabled
            BtnPublicEvents.IsEnabled = true;
            BtnProfile.IsEnabled = Session.IsAuth;
            BtnParticipantGroup.IsEnabled = Session.IsAuth;
            BtnModeration.IsEnabled = Session.IsAuth;
            BtnJury.IsEnabled = Session.IsAuth;
            BtnActivities.IsEnabled = Session.IsAuth;
        }

        // Навигация
        private void BtnPublicEvents_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new PublicEventsPage();
        }

        private void BtnProfile_Click(object sender, RoutedEventArgs e)
        {
            if (!Session.IsAuth) return;
            MainContent.Content = new ProfilePage();
        }

        private void BtnParticipantGroup_Click(object sender, RoutedEventArgs e)
        {
            if (!Session.IsAuth) return;
            MainContent.Content = new ParticipantGroupPage();
        }

        private void BtnModeration_Click(object sender, RoutedEventArgs e)
        {
            if (!Session.IsAuth) return;
            MainContent.Content = new ModerationPage();
        }

        private void BtnJury_Click(object sender, RoutedEventArgs e)
        {
            if (!Session.IsAuth) return;
            MainContent.Content = new JuryPage();
        }

        // НОВОЕ: переход на страницу управления активностями
        private void BtnActivities_Click(object sender, RoutedEventArgs e)
        {
            if (!Session.IsAuth) return;

            if (!IsOrganizer())
            {
                MessageBox.Show(
                    "Раздел «Активности» доступен только организатору.",
                    "Доступ запрещён",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            MainContent.Content = new ActivitiesPage();
        }
    }
}
