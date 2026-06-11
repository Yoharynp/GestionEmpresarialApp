using Microsoft.AspNetCore.Http;

namespace GestionEmpresarialApp.Helpers
{
    public static class SessionPermissions
    {
        public static bool Has(ISession session, string permission)
        {
            var raw = session.GetString("Permissions") ?? "";
            if (string.IsNullOrEmpty(raw)) return false;
            return raw.Split(',').Contains(permission);
        }

        public static bool IsAuthenticated(ISession session)
            => session.GetString("Username") != null;

        public static bool IsAdmin(ISession session)
            => session.GetString("Role") == "Administrador";
    }
}
