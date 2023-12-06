using Microsoft.VisualBasic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace radar
{
    public partial class MainForm : Form
    {
#if false && DEBUG
        // Faster turnaround for debugging purposes.
        TimeSpan UpdateInterval { get; } = TimeSpan.FromSeconds(10);
#else
        TimeSpan UpdateInterval { get; } = TimeSpan.FromMinutes(5);
#endif
        public MainForm()
        {
            InitializeComponent();
            _downloadProgress = new ProgressBar
            {
                Location = lblNextTimeDownload.Location,
                Size = lblNextTimeDownload.Size,
                Dock = DockStyle.Top,
                Value = 0,
            };
        }
        readonly ProgressBar _downloadProgress;

        DateTime _nextDownloadTime = DateTime.Now;
        private async Task updateLabelAsync()
        {
            while (!Disposing)
            {
                TimeSpan countdown = _nextDownloadTime - DateTime.Now + TimeSpan.FromSeconds(0.99);
                if (countdown <= TimeSpan.FromSeconds(0.99))
                {
                    lblNextTimeDownload.Visible = false;
                    _downloadProgress.Visible = true;
                    await _radar.ExececuteAsync();
                    Debug.Assert(_radar.State == RadarState.Waiting, "Expecting Radar to reset its state.");
                    _nextDownloadTime = DateTime.Now + UpdateInterval;
                }
                else
                {
                    lblNextTimeDownload.Visible = true;
                    _downloadProgress.Visible = false;
                    lblNextTimeDownload.Text = "Next download in: " + countdown.ToString(@"mm\:ss");
                    // This resolution should prevent any erratic intervals
                    // as the 'new' second will get picked up right away.
                    await Task.Delay(100);
                }
            }
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            lblNextTimeDownload.Visible = false;
            Controls.Add(_downloadProgress);
            _radar.PropertyChanged += (sender, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(_radar.State):
                        // Update the title bar
                        if (!Disposing) BeginInvoke(() => Text = $"Radar - {_radar.State}");
                        break;
                    case nameof(_radar.Progress):
                        // Update the progress bar
                        if (!Disposing) BeginInvoke(() => _downloadProgress.Value = _radar.Progress);
                        break;
                }
            };
            // Start timer
            Task task = updateLabelAsync();
            Disposed += async (sender, e) =>
            {
                await task;
                task.Dispose();
            };
        }
        Radar _radar = new Radar(Path.Combine
        (
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Assembly.GetEntryAssembly().GetName().Name
        ));
    }
    public enum RadarState 
    { 
        Waiting, 
        Initializing,
        Downloading,
        DownloadCompleted, 
        ImageProcessing, 
        ImageProcessed, 
    }
    public class Radar : INotifyPropertyChanged
    {
        public Radar(string? inputFolder = null) =>
            Folder = inputFolder ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        public string Folder { get; }
        public string DefaultLink { get; }  = "https://ims.gov.il/sites/default/files/ims_data/map_images/IMSRadar4GIS/IMSRadar4GIS_";
        public event PropertyChangedEventHandler? PropertyChanged;

        public RadarState State
        {
            get => _state;
            set
            {
                if (!Equals(_state, value))
                {
                    _state = value;
                    OnPropertyChanged();
                }
            }
        }
        RadarState _state = default;
        public int Progress
        {
            get => _progress;
            set
            {
                if (!Equals(_progress, value))
                {
                    _progress = value;
                    OnPropertyChanged();
                }
            }
        }
        int _progress = default;

        private void OnPropertyChanged([CallerMemberName]string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public async Task ExececuteAsync()
        {
            State = RadarState.Initializing;
            PrepareLinks();
            await DownloadImagesAsync();
            await ProcessDownloadedImagesAsync();
            State = RadarState.Waiting;
        }
        public List<string> Links { get; } = new List<string>();
        public Dictionary<string, string> LinksAndFileNames { get; } = new Dictionary<string, string>();
        public void PrepareLinks()
        {
            LinksAndFileNames.Clear();
            Links.Clear();

            GenerateRadarLinks();

            // Exclude links for files that already exist in the folder
            foreach (var existing in Directory.GetFiles(Folder, "*.png").Select(Path.GetFileNameWithoutExtension))
            {
                if(Links.FirstOrDefault(_=>_.Contains(existing)) is string link)
                {
                    Links.Remove(link);
                }
            }
        }

        private void GenerateRadarLinks()
        {
            DateTime current = RoundDown(DateTime.Now, 1);

            if (!Directory.Exists(Folder + "\\Dates"))
            {
                Directory.CreateDirectory(Folder + "\\Dates");
            }

            using (StreamWriter w = new StreamWriter(Folder + "\\Dates\\" + "dates.txt"))
            using (StreamWriter ww = new StreamWriter(Folder + "\\Dates\\" + "datesTime.txt"))
            {
                for (int i = 0; i < 200; i++)
                {
                    var date = current.ToString("yyyyMMddHHmm");
                    ww.WriteLine(current.ToString());
                    w.WriteLine(date);
                    var link = DefaultLink + date + "_0.png";
                    Links.Add(link);
                    LinksAndFileNames.Add(link, current.ToString("yyyy_MM_dd_HH_mm"));
                    current = current.AddMinutes(-1);
                }
            }
        }
        private DateTime RoundDown(DateTime dt, int NearestMinuteInterval) =>
            new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute / NearestMinuteInterval * NearestMinuteInterval, 0);

        private async Task DownloadImagesAsync()
        {
            Progress = 0;
            using (HttpClient client = new HttpClient())
            {
                int totalFiles = Links.Count;
                int completedFiles = 0;
                if (Links.Any())
                {
                    // Signal that the actual download has started
                    State = RadarState.Downloading;

                    foreach (var link in Links)
                    {
                        var fileName = LinksAndFileNames[link];
                        var filePath = Path.Combine(Folder, fileName + ".png");

                        // Mock Download
                        await Task.Delay(TimeSpan.FromMilliseconds(10));

                        completedFiles++;
                        Progress = (int)((double)completedFiles / totalFiles * 100);
                    }
                }
                // Notify completion
                State = RadarState.DownloadCompleted;
            }
            // Ensure Progress has gone ti the end, and
            // allow time to view the result.
            Progress = 100;
            await Task.Delay(TimeSpan.FromSeconds(1.5));
        }

        private async Task ProcessDownloadedImagesAsync()
        {
            Progress = 0;
            await Task.Run(async () =>
            {
                for (int i = 0; i < Links.Count; i++) 
                {
                    var link = Links[i];
                    var fileName = LinksAndFileNames[link];
                    var filePath = Path.Combine(Folder, fileName + ".png");

                    // Check if the file exists before processing
                    if (File.Exists(filePath))
                    {
                        State = RadarState.ImageProcessing;

                        // Process the downloaded image
                        // RadarImagesConvertor convertor = new RadarImagesConvertor(filePath, Folder);

                        // Signal that the image has been processed
                        State = RadarState.ImageProcessed;
                    }
                    else
                    {
#if DEBUG
                        // No files exist in this mock setting, so 
                        // exercise the state to make sure things work,
                        State = RadarState.ImageProcessing;
                        // Mock the downloaded image
                        await Task.Delay(50);

                        // Signal that the image has been processed
                        State = RadarState.ImageProcessed;
                        await Task.Delay(50);
#endif


                        // Handle the case where the file does not exist
                        Console.WriteLine($"File not found: {filePath}");
                    }
                    Progress = (int)(i/(Links.Count * 0.01));
                }
            });
            // Ensure Progress has gone ti the end, and
            // allow time to view the result.
            Progress = 100;
            await Task.Delay(TimeSpan.FromSeconds(1.5));
        }
    }
}
