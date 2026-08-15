using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JSL_SentinelPro.src.Models
{
    /// <summary>
    /// Item de limpieza del sistema.
    /// </summary>
    public class CleanupItem : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _path = string.Empty;
        private long _sizeBytes;
        private string _priority = "Medio";
        private bool _isSelected;
        private string _category = string.Empty;
        private double _sizeGB;
        private string _sizeDisplay = "0 KB";

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Path
        {
            get => _path;
            set { _path = value; OnPropertyChanged(); }
        }

        public long SizeBytes
        {
            get => _sizeBytes;
            set
            {
                _sizeBytes = value;
                _sizeGB = SizeBytes / (1024.0 * 1024.0 * 1024.0);
                UpdateSizeDisplay();
                OnPropertyChanged();
                OnPropertyChanged(nameof(SizeGB));
            }
        }

        public string Priority
        {
            get => _priority;
            set { _priority = value; OnPropertyChanged(); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(); }
        }

        public double SizeGB
        {
            get => _sizeGB;
            set { _sizeGB = value; OnPropertyChanged(); }
        }

        public string SizeDisplay
        {
            get => _sizeDisplay;
            set { _sizeDisplay = value; OnPropertyChanged(); }
        }

        private void UpdateSizeDisplay()
        {
            if (SizeBytes >= 1024L * 1024L * 1024L)
                _sizeDisplay = $"{SizeBytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
            else if (SizeBytes >= 1024L * 1024L)
                _sizeDisplay = $"{SizeBytes / (1024.0 * 1024.0):F2} MB";
            else
                _sizeDisplay = $"{SizeBytes / 1024.0:F2} KB";
            OnPropertyChanged(nameof(SizeDisplay));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
