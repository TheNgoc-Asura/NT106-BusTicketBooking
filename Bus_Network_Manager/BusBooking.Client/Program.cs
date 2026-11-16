using BusBooking.Client.Forms.Admin;
using BusBooking.Client.Forms.Auth;
using BusBooking.Client.Forms.Customer;
using BusBooking.Client.Services;
using BusBooking.Shared.Enums;

namespace BusBooking.Client;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        ApplicationConfiguration.Initialize();

        while (true)
        {
            using var loginForm = new frmLogin();
            if (loginForm.ShowDialog() != DialogResult.OK || SessionManager.CurrentUser == null)
                break;

            Form mainForm = SessionManager.CurrentUser.Role == UserRoles.Customer
                ? new frmMainCustomer()
                : new frmMainAdmin();

            bool logoutRequested = false;
            mainForm.FormClosed += (_, _) =>
            {
                logoutRequested = SessionManager.CurrentUser == null;
            };

            Application.Run(mainForm);
            mainForm.Dispose();

            if (!logoutRequested)
                break;
        }
    }
}
