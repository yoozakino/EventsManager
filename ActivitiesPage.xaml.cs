using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace EventsManager
{
    public partial class ActivitiesPage : UserControl
    {
        private List<Активности> _allActivities;
        private ICollectionView _view;
        private ActivityEditWindow _editWindow;

        public ActivitiesPage()
        {
            InitializeComponent();

            var role = (Session.RoleName ?? "").Trim().ToLower();
            if (!Session.IsAuth || (role != "организатор" && role != "организаторы"))
            {
                MessageBox.Show(
                    "Эта страница доступна только организатору.",
                    "Доступ запрещён",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                this.IsEnabled = false;
                return;
            }

            LoadData();
        }

        private void LoadData()
        {
            using (var db = new InfoSecEventsEntities())
            {
                _allActivities = db.Активности
                    .Include("Мероприятия")
                    .ToList();

                var events = db.Мероприятия.ToList();
                events.Insert(0, new Мероприятия { ID = 0, Name = "Все мероприятия" });

                EventFilterBox.ItemsSource = events;
                if (EventFilterBox.SelectedIndex < 0)
                    EventFilterBox.SelectedIndex = 0;
            }

            _view = CollectionViewSource.GetDefaultView(_allActivities);
            _view.Filter = FilterActivity;

            ApplySort();
            ActivitiesGrid.ItemsSource = _view;
        }

        private bool FilterActivity(object obj)
        {
            var a = obj as Активности;
            if (a == null) return false;

            int selectedEventId = 0;
            var ev = EventFilterBox.SelectedItem as Мероприятия;
            if (ev != null) selectedEventId = ev.ID;

            if (selectedEventId != 0 && a.EventID != selectedEventId)
                return false;

            var q = (SearchBox.Text ?? "").Trim().ToLower();
            if (q.Length == 0) return true;

            var hay = string.Format("{0} {1}",
                    a.Name,
                    a.Мероприятия != null ? a.Мероприятия.Name : "")
                .ToLower();

            return hay.Contains(q);
        }

        private void ApplySort()
        {
            if (_view == null) return;

            _view.SortDescriptions.Clear();

            var selectedItem = SortBox.SelectedItem as ComboBoxItem;
            var tag = selectedItem != null
                ? (selectedItem.Tag != null ? selectedItem.Tag.ToString() : "ASC")
                : "ASC";

            var dir = tag == "DESC"
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;

            _view.SortDescriptions.Add(new SortDescription(nameof(Активности.StartTime), dir));
        }

        private void RefreshView()
        {
            if (_view != null) _view.Refresh();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshView();
        private void EventFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshView();

        private void SortBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplySort();
            RefreshView();
        }

        private void AddActivity_Click(object sender, RoutedEventArgs e)
        {
            OpenEditWindow(null);
        }

        private void ActivitiesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var a = ActivitiesGrid.SelectedItem as Активности;
            if (a == null) return;

            OpenEditWindow(a.ID);
        }

        private void OpenEditWindow(int? activityId)
        {
            if (_editWindow != null && _editWindow.IsVisible)
            {
                _editWindow.Activate();
                return;
            }

            _editWindow = new ActivityEditWindow(activityId);
            _editWindow.Owner = Application.Current.MainWindow;
            _editWindow.Closed += (s, e) => _editWindow = null;

            _editWindow.ShowDialog();
            LoadData();
        }

        private void DeleteActivity_Click(object sender, RoutedEventArgs e)
        {
            var a = ActivitiesGrid.SelectedItem as Активности;
            if (a == null) return;

            using (var db = new InfoSecEventsEntities())
            {
                var item = db.Активности
                    .FirstOrDefault(x => x.ID == a.ID);

                if (item == null) return;

                // Раньше была many-to-many и можно было item.Пользователи.Any().
                // Теперь связь через entity ЖюриАктивностей: проверяем строки по ActivityID. [file:4]
                bool hasJury = db.ЖюриАктивностей.Any(x => x.ActivityID == item.ID);

                if (hasJury)
                {
                    MessageBox.Show(
                        "Нельзя удалить активность: для неё уже назначено жюри.",
                        "Удаление запрещено",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var confirm = MessageBox.Show(
                    "Удалить активность без возможности восстановления?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                    return;

                db.Активности.Remove(item);
                db.SaveChanges();
            }

            LoadData();
        }
    }
}
