using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace EventsManager
{
    public partial class ProfilePage : UserControl
    {
        public ProfilePage()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            if (!Session.IsAuth || Session.CurrentUser == null)
            {
                MessageBox.Show("Нужна авторизация.");
                return;
            }

            using (var db = new InfoSecEventsEntities())
            {
                var countries = await db.Cтраны
                    .OrderBy(c => c.CountryName)
                    .ToArrayAsync();

                CountryCombo.ItemsSource = countries;
                CountryCombo.DisplayMemberPath = "CountryName";
                CountryCombo.SelectedValuePath = "Code"; // Code int (PK) [file:4]

                var userId = Session.CurrentUser.ID;

                var user = await db.Пользователи
                    .Include(u => u.Роли)
                    .Include(u => u.Cтраны)
                    .FirstOrDefaultAsync(u => u.ID == userId);

                if (user == null)
                {
                    MessageBox.Show("Пользователь не найден.");
                    return;
                }

                Session.CurrentUser = user;

                IdBox.Text = user.ID.ToString();
                RoleBox.Text = user.Роли?.Name ?? user.RoleID.ToString();

                PhotoPathBox.Text = user.PhotoPath ?? "";
                FioBox.Text = user.FIO ?? "";

                EmailBox.Text = user.Email ?? "";
                PhoneBox.Text = user.PhoneNumber ?? "";
                BirthDatePicker.SelectedDate = user.DateOfBirth;

                DirectionBox.Text = user.Direction ?? "";
                EventBox.Text = user.Event ?? "";

                if (user.CountryID.HasValue)
                    CountryCombo.SelectedValue = user.CountryID.Value;
                else
                    CountryCombo.SelectedIndex = -1;

                LoadAvatar(user.PhotoPath);
            }
        }

        private void LoadAvatar(string photoPath)
        {
            AvatarImage.Source = null;

            var resolved = ResolvePhotoPath(photoPath);
            if (resolved == null) return;

            try
            {
                // OnLoad + Freeze: чтобы не держать файл открытым
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(resolved, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();

                AvatarImage.Source = bmp;
            }
            catch
            {
                AvatarImage.Source = null;
            }
        }

        private string ResolvePhotoPath(string photoPath)
        {
            photoPath = (photoPath ?? "").Trim();
            if (string.IsNullOrWhiteSpace(photoPath)) return null;

            // 1) Абсолютный путь
            if (Path.IsPathRooted(photoPath) && File.Exists(photoPath))
                return photoPath;

            // 2) Относительно папки приложения
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            var p1 = Path.Combine(baseDir, photoPath);
            if (File.Exists(p1)) return p1;

            // 3) Частый вариант: папка Images рядом с exe
            var p2 = Path.Combine(baseDir, "Images", photoPath);
            if (File.Exists(p2)) return p2;

            // 4) Частый вариант: Assets/Images
            var p3 = Path.Combine(baseDir, "Assets", photoPath);
            if (File.Exists(p3)) return p3;

            var p4 = Path.Combine(baseDir, "Assets", "Images", photoPath);
            if (File.Exists(p4)) return p4;

            return null;
        }

        private async void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!Session.IsAuth || Session.CurrentUser == null) return;

            var fio = (FioBox.Text ?? "").Trim();
            var email = (EmailBox.Text ?? "").Trim();
            var phone = (PhoneBox.Text ?? "").Trim();

            if (string.IsNullOrWhiteSpace(fio))
            {
                MessageBox.Show("ФИО не может быть пустым.");
                return;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Email не может быть пустым.");
                return;
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("Телефон не может быть пустым.");
                return;
            }

            SaveBtn.IsEnabled = false;

            try
            {
                using (var db = new InfoSecEventsEntities())
                {
                    var userId = Session.CurrentUser.ID;
                    var user = await db.Пользователи.FirstOrDefaultAsync(u => u.ID == userId);
                    if (user == null) return;

                    user.FIO = fio;
                    user.Email = email;
                    user.PhoneNumber = phone;

                    user.DateOfBirth = BirthDatePicker.SelectedDate;

                    if (CountryCombo.SelectedValue is int code)
                        user.CountryID = code;
                    else
                        user.CountryID = null;

                    user.Direction = string.IsNullOrWhiteSpace(DirectionBox.Text) ? null : DirectionBox.Text.Trim();
                    user.Event = string.IsNullOrWhiteSpace(EventBox.Text) ? null : EventBox.Text.Trim();
                    user.PhotoPath = string.IsNullOrWhiteSpace(PhotoPathBox.Text) ? null : PhotoPathBox.Text.Trim();

                    await db.SaveChangesAsync();
                }

                LoadAvatar(PhotoPathBox.Text);
                MessageBox.Show("Профиль сохранён.");
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message);
            }
            finally
            {
                SaveBtn.IsEnabled = true;
            }
        }

        private async void ChangePwdBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!Session.IsAuth || Session.CurrentUser == null) return;

            var oldPwd = OldPwdBox.Password ?? "";
            var newPwd = NewPwdBox.Password ?? "";

            if (string.IsNullOrWhiteSpace(newPwd) || newPwd.Length < 4)
            {
                MessageBox.Show("Новый пароль слишком короткий (минимум 4 символа).");
                return;
            }

            ChangePwdBtn.IsEnabled = false;

            try
            {
                using (var db = new InfoSecEventsEntities())
                {
                    var userId = Session.CurrentUser.ID;
                    var user = await db.Пользователи.FirstOrDefaultAsync(u => u.ID == userId);
                    if (user == null) return;

                    if ((user.Password ?? "") != oldPwd)
                    {
                        MessageBox.Show("Текущий пароль неверный.");
                        return;
                    }

                    user.Password = newPwd;
                    await db.SaveChangesAsync();
                }

                OldPwdBox.Password = "";
                NewPwdBox.Password = "";
                MessageBox.Show("Пароль изменён.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка смены пароля: " + ex.Message);
            }
            finally
            {
                ChangePwdBtn.IsEnabled = true;
            }
        }
    }
}
