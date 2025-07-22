using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AlbumViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool fullscreen_;
        private double originalWidth_;
        private double originalHeight_;

        private static string rootDir_;
        private Dictionary<String, String[]> albumsDirs_;
        private Dictionary<String, List<String>> albumsSubdirs_;

        private string curAlbumId_;
        private string curDir_;
        private int curSubDirIndex_;
        private int nbSubDirectories_;

        private string curFile_;
        private int curFileIndex_;
        private int nbFiles_;
        private string[] filenames_;

        private readonly List<Button> buttonsAlbums_;

        private static string logEventsPath_;
        private static string applicationPath_;

        private readonly DispatcherTimer timer_;
        private bool slideshow_;

        private BackgroundWorker workerInit_;

        private int indexAlbum_;
        private int nbAlbums_;
        private int indexSubAlbum_;
        private int nbSubAlbums_;

        public MainWindow()
        {
            InitializeComponent();

            this.Title += " " + Assembly.GetEntryAssembly().GetName().Version.ToString(3);

            fullscreen_ = false;
            nbSubDirectories_ = 0;
            curSubDirIndex_ = 0;
            curFile_ = "";
            curFileIndex_ = 0;
            nbFiles_ = 0;
            curDir_ = "";

            buttonsAlbums_ = new List<Button>
            {
                buttonAlbum11,
                buttonAlbum12,
                buttonAlbum13,
                buttonAlbum14,
                buttonAlbum21,
                buttonAlbum22,
                buttonAlbum23,
                buttonAlbum24,
                buttonAlbum31,
                buttonAlbum32,
                buttonAlbum33
            };

            // slideshow timer
            slideshow_ = true;
            timer_ = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(5 * 1000) // default
            };
            timer_.Tick += Timer__Tick;

            // events callbacks

            this.PreviewKeyDown += MainWindow_PreviewKeyDown; // to handle space and return key events

            this.MouseWheel += MainWindow_MouseWheel;            
            this.MouseDown += MainWindow_MouseDown;
            this.MouseDoubleClick += MainWindow_MouseDoubleClick;

            //imageBox.MouseDown += MainWindow_MouseDown;
            //imageBox.MouseDoubleClick += onMouseDoubleClick;

            this.KeyDown += MainWindow_KeyDown;
        }

        #region Callbacks

        private void buttonQuit_Click(object sender, RoutedEventArgs e)
        {
            if (workerInit_ != null && workerInit_.IsBusy)
                workerInit_.CancelAsync();

            this.Close();
        }

        private void MainWindow1_Loaded(object sender, RoutedEventArgs e)
        {
            originalWidth_ = this.Width;
            originalHeight_ = this.Height;

            progressBar1.Width = originalWidth_;
            progressBar1.Visibility = Visibility.Visible;

            progressBar2.Width = originalWidth_;
            progressBar2.Visibility = Visibility.Visible;

            workerInit_ = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            workerInit_.DoWork += worker_DoWork;
            workerInit_.ProgressChanged += worker_ProgressChanged;
            workerInit_.RunWorkerCompleted += Worker_RunWorkerCompleted;

            workerInit_.RunWorkerAsync();
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            // check if required config file is present
            string pathConfig = "AVConfig.txt";
            if (!File.Exists(pathConfig))
            {
                MessageBox.Show("Config file " + pathConfig + " not found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }

            // read config file

            rootDir_ = "";
            albumsDirs_ = new Dictionary<String, String[]>();
            albumsSubdirs_ = new Dictionary<string, List<string>>();
            readConfigFile(pathConfig);

            if (!Directory.Exists(rootDir_))
            {
                MessageBox.Show("Directory \"" + rootDir_ + "\" does not exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }

            // create log file

            logEventsPath_ = "AVLog.txt";
            if (File.Exists(logEventsPath_))
                File.Delete(logEventsPath_);

            LogEvent("Starting Album Viewer...");

            // list all album directories
            indexAlbum_ = 0;
            nbAlbums_ = albumsDirs_.Count;
            foreach (KeyValuePair<String, String[]> keyValue in albumsDirs_)
            {
                String id = keyValue.Key;
                String[] dirs = keyValue.Value;

                List<String> idAlbumDirs = new List<string>();

                foreach (string dir in dirs)
                {
                    string searchDir = System.IO.Path.Combine(rootDir_, dir);

                    // add directory of not empty
                    if (Directory.GetFiles(searchDir).Length > 0)
                        idAlbumDirs.Add(searchDir);

                    // list all subdirectories
                    indexSubAlbum_ = 0;
                    string[] subdirs = Directory.GetDirectories(searchDir, "*", System.IO.SearchOption.AllDirectories);
                    if (subdirs == null || subdirs.Length == 0)
                    {
                        //MessageBox.Show("No subdirectories found in " + searchDir);
                        continue;
                    }
                    nbSubAlbums_ = subdirs.Length;
                    //subDirectories_ = new List<string>(subdirs);

                    // add non-empty subdirectories
                    //List<string> emptySubDirs = new List<string>();
                    foreach (string subdir in subdirs)
                    {
                        if (Directory.GetFiles(subdir).Length > 0)
                            idAlbumDirs.Add(subdir);

                        // update progress bar
                        //Thread.Sleep(2000);
                        indexSubAlbum_++;
                        (sender as BackgroundWorker).ReportProgress(0/*dummy*/);
                    }
                    //foreach (string subdir in emptySubDirs)
                    //    subDirectories_.Remove(subdir);

                    LogEvent(nbSubAlbums_ + " directories found in \"" + searchDir + "\"");
                    //foreach (string subdir in subDirectories_)
                    //    LogEvent("   " + subdir);
                }

                // randomize subdirectories order
                Tools.Shuffle(idAlbumDirs);

                // add subdirectories list
                albumsSubdirs_.Add(id, idAlbumDirs);

                // update progress bar
                //Thread.Sleep(2000);
                indexAlbum_++;
                indexSubAlbum_ = 0;
                (sender as BackgroundWorker).ReportProgress(0/*dummy*/);
            }

            applicationPath_ = AppDomain.CurrentDomain.BaseDirectory;
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int progress = (int)Math.Round(100 * (indexAlbum_ / (float)nbAlbums_));
            progressBar1.Value = progress;

            int progressSubAlbums = (int)Math.Round(100 * (indexSubAlbum_ / (float)nbSubAlbums_));
            progressBar2.Value = progressSubAlbums;
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // write album id in button text
            int index = 0;
            foreach (KeyValuePair<string, List<string>> keyValue in albumsSubdirs_)
            {
                if (index >= buttonsAlbums_.Count)
                    break;  // incoherent

                Button buttonAlbum = buttonsAlbums_[index];
                buttonAlbum.Content = keyValue.Key;
                buttonAlbum.IsEnabled = true;
                index++;
            }

            progressBar1.Visibility = Visibility.Hidden;
            progressBar2.Visibility = Visibility.Hidden;
        }

        private void buttonAlbumId_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            string albumId = button.Content as string;

            if (String.IsNullOrEmpty(albumId.Trim()))
                return;

            if (!albumsSubdirs_.ContainsKey(albumId))
                return;

            // update variables
            curFileIndex_ = 0;
            curAlbumId_ = albumId;
            nbSubDirectories_ = albumsSubdirs_[albumId].Count;

            updateAlbum();
        }

        private void buttonQuit_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        private void MainWindow_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0) // up
                prevImage();
            if (e.Delta < 0) // down
                nextImage();
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState != MouseButtonState.Pressed)
                return;

            switch (e.ChangedButton)
            {
                case MouseButton.Left:     // go to next album
                    nextAlbum();
                    break;
                case MouseButton.Right:    // go back to interface
                    toggleFullscreen();
                    break;
                case MouseButton.Middle:   // pause image
                    pauseImage();
                    //Close();
                    break;
            }
        }

        private void MainWindow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState != MouseButtonState.Pressed)
                return;

            switch (e.ChangedButton)
            {
                case MouseButton.Left:     // go to next 2nd album
                    nextAlbum();
                    break;
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Right:
                    nextAlbum();
                    break;

                case Key.PageDown:
                    nextImage();
                    break;

                case Key.PageUp:
                    prevImage();
                    break;

                case Key.Home:
                    firstImage();
                    break;

                case Key.End:
                    lastImage();
                    break;

                case Key.Delete:   // delete current image
                    deleteImage();
                    break;

                case Key.Escape:   // go back to interface
                    toggleFullscreen();
                    break;

                case Key.P:        // pause image
                    pauseImage();
                    break;

                case Key.Space:    // close all
                case Key.B:
                    Close();
                    break;
            }
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                MainWindow_KeyDown(sender, e);
            }
        }

        private void MainWindow1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // stop slideshow
            timer_?.Stop();
        }

        private void Timer__Tick(object sender, EventArgs e)
        {
            nextImage();
        }

        #endregion

        private void enableFullscreen(bool fullscreen)
        {
            if (fullscreen)
            {
                // hide buttons
                foreach (Button buttonAlbum in buttonsAlbums_)
                    buttonAlbum.Visibility = Visibility.Hidden;
                buttonQuit.Visibility = Visibility.Hidden;

                imageBox.Visibility = Visibility.Visible;

                //this.Visibility = Visibility.Collapsed;
                this.WindowStyle = WindowStyle.None;
                this.ResizeMode = ResizeMode.NoResize;
                this.WindowState = WindowState.Maximized;
                //this.Topmost = true;
                //this.Visibility = Visibility.Visible;

                // update grid dimensions
                gridButtons.Width = this.Width;
                gridButtons.Height = this.Height;

                // start slideshow
                Mouse.OverrideCursor = Cursors.None; // hide cursor
                slideshow_ = true;
                timer_?.Start();
            }
            else
            {
                // stop slideshow
                timer_?.Stop();
                slideshow_ = false;
                Mouse.OverrideCursor = null; // show cursor

                this.WindowState = WindowState.Normal;
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                this.ResizeMode = ResizeMode.CanResize;
                this.Topmost = false;

                curSubDirIndex_++;

                imageBox.Visibility = Visibility.Hidden;
                imageBox.Source = null;
                mediaBox.Visibility = Visibility.Hidden;
                mediaBox.Source = null;

                // update grid dimensions
                gridButtons.Width = this.Width;
                gridButtons.Height = this.Height - SystemParameters.WindowCaptionHeight;

                // show buttons
                foreach (Button buttonAlbum in buttonsAlbums_)
                    buttonAlbum.Visibility = Visibility.Visible;
                buttonQuit.Visibility = Visibility.Visible;
            }

            fullscreen_ = fullscreen;
        }

        private void toggleFullscreen()
        {
            enableFullscreen(!fullscreen_);
        }

        #region Browse functions

        private void nextAlbum()
        {
            // stop slideshow
            timer_?.Stop();

            curFileIndex_ = 0;
            curSubDirIndex_++;

            updateAlbum();
        }

        private void nextImage()
        {
            // restart slideshow
            timer_?.Stop();

            curFileIndex_ = Math.Min(curFileIndex_ + 1, nbFiles_ - 1);
            updateImage();

            timer_?.Start();
        }

        private void prevImage()
        {
            // restart slideshow
            timer_?.Stop();

            curFileIndex_ = Math.Max(0, curFileIndex_ - 1);
            updateImage();

            timer_?.Start();
        }

        private void firstImage()
        {
            // restart slideshow
            timer_?.Stop();

            curFileIndex_ = 0;
            updateImage();

            timer_?.Start();
        }

        private void lastImage()
        {
            // stop slideshow
            timer_?.Stop();

            curFileIndex_ = nbFiles_ - 1;
            updateImage();
        }

        private void deleteImage()
        {
            // stop slideshow
            timer_?.Stop();

            try
            {

                if (String.IsNullOrEmpty(curFile_))
                    return;

                if (!File.Exists(curFile_))
                    return;

                // confirmation dialog
                MessageBoxResult result = MessageBox.Show("Are you sure to delete image \"" + curFile_ + "\"?",
                    "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                if (result == MessageBoxResult.OK)
                {
                    if (imageBox.Source != null)
                        imageBox.Source = null;
                    if (mediaBox.Source != null)
                        mediaBox.Source = null;

                    // delete current file
                    FileSystem.DeleteFile(curFile_, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);

                    // update current album file list
                    List<string> fileList = new List<string>(filenames_);
                    fileList.Remove(curFile_);
                    filenames_ = fileList.ToArray();
                    nbFiles_ = filenames_.Length;

                    // update current file
                    curFileIndex_ = Math.Min(curFileIndex_, nbFiles_ - 1);
                    updateImage();

                    // restart slideshow
                    timer_?.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void pauseImage()
        {
            if (slideshow_)
                timer_?.Stop();
            else
                timer_?.Start();

            slideshow_ = !slideshow_;
        }

        private void updateAlbum()
        {
            int curSubDirIndex = curSubDirIndex_;
            if (nbSubDirectories_ > 0)
                curSubDirIndex = curSubDirIndex_ % nbSubDirectories_;
            curDir_ = albumsSubdirs_[curAlbumId_][curSubDirIndex];

            // filter with image extensions
            string extensions = @"jpg|jpe|jpeg|png|bmp|gif|tif|tiff";
            string searchPattern = @"^.+\.(" + extensions + ")$";
            filenames_ = (Directory.GetFiles(curDir_).Where(file => Regex.IsMatch(file.ToLower(), searchPattern))).ToArray();
            nbFiles_ = filenames_.Length;
            if (nbFiles_ == 0)
            {
                MessageBox.Show("No files found in directory " + curDir_);
                return;
            }

            LogEvent("Opening album \"" + curDir_ + "\"...");

            enableFullscreen(true);
            updateImage();
        }

        private void updateImage()
        {
            if (filenames_ == null || filenames_.Length == 0)
            {
                MessageBox.Show("No files found in directory " + curDir_);
                return;
            }

            /*FileStream fs = null;
            try
            {
                fs = new FileStream(filenames_[curFileIndex_], FileMode.Open, FileAccess.Read);
                imageBox.Image = Image.FromStream(fs);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                fs.Close();
            }
            */

            try
            {
                //if (imageBox.Source != null)
                //    imageBox.Source.Dispose();

                curFile_ = filenames_[curFileIndex_];
                string curFilePath = System.IO.Path.Combine(applicationPath_, curFile_);
                Uri uriSource = new Uri(curFilePath, UriKind.RelativeOrAbsolute);

                // display media element if animated image
                string extension = System.IO.Path.GetExtension(curFilePath).ToLower();
                if (extension == ".gif")
                {
                    imageBox.Visibility = Visibility.Hidden;
                    mediaBox.Visibility = Visibility.Visible;

                    mediaBox.Source = uriSource;

                    // loop animation
                    mediaBox.MediaEnded += MediaBox_MediaEnded;
                }
                else
                {
                    // stop animation
                    mediaBox.Stop();
                    mediaBox.MediaEnded -= MediaBox_MediaEnded;

                    imageBox.Visibility = Visibility.Visible;
                    mediaBox.Visibility = Visibility.Hidden;

                    imageBox.Source = new BitmapImage(uriSource);
                    //imageBox.Image = Image.FromFile(curFile_).Clone() as Image;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void MediaBox_MediaEnded(object sender, RoutedEventArgs e)
        {
            mediaBox.Position = new TimeSpan(0, 0, 1);
            mediaBox.Play();
        }

        #endregion

        private void readConfigFile(string filename)
        {
            string line;
            int indexSection = -1;

            // Read the file and display it line by line.
            StreamReader file = new StreamReader(filename, Encoding.UTF8);
            while ((line = file.ReadLine()) != null)
            {
                // section

                bool isSection = false;
                switch (line)
                {
                    case "[CONFIG]": indexSection = 0; isSection = true; break;
                    case "[ALBUMS]": indexSection = 1; isSection = true; break;
                }

                if (isSection)
                    continue;

                if (String.IsNullOrEmpty(line))
                    continue;

                // key / value

                String[] keyValue = line.Split('=');
                if (keyValue.Length < 2)
                    continue;

                String key = keyValue[0];
                String value = keyValue[1];

                // handle multiple '=' in value
                if (keyValue.Length > 2)
                    for (int i = 2; i < keyValue.Length; i++)
                        value += "=" + keyValue[i];

                if (indexSection == 0)  // general config
                {
                    switch (key)
                    {
                        case "root_dir":
                            rootDir_ = value;
                            break;
                        case "slideshow_interval":
                            if (timer_ != null)
                                timer_.Interval = TimeSpan.FromMilliseconds(Convert.ToInt32(value));
                            break;
                    }
                }
                else if (indexSection == 1) // get album directories
                {
                    String[] dirs = value.Split(';');
                    foreach (String subdir in dirs)
                    {
                        // check if subdirectory exists
                        string subdirPath = System.IO.Path.Combine(rootDir_, subdir);
                        if (!Directory.Exists(subdirPath))
                        {
                            MessageBox.Show("Subdirectory " + subdirPath + " does not exist.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            List<String> dirsList = new List<String>(dirs);
                            dirsList.Remove(subdir);
                            dirs = dirsList.ToArray();
                            continue;
                        }
                    }

                    if (dirs.Length == 0)
                        MessageBox.Show("Button \"" + key + "\" not added.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    else
                        albumsDirs_.Add(key, dirs);
                }
            }

            file.Close();
        }

        private void LogEvent(string text)
        {
            using (StreamWriter file = new StreamWriter(@logEventsPath_, true))
            {
                file.WriteLine(DateTime.Now.ToString() + ": " + text);
                //file.Write(text);
            }
        }
    }
}
