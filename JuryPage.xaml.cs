using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace EventsManager
{
    public partial class JuryPage : UserControl
    {
        private JuryActivityVm[] _items = Array.Empty<JuryActivityVm>();
        private JuryActivityVm _selected;

        public JuryPage()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadAllAsync();
        }

        private async Task LoadAllAsync()
        {
            if (Session.CurrentUser == null)
            {
                MessageBox.Show("Нет пользователя в сессии (Session.CurrentUser == null).");
                return;
            }

            await LoadMyActivitiesAsync();

            _selected = null;
            EventNameText.Text = "—";
            CandidatesCombo.ItemsSource = null;
            CandidatesCombo.SelectedIndex = -1;
            WinnerHint.Text = "Выберите активность слева.";
        }

        private async Task LoadMyActivitiesAsync()
        {
            int myId = Session.CurrentUser.ID;

            try
            {
                using (var db = new InfoSecEventsEntities())
                {
                    // ЖюриАктивностей(ActivityID, JuryID) -> Активности(EventID...). [file:4]
                    _items = await db.ЖюриАктивностей
                        .Where(ja => ja.JuryID == myId)
                        .Select(ja => ja.Активности)
                        .OrderBy(a => a.EventID)
                        .ThenBy(a => a.Day)
                        .ThenBy(a => a.StartTime)
                        .Select(a => new JuryActivityVm
                        {
                            ActivityID = a.ID,
                            EventID = a.EventID,
                            Day = a.Day,
                            StartTime = a.StartTime,
                            ActivityName = a.Name
                        })
                        .ToArrayAsync();
                }

                MyJuryActivitiesGrid.ItemsSource = _items;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка загрузки активностей",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => _ = LoadMyActivitiesAsync();

        private async void MyJuryActivitiesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selected = MyJuryActivitiesGrid.SelectedItem as JuryActivityVm;
            if (_selected == null) return;

            await LoadRightPanelAsync();
        }

        private async Task LoadRightPanelAsync()
        {
            if (_selected == null) return;

            try
            {
                using (var db = new InfoSecEventsEntities())
                {
                    var ev = await db.Мероприятия.FirstOrDefaultAsync(x => x.ID == _selected.EventID);
                    EventNameText.Text = ev != null ? ev.Name : "Мероприятие не найдено";

                    if (ev == null)
                    {
                        CandidatesCombo.ItemsSource = null;
                        CandidatesCombo.SelectedIndex = -1;
                        WinnerHint.Text = "Мероприятие не найдено.";
                        return;
                    }

                    int myId = Session.CurrentUser != null ? Session.CurrentUser.ID : 0;
                    const int juryRoleId = 0;

                    // Кандидаты: все НЕ жюри (RoleID != 0) и не текущий пользователь. [file:4]
                    var candidates = await db.Пользователи
                        .Where(u => u.ID != myId)
                        .Where(u => u.RoleID != juryRoleId)
                        .OrderBy(u => u.FIO)
                        .ToListAsync();

                    CandidatesCombo.ItemsSource = candidates;

                    if (candidates.Count == 0)
                    {
                        CandidatesCombo.SelectedIndex = -1;
                        WinnerHint.Text = "Кандидатов нет (в базе нет пользователей кроме жюри).";
                        return;
                    }

                    // Если победитель уже выбран — автоматически выбираем его в ComboBox. [file:4]
                    if (ev.WinnerID != null)
                    {
                        CandidatesCombo.SelectedValue = ev.WinnerID.Value;

                        var winner = candidates.FirstOrDefault(u => u.ID == ev.WinnerID.Value);
                        WinnerHint.Text = winner != null
                            ? $"Текущий победитель: {winner.FIO} (ID={winner.ID}). Можно переназначить."
                            : $"Текущий WinnerID={ev.WinnerID}, но такого пользователя нет в списке кандидатов.";
                    }
                    else
                    {
                        CandidatesCombo.SelectedIndex = 0;
                        WinnerHint.Text = "Победитель ещё не назначен. Выберите кандидата и нажмите кнопку.";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SetWinner_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null)
            {
                MessageBox.Show("Сначала выберите активность слева.", "Жюри",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var winner = CandidatesCombo.SelectedItem as Пользователи;
            if (winner == null)
            {
                MessageBox.Show("Выберите кандидата.", "Жюри",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using (var db = new InfoSecEventsEntities())
                {
                    var ev = await db.Мероприятия.FirstOrDefaultAsync(x => x.ID == _selected.EventID);
                    if (ev == null)
                    {
                        MessageBox.Show("Мероприятие не найдено.", "Жюри",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    ev.WinnerID = winner.ID; // WinnerID есть в таблице Мероприятия. [file:4]
                    await db.SaveChangesAsync();
                }

                // Перечитаем и снова выделим WinnerID
                await LoadRightPanelAsync();

                MessageBox.Show("Победитель назначен.", "Жюри",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка сохранения",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class JuryActivityVm
    {
        public int ActivityID { get; set; }
        public int EventID { get; set; }
        public int Day { get; set; }
        public TimeSpan StartTime { get; set; }
        public string ActivityName { get; set; }
    }
}
