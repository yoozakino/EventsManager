using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace EventsManager
{
    public partial class ParticipantGroupPage : UserControl
    {
        private Мероприятия[] _allEvents = Array.Empty<Мероприятия>();
        private bool _init;

        public ParticipantGroupPage()
        {
            InitializeComponent();
            Loaded += async (_, __) => await InitAsync();
        }

        private bool IsParticipant()
        {
            var role = (Session.RoleName ?? "").Trim().ToLower();
            return Session.IsAuth && (role == "участник" || role == "участники");
        }

        private async Task InitAsync()
        {
            if (!IsParticipant())
            {
                MessageBox.Show("Этот раздел доступен только участнику.",
                    "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.IsEnabled = false;
                return;
            }

            _init = true;

            // Заполняем из профиля (это строки в БД) [file:4]
            UserEventBox.Text = Session.CurrentUser?.Event ?? "";
            UserDirectionBox.Text = Session.CurrentUser?.Direction ?? "";

            using (var db = new InfoSecEventsEntities())
            {
                _allEvents = await db.Мероприятия
                    .Include(e => e.Активности)
                    .OrderByDescending(e => e.ID)
                    .ToArrayAsync();
            }

            EventPicker.ItemsSource = _allEvents;

            var preferred = (Session.CurrentUser?.Event ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(preferred))
            {
                var match = _allEvents.FirstOrDefault(e =>
                    (e.Name ?? "").Equals(preferred, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                    EventPicker.SelectedItem = match;
            }

            if (EventPicker.SelectedItem == null && _allEvents.Length > 0)
                EventPicker.SelectedIndex = 0;

            _init = false;

            UpdateRightPanel();
        }

        private void EventPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_init) return;

            // Удобство: при выборе из базы — подставим название в поле "Мероприятие"
            var ev = EventPicker.SelectedItem as Мероприятия;
            if (ev != null)
                UserEventBox.Text = ev.Name ?? "";

            UpdateRightPanel();
        }

        private void UpdateRightPanel()
        {
            var ev = EventPicker.SelectedItem as Мероприятия;
            if (ev == null)
            {
                EventTitle.Text = "";
                ActivitiesGrid.ItemsSource = null;
                return;
            }

            EventTitle.Text = ev.Name ?? "";

            var activities = (ev.Активности ?? Array.Empty<Активности>())
                .OrderBy(a => a.Day)
                .ThenBy(a => a.StartTime)
                .ToArray();

            ActivitiesGrid.ItemsSource = activities;
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            if (!Session.IsAuth || Session.CurrentUser == null) return;

            var newEvent = (UserEventBox.Text ?? "").Trim();
            var newDirection = (UserDirectionBox.Text ?? "").Trim();

            using (var db = new InfoSecEventsEntities())
            {
                var user = db.Пользователи.FirstOrDefault(u => u.ID == Session.CurrentUser.ID);
                if (user == null) return;

                user.Event = newEvent;
                user.Direction = newDirection;

                db.SaveChanges();

                // обновляем сессию, чтобы на других экранах тоже было актуально
                Session.CurrentUser.Event = newEvent;
                Session.CurrentUser.Direction = newDirection;
            }

            MessageBox.Show("Сохранено.", "Профиль",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
