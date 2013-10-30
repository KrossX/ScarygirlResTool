/*  Scarygirl Resolution Tool - Copyright (C) 2012 KrossX
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace ScarygirlTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        uint width = 0, height = 0;
        bool fullscreen = true, forceFullscreen = false;
        string gamefile = "";
		string gamebackup = "";

        public MainWindow()
        {
			InitializeComponent();
			VersionLabel.Content = "v0.8 r" + SvnRevision.SVN_REV;

            ReadReg();
            
            TextBox tWidth = FindName("TextWidth") as TextBox;
            TextBox tHeight = FindName("TextHeight") as TextBox;
            CheckBox tFullscreen = FindName("CheckFullscreen") as CheckBox;
            CheckBox tForceFullscreen = FindName("CheckForce") as CheckBox;

            tWidth.Text = width.ToString();
            tHeight.Text = height.ToString();

            tFullscreen.IsChecked = fullscreen;
            tForceFullscreen.IsChecked = forceFullscreen;

            DisableControls();

            if (FindFile() && VerifyFile()) EnableControls();
        }

        void SetStatus(string message, bool isBad)
        {
            Label statusLabel = FindName("StatusLabel") as Label;
            Rectangle statusBackground = FindName("StatusBackground") as Rectangle;
			PushButton patch = FindName("PatchButton") as PushButton;

			statusLabel.Content = message;

			if (isBad)
			{
				statusLabel.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x00, 0x00));
				statusBackground.Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0x5B, 0x30, 0x30));
				patch.Foreground = Brushes.Red;
				patch.BorderBrush = Brushes.Red;
				
			}
			else
			{
				statusLabel.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x41, 0x51, 0x31));
				statusBackground.Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0x9E, 0xB8, 0x9D));
				patch.Foreground = Brushes.Green;
				patch.BorderBrush = Brushes.Green;
			}
        }

        void DisableControls()
        {
			Grid settings = FindName("Settings") as Grid;
			PushButton push = FindName("PatchButton") as PushButton;

			push.IsEnabled = false;
			settings.IsEnabled = false;
			settings.Opacity = 0.5;
        }

        void EnableControls()
        {
			Grid settings = FindName("Settings") as Grid;
			PushButton push = FindName("PatchButton") as PushButton;

			push.IsEnabled = true;
			settings.IsEnabled = true;
			settings.Opacity = 1.0;
        }

        bool FindFile()
        {
            RegistryKey RegKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 202370");

			if (RegKey != null)
			{
				gamefile = RegKey.GetValue("InstallLocation", "Mew").ToString();
				if (gamefile.Equals("Mew")) return false;
				gamefile += @"\Game.exe";
			}
			else
			{
				RegKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 202370");
				if (RegKey != null)
				{
					gamefile = RegKey.GetValue("InstallLocation", "Mew").ToString();
					if (gamefile.Equals("Mew")) return false;
					gamefile += @"\Game.exe";
				}
				else
				{
					RegKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
					if (RegKey == null) return false;
					gamefile = RegKey.GetValue("SteamPath", "Mew").ToString();
					if (gamefile.Equals("Mew")) return false;

					gamefile.Replace('/','\\'); // Steam saves the path linux style
					gamefile += @"\steamapps\common\scarygirl\Game.exe";
				}
			}            

			if (File.Exists(gamefile))
			{
				Label filenameLabel = FindName("FilenameLabel") as Label;
				filenameLabel.Content = gamefile;

				if (gamefile.Length > 45) filenameLabel.FlowDirection = System.Windows.FlowDirection.RightToLeft;
				else filenameLabel.FlowDirection = System.Windows.FlowDirection.LeftToRight;

				gamebackup = gamefile.Substring(0, gamefile.Length - 3) + "backup";

				SetStatus("File found", false);
				return true;
			}
			else
			{
				SetStatus("File not found", true);
				return false;
			}
        }

        bool VerifyFile()
        {
			FileInfo fileinfo = new FileInfo(gamefile);

			if (fileinfo.Length != 2527744)
			{
				SetStatus("Wrong filesize", true);
				return false;
			}
			else
			{
				const UInt32 originalCRC = 0x8DAEDE7C;
				const UInt32 firtpartCRC = 0x56126683;

				bool makebackup = false;
				bool goodfile = false;
				bool original = false;

				UInt32 crc = new CRC().CheckCRC32(gamefile, fileinfo.Length);

				if (crc == originalCRC)
				{
					SetStatus("Original file", false);
					goodfile = original = true;
				}
				else
				{
					crc = new CRC().CheckCRC32(gamefile, 0x64BCF);

					if (crc == firtpartCRC)
					{
						SetStatus("Modified file", false);
						goodfile = true;
					}
					else
						SetStatus("Unknown file", true);
				}
				
				if (File.Exists(gamebackup))
				{
					FileInfo backupinfo = new FileInfo(gamebackup);

					if (backupinfo.Length != 2527744) makebackup = true;
					else
					{
						UInt32 backupCRC = new CRC().CheckCRC32(gamebackup, backupinfo.Length);
						if (backupCRC != originalCRC) makebackup = true;
					}
				}
				else
					makebackup = true;

				if (original && makebackup) 
					File.Copy(gamefile, gamebackup, true);

				if (makebackup)
				{
					Restore.Opacity = 0.5;
					Restore.IsEnabled = false;
				}
				else
				{
					Restore.Opacity = 1.0;
					Restore.IsEnabled = true;
				}
				
				return goodfile;
			}
        }


        void ReadReg()
        {
            string location = @"Software\Frozen Codebase\Scarygirl PC\Settings";

            RegistryKey RegKey = Registry.CurrentUser.OpenSubKey(location);
            if (RegKey != null) Registry.CurrentUser.CreateSubKey(location);
            RegKey = Registry.CurrentUser.OpenSubKey(location);

            width = uint.Parse(RegKey.GetValue("Width", 1).ToString());
            height = uint.Parse(RegKey.GetValue("Height", 1).ToString());
            fullscreen = RegKey.GetValue("Fullscreen", 0).ToString().Equals("0") ? false : true;

            RegKey.Close();
        }

        void SaveReg()
        {
            string location = @"Software\Frozen Codebase\Scarygirl PC\Settings";
            RegistryKey RegKey = Registry.CurrentUser.OpenSubKey(location, true);
            if (RegKey != null) Registry.CurrentUser.CreateSubKey(location);
            RegKey = Registry.CurrentUser.OpenSubKey(location, true);

            RegKey.SetValue("Width", width, RegistryValueKind.DWord);
            RegKey.SetValue("Height", height, RegistryValueKind.DWord);
            RegKey.SetValue("Fullscreen", fullscreen ? 1 : 0, RegistryValueKind.DWord);

			if (forceFullscreen && fullscreen)
			{
				const int value = unchecked((int)0x80000008); //Agh..
				RegKey.SetValue("Top", value, RegistryValueKind.DWord);
				RegKey.SetValue("Left", value, RegistryValueKind.DWord);
			}
			else
			{
				RegKey.SetValue("Top", 0, RegistryValueKind.DWord);
				RegKey.SetValue("Left", 0, RegistryValueKind.DWord);
			}

            RegKey.Close();
        }

        private void Text_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = (e.Key < Key.D0 || e.Key > Key.D9) && (e.Key < Key.NumPad0 || e.Key > Key.NumPad9);
            e.Handled &= (e.Key != Key.Tab);
        }

        private void Text_KeyUp(object sender, KeyEventArgs e)
        {
            TextBox pochy = sender as TextBox;
            
            if (pochy.Text.Length > 0)
            {
                uint number = uint.Parse(pochy.Text);

                if (number > 0xFFFF) pochy.Foreground = Brushes.Red;
                else pochy.Foreground = Brushes.Black;
            }
        }

        private void Text_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = e.Key == Key.Space;
        }

        private void PatchIt(object sender, MouseButtonEventArgs e)
        {
            TextBox tWidth = FindName("TextWidth") as TextBox;
            TextBox tHeight = FindName("TextHeight") as TextBox;
            CheckBox tFullscreen = FindName("CheckFullscreen") as CheckBox;
            CheckBox tForceFullscreen = FindName("CheckForce") as CheckBox;

            if(tWidth.Text.Length > 0) width = uint.Parse(tWidth.Text);
            if(tHeight.Text.Length > 0) height = uint.Parse(tHeight.Text);

            fullscreen = tFullscreen.IsChecked.Value;
            forceFullscreen = tForceFullscreen.IsChecked.Value;
            
            SaveReg();

            FileStream file = File.OpenWrite(gamefile);

            if (file != null)
            {
                byte[] bWidth = BitConverter.GetBytes(width);
                byte[] bHeight = BitConverter.GetBytes(height);

				// Don't try to save the registry keys
				file.Position = 0x064BD0; file.WriteByte(0xC3);

				// Force fullscreen
				file.Position = 0x064DFB;

				if (forceFullscreen && fullscreen) 
					file.WriteByte(0x58);
				else
					file.WriteByte(0x6C);
				
				// 640x480 override
                file.Position = 0x177376; file.Write(bWidth, 0, 2);
                file.Position = 0x17737E; file.Write(bHeight, 0, 2);

                // 800x600 override
                file.Position = 0x177388; file.Write(bWidth, 0, 2);
                file.Position = 0x177390; file.Write(bHeight, 0, 2);

                // 1024x1024? override
                file.Position = 0x177397; file.Write(bWidth, 0, 2);

                // 1280x1024 override
                file.Position = 0x1773A9; file.Write(bWidth, 0, 2);
                file.Position = 0x1773B1; file.Write(bHeight, 0, 2);

                // Magic jump JAE to JB 
                file.Position = 0x177357; file.WriteByte(0x82);
				                                
                file.Close();
                SetStatus("Patched! =)", false);
            }
            else
				SetStatus("Patch failed ='(", false);   
        }

        private void BrowseOnClick(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog Dialog = new OpenFileDialog();

            Dialog.Filter = "Executable File (*.exe)|*.exe";
            Dialog.Multiselect = false;

            if (Dialog.ShowDialog().Value)
            {
                gamefile = Dialog.FileName;

                Label filenameLabel = FindName("FilenameLabel") as Label;
                
                filenameLabel.Content = gamefile;

                if (gamefile.Length > 45) filenameLabel.FlowDirection = System.Windows.FlowDirection.RightToLeft;
                else filenameLabel.FlowDirection = System.Windows.FlowDirection.LeftToRight;

				gamebackup = gamefile.Substring(0, gamefile.Length - 3) + "backup";

				if (VerifyFile()) EnableControls();
				else DisableControls();
            }

        }

        private void Text_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox pochy = sender as TextBox;
            pochy.SelectAll();
        }

		private void eCheckFullscreen(object sender, RoutedEventArgs e)
		{
			CheckBox pochy = sender as CheckBox;

			if (pochy.IsChecked == true)
			{
				CheckForce.IsEnabled = true;
				CheckForce.Opacity = 1.0;
			}
			else
			{
				CheckForce.IsEnabled = false;
				CheckForce.Opacity = 0.5;
			}
		}

		private void eForceFullscreen(object sender, RoutedEventArgs e)
		{
			CheckBox pochy = sender as CheckBox;

			if (pochy.IsChecked == false)
			{
				CheckFullscreen.IsEnabled = true;
				CheckFullscreen.Opacity = 1.0;
			}
			else
			{
				CheckFullscreen.IsEnabled = false;
				CheckFullscreen.Opacity = 0.5;
			}
		}

		private void Restore_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			File.Copy(gamebackup, gamefile, true);
			VerifyFile();
		}

		

    }
}
