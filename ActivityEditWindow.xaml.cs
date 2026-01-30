using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace EventsManager
{
    public partial class ActivityEditWindow : Window
    {
        private readonly int? _activityId;
        private Активности _entity;
        private bool _isInitializing;

        public ActivityEditWindow(int? activityId)
        {
            InitializeComponent();

            _activityId = activityId;

            _isInitializing = true;
            LoadEvents();
            LoadEntityOrPrepareNew();
            _isInitializing = false;

            RebuildTimeSlots();
        }

        private void LoadEvents()
        {
            using (var db = new InfoSecEventsEntities())
            {
                EventBox.ItemsSource = db.Мероприятия.ToList();
            }
        }

        private void LoadEntityOrPrepareNew()
        {
            using (var db = new InfoSecEventsEntities())
            {
                if (_activityId.HasValue)
                {
                    _entity = db.Активности.First(x => x.ID == _activityId.Value);

                    IdBox.Text = _entity.ID.ToString();
                    NameBox.Text = _entity.Name ?? "";
                    ModeratorBox.Text = _entity.Moderator.ToString();

                    SelectEventById(_entity.EventID);
                    InitDaysForSelectedEvent(_entity.EventID);
                    DayBox.SelectedItem = _entity.Day;

                    Title = "Редактирование активности";
                }
                else
                {
                    _entity = new Активности();

                    IdBox.Text = "(новая)";
                    ModeratorBox.Text = "0";

                    Title = "Добавление активности";

                    if (EventBox.Items.Count > 0)
                        EventBox.SelectedIndex = 0;

                    InitDaysForSelectedEvent(GetSelectedEventId());
                    if (DayBox.Items.Count > 0)
                        DayBox.SelectedIndex = 0;
                }
            }
        }

        private void SelectEventById(int eventId)
        {
            foreach (var item in EventBox.Items)
            {
                var ev = item as Мероприятия;
                if (ev != null && ev.ID == eventId)
                {
                    EventBox.SelectedItem = item;
                    return;
                }
            }
        }

        private int GetSelectedEventId()
        {
            var ev = EventBox.SelectedItem as Мероприятия;
            return ev != null ? ev.ID : 0;
        }

        private int GetSelectedDay()
        {
            return DayBox.SelectedItem is int ? (int)DayBox.SelectedItem : 1;
        }

        private void InitDaysForSelectedEvent(int eventId)
        {
            using (var db = new InfoSecEventsEntities())
            {
                var ev = db.Мероприятия.FirstOrDefault(x => x.ID == eventId);
                int days = (ev != null ? ev.NumberOfDays : 1);
                if (days < 1) days = 1;

                DayBox.ItemsSource = Enumerable.Range(1, days).ToList();
            }
        }

        private void EventBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            if (EventBox.SelectedItem == null) return;

            InitDaysForSelectedEvent(GetSelectedEventId());
            if (DayBox.Items.Count > 0)
                DayBox.SelectedIndex = 0;

            RebuildTimeSlots();
        }

        private void DayBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            RebuildTimeSlots();
        }

        private void RebuildTimeSlots()
        {
            int eventId = GetSelectedEventId();
            if (eventId == 0) return;

            int day = GetSelectedDay();

            var allSlots = GenerateSlots(
                new TimeSpan(9, 0, 0),
                new TimeSpan(18, 0, 0),
                TimeSpan.FromMinutes(90),
                TimeSpan.FromMinutes(15));

            HashSet<TimeSpan> busy;
            using (var db = new InfoSecEventsEntities())
            {
                var q = db.Активности.Where(x => x.EventID == eventId && x.Day == day);

                if (_activityId.HasValue)
                    q = q.Where(x => x.ID != _activityId.Value);

                busy = new HashSet<TimeSpan>(q.Select(x => x.StartTime).ToList());
            }

            var available = allSlots.Where(s => !busy.Contains(s)).ToList();

            if (_activityId.HasValue && _entity != null)
            {
                if (available.All(x => x != _entity.StartTime))
                    available.Insert(0, _entity.StartTime);
            }

            StartTimeBox.ItemsSource = available;

            if (_activityId.HasValue && _entity != null)
                StartTimeBox.SelectedItem = _entity.StartTime;
            else if (available.Count > 0)
                StartTimeBox.SelectedIndex = 0;
        }

        private static List<TimeSpan> GenerateSlots(TimeSpan start, TimeSpan end, TimeSpan duration, TimeSpan breakTime)
        {
            var res = new List<TimeSpan>();
            var t = start;

            while (t + duration <= end)
            {
                res.Add(t);
                t = t + duration + breakTime;
            }

            return res;
        }

        private int GetNextActivityId(InfoSecEventsEntities db)
        {
            int maxId = 0;
            if (db.Активности.Any())
                maxId = db.Активности.Max(x => x.ID);

            return maxId + 1;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Заполните название активности.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var ev = EventBox.SelectedItem as Мероприятия;
            if (ev == null)
            {
                MessageBox.Show("Выберите мероприятие.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (StartTimeBox.SelectedItem == null || !(StartTimeBox.SelectedItem is TimeSpan))
            {
                MessageBox.Show("Выберите время начала.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var st = (TimeSpan)StartTimeBox.SelectedItem;

            int modId;
            if (!int.TryParse((ModeratorBox.Text ?? "").Trim(), out modId))
                modId = 0;

            using (var db = new InfoSecEventsEntities())
            {
                Активности target;

                if (_activityId.HasValue)
                {
                    target = db.Активности.First(x => x.ID == _activityId.Value);
                }
                else
                {
                    target = new Активности();
                    target.ID = GetNextActivityId(db); // КЛЮЧЕВАЯ СТРОКА
                }

                target.Name = NameBox.Text.Trim();
                target.EventID = ev.ID;
                target.Day = GetSelectedDay();
                target.StartTime = st;
                target.Moderator = modId;

                if (!_activityId.HasValue)
                    db.Активности.Add(target);

                db.SaveChanges();
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
