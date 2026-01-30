namespace EventsManager
{
    public static class Session
    {
        public static Пользователи CurrentUser { get; set; }

        public static bool IsAuth => CurrentUser != null;

        public static string RoleName => CurrentUser?.Роли?.Name ?? "";
    }
}
