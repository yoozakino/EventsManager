using System;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace EventsManager
{
    public partial class ModerationPage : UserControl
    {
        private Активности[] _myActivities = Array.Empty<Активности>();
        private Активности _selected;

        public ModerationPage()
        {
            InitializeComponent();
            Loaded += async (_, __) => await InitAsync();
        }

        private bool IsModerator()
        {
            var role = (Session.RoleName ?? "").Trim().ToLower();
            return Session.IsAuth && (role == "модератор" || role == "модераторы");
        }

        private async Task InitAsync()
        {
            if (!IsModerator())
            {
                MessageBox.Show("Этот раздел доступен только модератору.",
                    "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.IsEnabled = false;
                return;
            }

            await LoadMyActivitiesAsync();
        }

        private async Task LoadMyActivitiesAsync()
        {
            if (Session.CurrentUser == null) return;

            using (var db = new InfoSecEventsEntities())
            {
                int myId = Session.CurrentUser.ID;

                // В Активности есть поле Moderator (int) [file:4]
                _myActivities = await db.Активности
                    .Where(a => a.Moderator == myId)
                    .OrderBy(a => a.EventID)
                    .ThenBy(a => a.Day)
                    .ThenBy(a => a.StartTime)
                    .ToArrayAsync();
            }

            MyActivitiesGrid.ItemsSource = _myActivities;

            // сброс правой панели
            _selected = null;
            NameBox.Text = "";
            DayBox.Text = "";
            TimeBox.Text = "";
            EventIdBox.Text = "";
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            _ = LoadMyActivitiesAsync();
        }

        private void MyActivitiesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selected = MyActivitiesGrid.SelectedItem as Активности;
            if (_selected == null) return;

            NameBox.Text = _selected.Name ?? "";
            DayBox.Text = _selected.Day.ToString(CultureInfo.InvariantCulture);
            TimeBox.Text = _selected.StartTime.ToString(); // TimeSpan обычно красиво печатается
            EventIdBox.Text = _selected.EventID.ToString(CultureInfo.InvariantCulture);
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null)
            {
                MessageBox.Show("Выбери активность слева.", "Модерация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // валидация
            var newName = (NameBox.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("Название не может быть пустым.", "Модерация",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(DayBox.Text?.Trim(), out int newDay) || newDay <= 0)
            {
                MessageBox.Show("День должен быть числом > 0.", "Модерация",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(EventIdBox.Text?.Trim(), out int newEventId) || newEventId < 0)
            {
                MessageBox.Show("EventID должен быть числом (>= 0).", "Модерация",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TimeSpan.TryParse(TimeBox.Text?.Trim(), out TimeSpan newTime))
            {
                MessageBox.Show("Время введено неверно. Пример: 09:00 или 09:00:00.", "Модерация",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var db = new InfoSecEventsEntities())
                {
                    int myId = Session.CurrentUser.ID;

                    // защищаемся: редактировать можно только свои активности [file:4]
                    var act = await db.Активности.FirstOrDefaultAsync(a => a.ID == _selected.ID && a.Moderator == myId);
                    if (act == null)
                    {
                        MessageBox.Show("Активность не найдена или не принадлежит вам.", "Модерация",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    act.Name = newName;
                    act.Day = newDay;
                    act.StartTime = newTime;
                    act.EventID = newEventId;

                    await db.SaveChangesAsync();
                }

                await LoadMyActivitiesAsync();
                MessageBox.Show("Сохранено.", "Модерация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка сохранения",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
