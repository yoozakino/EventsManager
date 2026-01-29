using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace EventsManager
{
    public partial class PublicEventsPage : UserControl
    {
        private Мероприятия[] _allEvents = Array.Empty<Мероприятия>();

        public PublicEventsPage()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadEventsAsync();
        }

        private bool IsOrganizer()
        {
            var role = (Session.RoleName ?? "").Trim().ToLower();
            return Session.IsAuth && (role == "организатор" || role == "организаторы");
        }

        private void ApplyOrganizerUI()
        {
            OrganizerPanel.Visibility = IsOrganizer()
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private async Task LoadEventsAsync()
        {
            ApplyOrganizerUI();

            using (var db = new InfoSecEventsEntities())
            {
                _allEvents = await db.Мероприятия
                    .Include(e => e.Города)
                    .Include(e => e.Активности)
                    .OrderByDescending(e => e.ID)
                    .ToArrayAsync();
            }

            EventsList.ItemsSource = _allEvents;
            if (_allEvents.Length > 0)
                EventsList.SelectedIndex = 0;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = (SearchBox.Text ?? "").Trim();

            if (string.IsNullOrWhiteSpace(q))
            {
                EventsList.ItemsSource = _allEvents;
                return;
            }

            EventsList.ItemsSource = _allEvents
                .Where(x => (x.Name ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
        }

        private void EventsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var evnt = EventsList.SelectedItem as Мероприятия;
            if (evnt == null)
            {
                EventTitle.Text = "";
                EventInfo.Text = "";
                ActivitiesList.ItemsSource = null;
                return;
            }

            EventTitle.Text = evnt.Name ?? "";

            var cityName = evnt.Города?.Name ?? $"CityID: {evnt.CityID}";
            EventInfo.Text =
                $"Дата: {evnt.Date}\n" +
                $"Дней: {evnt.NumberOfDays}\n" +
                $"Город: {cityName}";

            ActivitiesList.ItemsSource = (evnt.Активности ?? Array.Empty<Активности>())
                .OrderBy(a => a.Day)
                .ThenBy(a => a.StartTime)
                .ToArray();
        }

        private async void AddEvent_Click(object sender, RoutedEventArgs e)
        {
            if (!IsOrganizer())
            {
                MessageBox.Show("Добавление доступно только организатору.",
                    "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var w = new EventEditWindow(null);
            w.Owner = Application.Current.MainWindow;
            w.ShowDialog();

            await LoadEventsAsync();
        }

        private async void DeleteEvent_Click(object sender, RoutedEventArgs e)
        {
            if (!IsOrganizer())
            {
                MessageBox.Show("Удаление доступно только организатору.",
                    "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selected = EventsList.SelectedItem as Мероприятия;
            if (selected == null) return;

            using (var db = new InfoSecEventsEntities())
            {
                var ev = db.Мероприятия.FirstOrDefault(x => x.ID == selected.ID);
                if (ev == null) return;

                bool hasActivities = db.Активности.Any(a => a.EventID == ev.ID);
                if (hasActivities)
                {
                    MessageBox.Show("Нельзя удалить мероприятие: в нём уже есть активности.",
                        "Удаление запрещено", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var confirm = MessageBox.Show(
                    "Удалить мероприятие без возможности восстановления?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                    return;

                db.Мероприятия.Remove(ev);
                db.SaveChanges();
            }

            await LoadEventsAsync();
        }
    }
}
