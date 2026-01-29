namespace EventsManager
{
    public static class Session
    {
        public static Пользователи CurrentUser { get; set; }

        // Авторизация = есть текущий пользователь
        public static bool IsAuth => CurrentUser != null;

        // Имя роли (если пользователь загружен с Include(u => u.Роли))
        public static string RoleName => CurrentUser?.Роли?.Name ?? "";
    }
}
