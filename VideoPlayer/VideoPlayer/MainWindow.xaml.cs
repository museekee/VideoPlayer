using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MessageBox = System.Windows.Forms.MessageBox;

namespace VideoPlayer
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private string SelectedPath;

        private List<string> FileList = new List<string>();
        private int VidIdx = 0;
        private string[] VideoFileFormats = { ".3gp", ".avi", ".mp4", ".webm", ".mkv", ".mov", ".wmv", ".m4v", ".3g2" };

        private bool isSliding = false;
        private bool isPause = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Space:
                    PlayToggle();
                    break;
                case Key.K:
                    PlayToggle();
                    break;
                case Key.Left:
                    AddVidPosition(-5000);
                    break;
                case Key.Right:
                    AddVidPosition(5000);
                    break;
                case Key.J:
                    AddVidPosition(-10000);
                    break;
                case Key.L:
                    AddVidPosition(10000);
                    break;
            }
        }

        private void InputFile_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SelectedPath = fbd.SelectedPath;
                FileList.Clear();
                FilesPanel.Children.Clear();
                GetFiles(new DirectoryInfo(SelectedPath));
                for (int i = 0; i < FileList.Count; i++)
                {
                    TextBlock fl = new TextBlock
                    {
                        Text = FileList[i].Replace(SelectedPath, ""),
                        Name = $"File_{i}",
                        Foreground = Brushes.White
                    };
                    if (i == 0)
                    {
                        fl.FontSize = 20;
                        fl.FontWeight = FontWeights.Bold;
                    }
                    fl.MouseLeftButtonDown += Fl_MouseLeftButtonDown;
                    FilesPanel.Children.Add(fl);
                }
                VidIdx = 0;
                Debug.WriteLine(string.Join(" | ", FileList));
                PlayVideo();
                VidPlayer.Play();
                PlayToggleBtn.Focus();
            }
        }

        private void Fl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TextBlock thisFl = (TextBlock)sender;
            int thisIdx = int.Parse(thisFl.Name.Replace("File_", ""));
            VidIdx = thisIdx;
            PlayVideo();
            foreach (TextBlock fl in FilesPanel.Children)
            {
                fl.FontSize = SystemFonts.MessageFontSize;
                fl.FontWeight = FontWeights.Normal;
            }
            thisFl.FontSize = 20;
            thisFl.FontWeight = FontWeights.Bold;
            Debug.WriteLine(thisIdx);
        }

        private void VidPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            VidIdx++;
            if (VidIdx >= FileList.Count)
            {
                MessageBox.Show("모든 영상이 재생되었습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                PlayVideo();
            }
        }

        private void AddVidPosition(int ms)
        {
            VidPlayer.Pause();
            isSliding = true;
            VidPlayer.Position += TimeSpan.FromMilliseconds(ms);
            isSliding = false;
            VidPlayer.Play();
        }

        private void PlayVideo()
        {
            if (FileList.Count <= 0)
            {
                MessageBox.Show("감지된 영상이 없습니다", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            VidPlayer.Source = new Uri(FileList[VidIdx]);
            DispatcherTimer dispatcherTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };

            dispatcherTimer.Tick += dispatcherTimer_Tick;

            dispatcherTimer.Start();
            VidTitle.Text = VidPlayer.Source.LocalPath;
            RemainingFiles.Text = $"{VidIdx+1} / {FileList.Count}";
            if (VidIdx > 0)
            {
                TextBlock oldFl = (TextBlock)FilesPanel.Children[VidIdx-1];
                oldFl.FontSize = SystemFonts.MessageFontSize;
                oldFl.FontWeight = FontWeights.Normal;
                TextBlock nowFl = (TextBlock)FilesPanel.Children[VidIdx];
                nowFl.FontSize = 20;
                nowFl.FontWeight = FontWeights.Bold;
            }
        }

        private void VidPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            TimeSpan time = TimeSpan.FromMilliseconds(VidPlayer.NaturalDuration.TimeSpan.TotalMilliseconds);
            PlayTime.Text = string.Format("{0:D2}:{1:D2}:{2:D2}",
                                time.Hours,
                                time.Minutes,
                                time.Seconds);
            TimeSlider.Maximum = VidPlayer.NaturalDuration.TimeSpan.TotalMilliseconds;
        }

        private void GetFiles(DirectoryInfo directory)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(directory.FullName);
            foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
            {
                GetFiles(dir);
            }
            foreach(FileInfo file in directoryInfo.GetFiles())
            {
                if (file.Exists)
                {
                    if (VideoFileFormats.Contains(Path.GetExtension(file.Name).ToLower()))
                        FileList.Add(file.FullName);
                }
            }
        }

        private void PlayToggleBtn_Click(object sender, RoutedEventArgs e)
        {
            PlayToggle();
        }

        private void PlayToggle()
        {
            if (VidPlayer.CanPause && !isPause)
            {
                VidPlayer.Pause();
                isPause = true;
                PlayToggleBtn.Content = "▶";
            }
            else if (isPause)
            {
                VidPlayer.Play();
                isPause = false;
                PlayToggleBtn.Content = "II";
            }
        }

        private void MediaTimeline_CurrentTimeInvalidated(object sender, EventArgs e)
        {
            TimeSlider.Value = VidPlayer.Position.TotalMilliseconds;
            Debug.WriteLine(VidPlayer.Position.TotalSeconds);
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (isSliding)
            {
                return;
            }

            if (VidPlayer.Source == null || !VidPlayer.NaturalDuration.HasTimeSpan)
            {
                return;
            }

            TimeSlider.Value = VidPlayer.Position.TotalMilliseconds;
        }

        private void TimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VidPlayer.Source == null)
            {
                return;
            }

            CurrentTime.Text = VidPlayer.Position.ToString(@"hh\:mm\:ss");
        }

        private void TimeSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            isSliding = true;
            VidPlayer.Pause();
        }

        private void TimeSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            VidPlayer.Position = TimeSpan.FromMilliseconds(TimeSlider.Value);

            VidPlayer.Play();

            isSliding = false;
        }
        private void FullScreenBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ResizeMode != ResizeMode.NoResize)
            {
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                ResizeMode = ResizeMode.NoResize;
                ShowInTaskbar = false;
                // RightPanel.Visibility = Visibility.Hidden;
                // VidPlayer.SetValue(Grid.ColumnSpanProperty, 2);
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                ResizeMode = ResizeMode.CanResize;
                ShowInTaskbar = true;
                // RightPanel.Visibility = Visibility.Visible;
                // VidPlayer.SetValue(Grid.ColumnSpanProperty, 1);
            }
        }
    }
}
