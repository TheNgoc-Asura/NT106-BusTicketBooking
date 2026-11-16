using System;
using System.Windows.Forms;
using BusTicket.Client.Forms;

namespace BusTicket.Client;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new frmDangNhap());
    }    
}
