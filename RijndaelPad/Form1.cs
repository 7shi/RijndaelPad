using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace RijndaelPad
{
    public partial class Form1 : Form
    {
        private static MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();

        public static void Encrypt(Stream sout, byte[] data, byte[] key)
        {
            var sin = new MemoryStream(data);
            var rm = new RijndaelManaged();
            var enc = rm.CreateEncryptor(key, md5.ComputeHash(key));
            using (var cs = new CryptoStream(sout, enc, CryptoStreamMode.Write))
                cs.Write(data, 0, data.Length);
        }

        public static void Decrypt(Stream sout, byte[] data, byte[] key)
        {
            var sin = new MemoryStream(data);
            var rm = new RijndaelManaged();
            var buf = new byte[4096];
            var dec = rm.CreateDecryptor(key, md5.ComputeHash(key));
            using (var cs = new CryptoStream(sin, dec, CryptoStreamMode.Read))
            {
                int len;
                while ((len = cs.Read(buf, 0, buf.Length)) > 0)
                    sout.Write(buf, 0, len);
            }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private bool isChanged = false;
        private string filename = "";
        private byte[] key;

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            isChanged = true;
        }

        private byte[] getKey()
        {
            using (var f = new Form2())
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                    return md5.ComputeHash(Encoding.UTF8.GetBytes(f.Password));
                else
                    return null;
            }
        }

        private bool saveAs()
        {
            saveFileDialog1.FileName = filename;
            if (saveFileDialog1.ShowDialog(this) != DialogResult.OK) return false;

            filename = saveFileDialog1.FileName;
            key = null;
            return save();
        }

        private bool save()
        {
            if (filename == "") return saveAs();

            while (key == null)
            {
                key = getKey();
                if (key == null) return false;
            }

            try
            {
                using (var fs = new FileStream(filename, FileMode.Create))
                    Encrypt(fs, Encoding.UTF8.GetBytes(textBox1.Text), key);
                isChanged = false;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
        }

        private bool checkSave()
        {
            if (!isChanged) return true;

            var result = MessageBox.Show(this, "変更を保存しますか？", Text,
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Cancel) return false;
            if (result == DialogResult.No) return true;
            return save();
        }

        public void Open(string filename)
        {
            for (; ; )
            {
                var key = getKey();
                if (key == null) return;

                try
                {
                    var ms = new MemoryStream();
                    Decrypt(ms, File.ReadAllBytes(filename), key);
                    textBox1.Clear();
                    textBox1.Text = Encoding.UTF8.GetString(ms.ToArray());
                    isChanged = false;
                    this.filename = filename;
                    this.key = key;
                    return;
                }
                catch
                {
                    MessageBox.Show("パスワードが違います。", Text,
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (!e.Cancel && !checkSave()) e.Cancel = true;
        }

        private void mnuFileNew_Click(object sender, EventArgs e)
        {
            if (!checkSave()) return;

            isChanged = false;
            filename = null;
            key = null;
            textBox1.Clear();
        }

        private void mnuFileOpen_Click(object sender, EventArgs e)
        {
            if (checkSave() && openFileDialog1.ShowDialog(this) == DialogResult.OK)
                Open(openFileDialog1.FileName);
        }

        private void mnuFileSave_Click(object sender, EventArgs e)
        {
            save();
        }

        private void mnuFileSaveAs_Click(object sender, EventArgs e)
        {
            saveAs();
        }

        private void mnuFileExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void mnuPassword_Click(object sender, EventArgs e)
        {
            var sb = new StringBuilder();
            var r = new Random();
            for (int i = 0; i < 12; i++)
            {
                var v = r.Next(62);
                if (v < 26)
                    sb.Append((char)('A' + v));
                else if (v < 52)
                    sb.Append((char)('a' + (v - 26)));
                else
                    sb.Append((char)('0' + (v - 52)));
            }
            textBox1.SelectedText = sb.ToString() + Environment.NewLine;
        }

        private void textBox1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
        }

        private void textBox1_DragDrop(object sender, DragEventArgs e)
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null && files.Length > 0) Open(files[0]);
        }
    }
}
