using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JSL_SentinelPro.src.Models
{
    /// <summary>
    /// Ajuste de optimizacion.
    /// </summary>
    public class OptimizationSetting : INotifyPropertyChanged
    {
        private bool _isRecommended;
        private bool _isApplied;

        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medio";
        public bool IsRecommended
        {
            get => _isRecommended;
            set { _isRecommended = value; OnPropertyChanged(); }
        }
        public bool IsApplied
        {
            get => _isApplied;
            set { _isApplied = value; OnPropertyChanged(); }
        }
        public double EstimatedGainPercent { get; set; }
        public string Category { get; set; } = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
