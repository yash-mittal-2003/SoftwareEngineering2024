using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Dashboard;



namespace ViewModel.DashboardViewModel;

//viewmodel for custom window
public class WindowViewModel : INotifyPropertyChanged
{
    #region Private Members

    private Window _mwindow;

    //Margin around the window that allows to Drop Shadow
    private int _mouterMarginSize = 10;

    private int _mwindowCornerSize = 10;

    #endregion

    #region Public Members
    public MainPageViewModel MainPageViewModel { get; private set; }
    public double WindowMinimumWidth { get; set; } = 400;
    public double WindowMaximumHeight { get; set; } = 400;

    /// <summary>
    /// True if the window should be borderless because it is docked or maximized
    /// </summary>
    public bool Borderless => _mwindow.WindowState == WindowState.Maximized;

    /// <summary>
    /// The size of the resize border around the window
    /// </summary>
    public int Resizebordersize => Borderless ? 0 : 6;
    public Thickness ResizeBorderThickness => new Thickness(Resizebordersize + OuterMarginSize);

    public Thickness InnerContentPadding => new Thickness(Resizebordersize);
    public int OuterMarginSize
    {
        get => _mwindow.WindowState == WindowState.Maximized ? 0 : _mouterMarginSize;
        set => _mouterMarginSize = value;
    }
    public Thickness OuterMarginThickness => new Thickness(OuterMarginSize);
    public int CornerRadius
    {
        get => _mwindow.WindowState == WindowState.Maximized ? 0 : _mwindowCornerSize;
        set => _mwindowCornerSize = value;
    }
    public CornerRadius WindowCornerRadius => new CornerRadius(CornerRadius);
    public int CaptionHeightSize{get;set;} =42;

    public GridLength CaptionHeightGridLength => new GridLength(CaptionHeightSize);

    //currentpage of application
    //public ApplicationPage CurrentPage { get; set; } = ApplicationPage.Homepage;

    #endregion

    #region Commands
    //The command to minimize the  Window
    public ICommand MinimizeCommand { get; set; }

    //The command to maximize the  Window
    public ICommand MaximizeCommand { get; set; }

    //The command Close the  Window
    public ICommand CloseCommand { get; set; }

    //The command to show Menu of the window
    public ICommand MenuCommand { get; set; }


    #endregion

    #region Constructor
    private ApplicationPage _currentPage = ApplicationPage.Login;
    public ApplicationPage CurrentPage
    {
        get => _currentPage;
        set
        {
            _currentPage = value;
            OnPropertyChanged(nameof(CurrentPage));
        }
    }
    public WindowViewModel(Window window, MainPageViewModel viewModel)
    {
        this._mwindow = window;
        this.MainPageViewModel = viewModel;

        _mwindow.StateChanged += (sender, e) =>
        {
            OnPropertyChanged(nameof(CaptionHeightGridLength));
            OnPropertyChanged(nameof(ResizeBorderThickness));
            OnPropertyChanged(nameof(CornerRadius));
            OnPropertyChanged(nameof(WindowCornerRadius));

        };
        MinimizeCommand = new RelayCommand(_ => _mwindow.WindowState = WindowState.Minimized);
        MaximizeCommand = new RelayCommand(_ => _mwindow.WindowState ^= WindowState.Maximized);
        CloseCommand = new RelayCommand(_ => _mwindow.Close());
        MenuCommand = new RelayCommand(_ => SystemCommands.ShowSystemMenu(_mwindow, GetMousePosition()));
    }
    #endregion
    #region Private Helpers
    /// <summary>
    /// Gets the current mouse position on the screen
    /// </summary>
    /// <returns></returns>
    private Point GetMousePosition()
    {
        // Position of the mouse relative to the window
        Point position = Mouse.GetPosition(_mwindow);

        // Add the window position so its a "ToScreen"
        return new Point(position.X + _mwindow.Left, position.Y + _mwindow.Top);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

}
