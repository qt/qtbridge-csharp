using Qt.MetaObject;
using Qt.Quick;

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NewProject;

[QObject]
[QmlElement(Name = "Counter", Singleton = true)]
public class Counter : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private int _clicks = 0;
    public int Clicks
    {
        get => _clicks;
        set
        {
            if (_clicks == value)
                return;
            _clicks = value;
            OnPropertyChanged();
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class Program
{
    internal static void Main(string[] args)
    {
        Qml.LoadFromRootModule("Main");
        Qml.WaitForExit();
    }
}
