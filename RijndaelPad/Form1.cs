﻿using System;
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
        public Form1()
        {
            InitializeComponent();
        }

        private bool isChanged = false;
        private string filename = "";

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            isChanged = true;
        }

        public static void Encrypt(Stream sout, byte[] data, string password)
        {
            var md5 = new MD5CryptoServiceProvider();
            var key = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
            var iv = md5.ComputeHash(key);

            var sin = new MemoryStream(data);
            var rm = new RijndaelManaged();
            using (var cs = new CryptoStream(sout, rm.CreateEncryptor(key, iv), CryptoStreamMode.Write))
                cs.Write(data, 0, data.Length);
        }

        public static void Decrypt(Stream sout, byte[] data, string password)
        {
            var md5 = new MD5CryptoServiceProvider();
            var key = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
            var iv = md5.ComputeHash(key);

            var sin = new MemoryStream(data);
            var rm = new RijndaelManaged();
            var buf = new byte[4096];
            using (var cs = new CryptoStream(sin, rm.CreateDecryptor(key, iv), CryptoStreamMode.Read))
            {
                int len;
                while ((len = cs.Read(buf, 0, buf.Length)) > 0)
                    sout.Write(buf, 0, len);
            }
        }

        private string GetPassword()
        {
            using (var f = new Form2())
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                    return f.Password;
                else
                    return null;
            }
        }

        private bool saveAs()
        {
            saveFileDialog1.FileName = filename;
            if (saveFileDialog1.ShowDialog(this) != DialogResult.OK) return false;

            filename = saveFileDialog1.FileName;
            return save();
        }

        private bool save()
        {
            if (filename == "") return saveAs();

            var password = GetPassword();
            if (password == null) return false;

            try
            {
                using (var fs = new FileStream(filename, FileMode.Create))
                    Encrypt(fs, Encoding.UTF8.GetBytes(textBox1.Text), password);
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
            string password = GetPassword();
            if (password == null) return;

            try
            {
                var ms = new MemoryStream();
                Decrypt(ms, File.ReadAllBytes(filename), password);
                textBox1.Clear();
                textBox1.Text = Encoding.UTF8.GetString(ms.ToArray());
                isChanged = false;
                this.filename = filename;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
