using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowPositioner.Models
{
    public class WindowConfig : INotifyPropertyChanged
    {
        private bool _isEnabled = true;
        private string _processName = string.Empty;
        private string _windowTitleHint = string.Empty;
        private bool _controlPosition = true;
        private int _targetX;
        private int _targetY;
        private bool _controlSize = true;
        private int _targetWidth;
        private int _targetHeight;

        public bool IsEnabled { get => _isEnabled; set => SetField(ref _isEnabled, value); }
        public string ProcessName { get => _processName; set => SetField(ref _processName, value); }
        public string WindowTitleHint { get => _windowTitleHint; set => SetField(ref _windowTitleHint, value); }
        public bool ControlPosition { get => _controlPosition; set => SetField(ref _controlPosition, value); }
        public int TargetX { get => _targetX; set => SetField(ref _targetX, value); }
        public int TargetY { get => _targetY; set => SetField(ref _targetY, value); }
        public bool ControlSize { get => _controlSize; set => SetField(ref _controlSize, value); }
        public int TargetWidth { get => _targetWidth; set => SetField(ref _targetWidth, value); }
        public int TargetHeight { get => _targetHeight; set => SetField(ref _targetHeight, value); }


        public WindowConfig() { }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<TField>(ref TField field, TField value, [CallerMemberName] string propertyName = null)
        {
            if(EqualityComparer<TField>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}