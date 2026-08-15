using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JSL_SentinelPro.src.Models
{
    public class MemoryInfo : INotifyPropertyChanged
    {
        private ulong _totalBytes;
        private ulong _usedBytes;
        private ulong _freeBytes;
        private double _usagePercent;
        private double _totalGB;
        private double _usedGB;
        private double _freeGB;
        private string _status = "Normal";

        public ulong TotalBytes
        {
            get => _totalBytes;
            set
            {
                _totalBytes = value;
                Recalculate();
                OnPropertyChanged();
            }
        }

        public ulong UsedBytes
        {
            get => _usedBytes;
            set
            {
                _usedBytes = value;
                Recalculate();
                OnPropertyChanged();
            }
        }

        public ulong FreeBytes
        {
            get => _freeBytes;
            set
            {
                _freeBytes = value;
                Recalculate();
                OnPropertyChanged();
            }
        }

        public double UsagePercent
        {
            get => _usagePercent;
            set { _usagePercent = value; OnPropertyChanged(); }
        }

        public double TotalGB
        {
            get => _totalGB;
            set { _totalGB = value; OnPropertyChanged(); }
        }

        public double UsedGB
        {
            get => _usedGB;
            set { _usedGB = value; OnPropertyChanged(); }
        }

        public double FreeGB
        {
            get => _freeGB;
            set { _freeGB = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        private void Recalculate()
        {
            _totalGB = TotalBytes / (1024.0 * 1024.0 * 1024.0);
            _usedGB = UsedBytes / (1024.0 * 1024.0 * 1024.0);
            _freeGB = FreeBytes / (1024.0 * 1024.0 * 1024.0);
            _usagePercent = TotalBytes > 0 ? (UsedBytes * 100.0 / TotalBytes) : 0;
            _status = _usagePercent > 95 ? "Critico" : _usagePercent > 80 ? "Atencion" : "Normal";

            OnPropertyChanged(nameof(UsagePercent));
            OnPropertyChanged(nameof(TotalGB));
            OnPropertyChanged(nameof(UsedGB));
            OnPropertyChanged(nameof(FreeGB));
            OnPropertyChanged(nameof(Status));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
