using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace PrimalFearsUkr
{
    public partial class fMain : Form
    {
        string mPath = "";
        const string mGameName = "Primal Fears";
        const string mTextFileSubpath = "\\media\\HUD\\";
        const string mfontFileSubpath = "\\media\\MyGUI_Media\\";
        const string mTextFileName = "ukrainian.txt";
        const string mFontFileName = "core_font.xml";
        const string EmbeddedTextFile = "PrimalFearsUkr.LocalizationFiles.ukrainian.txt";
        const string EmbeddedfontFile = "PrimalFearsUkr.LocalizationFiles.core_font.xml";

        public fMain()
        {
            InitializeComponent();
            FindGamePath();
            mPath = PrepareGamePath(mPath);        
        }

        private void bLocalize_Click(object sender, EventArgs e)
        {
            if(string.IsNullOrWhiteSpace(mPath))
            {
                SetMessage("Помилка локалізації. Розташування теки гри не було знайдено у реєстрі Windows.");
                return;
            }

            RenamefontFile("", "_ORIGINAL");
            CreateFile(mPath + mfontFileSubpath, mFontFileName, EmbeddedfontFile);
            CreateFile(mPath + mTextFileSubpath, mTextFileName, EmbeddedTextFile);

            SetMessage("Гру успішно локалізовано.", false);
        }

        private void bRemove_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(mPath))
            {
                SetMessage("Помилка локалізації. Розташування теки гри не було знайдено у реєстрі Windows.");
                return;
            }

            RemoveFile(mPath + mTextFileSubpath, mTextFileName);
            RemoveFile(mPath + mfontFileSubpath, mFontFileName);
            RenamefontFile("_ORIGINAL", "");

            SetMessage("Гру успішно повернуто то оригінального стану.", false);
        }

        private void CreateFile(string Path, string FileName, string EmbeddedFilePath)
        {
            MemoryStream ms = GetEmbeddedFileStream(EmbeddedFilePath);
            RemoveFile(Path, FileName); 
            try
            {
                using (FileStream fs = File.Create(Path + FileName))
                {
                    Byte[] info = ms.GetBuffer();                    
                    fs.Write(info, 0, info.Length);
                }
            }
            catch (Exception ex)
            {
                string Message = ComposeErrorMessage(ex);
                SetMessage("Помилка створення файлу " + FileName + ".\r\n" + Message);
            }
        }

        private MemoryStream GetEmbeddedFileStream(string Path)
        {
            MemoryStream ms = null;
            try
            {
                ms = new MemoryStream();
                Assembly assembly = Assembly.GetExecutingAssembly();
                Stream s = assembly.GetManifestResourceStream(Path);
                s.CopyTo(ms);
                s.Close();
            }
            catch(Exception ex)
            {
                string Message = ComposeErrorMessage(ex);
                SetMessage("Помилка видобування файлу.\r\n" + Message);
            }
            return ms;
        }

        private void RemoveFile(string Path, string FileName)
        {
            try
            {
                if (File.Exists(Path + FileName))
                    File.Delete(Path + FileName);
            }
            catch(Exception ex)
            {
                string Message = ComposeErrorMessage(ex);
                SetMessage("Помилка вилучення файлу " + FileName + ".\r\n" + Message);
            }
        }

        private void RenamefontFile(string OldSufix, string NewSufix)
        {
            string OldName = mFontFileName;
            string NewName = mFontFileName;
            try
            {
                OldName = OldName.Replace(".xml", OldSufix + ".xml");
                NewName = OldName.Replace(OldSufix + ".xml", NewSufix + ".xml");
                if(File.Exists(mPath + mfontFileSubpath + OldName))
                    File.Move(mPath + mfontFileSubpath + OldName, mPath + mfontFileSubpath + NewName);
            }
            catch(Exception ex)
            {
                string Message = ComposeErrorMessage(ex);
                SetMessage("Помилка перемейнування файлу " + OldName + " у " + NewName + ".\r\n" + Message);
            }
        }

        private void bOpenGameFolder_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(mPath))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo { Arguments = mPath, FileName = "explorer.exe" };
                Process.Start(startInfo);
            }
            else
            {
                SetMessage("Тека гри не знайдена в реєстрі Windows. Тому її розташування відкрити неможливо.");
            }
        }

        private void FindGamePath()
        {
            try
            {
                string DisplayName;

                string registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
                RegistryKey key = Registry.LocalMachine.OpenSubKey(registryKey);
                if (key != null)
                {
                    foreach (RegistryKey subkey in key.GetSubKeyNames().Select(keyName => key.OpenSubKey(keyName)))
                    {
                        DisplayName = subkey.GetValue("DisplayName") as string;
                        if (DisplayName != null && DisplayName.Contains(mGameName))
                        {
                            mPath = subkey.GetValue("InstallLocation") as string;
                            return;
                        }
                    }
                    key.Close();
                }

                registryKey = @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
                key = Registry.LocalMachine.OpenSubKey(registryKey);
                if (key != null)
                {
                    foreach (RegistryKey subkey in key.GetSubKeyNames().Select(keyName => key.OpenSubKey(keyName)))
                    {
                        DisplayName = subkey.GetValue("DisplayName") as string;
                        if (DisplayName != null && DisplayName.Contains(mGameName))
                        {
                            mPath = subkey.GetValue("InstallLocation") as string;
                            return;
                        }
                    }
                    key.Close();
                }
            }
            catch(Exception ex)
            {
                string Message = ComposeErrorMessage(ex);
                SetMessage("Помилка при читанні реєстру Windows.\r\n" + Message);
            }
        }

        private string PrepareGamePath(string Path)
        {
            if (string.IsNullOrWhiteSpace(Path))
                return Path;

            Path = Path.TrimEnd(new char[] { '\\' });
            return Path;
        }

        private void SetMessage(string Message, bool IsError = true)
        {
            tbMessage.Text = Message;
            if (IsError)
                tbMessage.ForeColor = Color.Red;
            else
                tbMessage.ForeColor = Color.Green;
        }

        public string ComposeErrorMessage(Exception ex)
        {
            string Message = ex.Message.Replace("See the inner exception for details.", "");
            if (null != ex.InnerException)
            {
                Message += "\r\n";
                Message += ComposeErrorMessage(ex.InnerException);
            }
            return Message;
        }
    }
}
