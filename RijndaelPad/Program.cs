using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace RijndaelPad
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var f = new Form1();
            if (args.Length > 0)
            {
                f.Show();
                f.Open(args[0]);
            }
            Application.Run(f);
        }
    }
}
