using System;
using System.Linq;
using System.Windows;

namespace EventsManager
{
    public partial class EventEditWindow : Window
    {
        private readonly int? _eventId;

        public EventEditWindow(int? eventId)
        {
            InitializeComponent();
            _eventId = eventId;

            LoadCities();
            LoadEntityOrPrepareNew();
        }

        private void LoadCities()
        {
            using (var db = new InfoSecEventsEntities())
            {
                CityBox.ItemsSource = db.Города.OrderBy(x => x.Name).ToList();
            }
        }

        private int GetNextEventId(InfoSecEventsEntities db)
        {
            int maxId = 0;
            if (db.Мероприятия.Any())
                maxId = db.Мероприятия.Max(x => x.ID);
            return maxId + 1;
        }

        private void LoadEntityOrPrepareNew()
        {
            using (var db = new InfoSecEventsEntities())
            {
                if (_eventId.HasValue)
                {
                    var ev = db.Мероприятия.First(x => x.ID == _eventId.Value);

                    IdBox.Text = ev.ID.ToString();
                    NameBox.Text = ev.Name ?? "";
                    DateBox.Text = ev.Date ?? "";

                    // город
                    foreach (var item in CityBox.Items)
                    {
                        var c = item as Города;
                        if (c != null && c.ID == ev.CityID)
                        {
                            CityBox.SelectedItem = item;
                            break;
                        }
                    }

                    Title = "Редактирование мероприятия";
                }
                else
                {
                    IdBox.Text = "(новое)";
                    Title = "Добавление мероприятия";

                    if (CityBox.Items.Count > 0)
                        CityBox.SelectedIndex = 0;
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Заполните название мероприятия.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var city = CityBox.SelectedItem as Города;
            if (city == null)
            {
                MessageBox.Show("Выберите город.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(DateBox.Text))
            {
                MessageBox.Show("Заполните дату.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var db = new InfoSecEventsEntities())
            {
                Мероприятия target;

                if (_eventId.HasValue)
                {
                    target = db.Мероприятия.First(x => x.ID == _eventId.Value);
                }
                else
                {
                    target = new Мероприятия();
                    target.ID = GetNextEventId(db); // важно, если ID не identity [file:4]
                }

                target.Name = NameBox.Text.Trim();
                target.CityID = city.ID;
                target.Date = DateBox.Text.Trim();

                // В БД у тебя есть NumberOfDays (int) — при необходимости добавь поле на форму.
                if (!_eventId.HasValue)
                {
                    // минимально: 1 день по умолчанию
                    target.NumberOfDays = 1;
                }

                if (!_eventId.HasValue)
                    db.Мероприятия.Add(target);

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
