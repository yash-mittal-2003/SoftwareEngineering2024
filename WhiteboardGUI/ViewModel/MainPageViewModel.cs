/**************************************************************************************************
 * Filename    = MainPageViewModel.cs
 *
 * Authors     = Likith Anaparty, Rachit Jain, and Kshitij Ghodake
 *
 * Product     = WhiteBoard
 * 
 * Project     = WhiteboardGUI
 *
 * Description = ViewModel class for the main page of the Whiteboard GUI application.
 *               Handles user interactions, shape management, networking, and rendering.
 *               Implements the INotifyPropertyChanged interface for data binding.
 *************************************************************************************************/

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using WhiteboardGUI.Adorners;
using WhiteboardGUI.Models;
using WhiteboardGUI.Services;
using System.Windows.Data;
using System.Windows.Navigation;

namespace WhiteboardGUI.ViewModel;

/// <summary>
/// ViewModel for the Main Page of the Whiteboard GUI application.
/// Handles user interactions, shape management, networking, and rendering.
/// Implements the INotifyPropertyChanged interface for data binding.
/// </summary>
public class MainPageViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// Service responsible for handling networking operations.
    /// </summary>
    private readonly NetworkingService _networkingService;

    /// <summary>
    /// Service responsible for managing undo and redo operations.
    /// </summary>
    private readonly UndoRedoService _undoRedoService = new();

    /// <summary>
    /// Service responsible for rendering shapes on the canvas.
    /// </summary>
    public readonly RenderingService RenderingService;

    /// <summary>
    /// Service responsible for handling snapshot operations.
    /// </summary>
    private readonly SnapShotService _snapShotService;

    // <summary>
    /// Service responsible for managing the Z-index of shapes.
    /// </summary>
    private readonly MoveShapeZIndexing _moveShapeZIndexing;

    /// <summary>
    /// Gets the Client ID from the networking service.
    /// </summary>
    public double ClientID => _networkingService._clientID;

    /// <summary>
    /// Timer for periodic operations, such as checking dark mode settings.
    /// </summary>
    private readonly DispatcherTimer _timer;


    private string _defaultColor;
    private IShape _selectedShape;
    private ShapeType _currentTool = ShapeType.Pencil;
    private Point _startPoint;
    private Point _lastMousePosition;
    private bool _isSelecting;
    private bool _isDragging;
    private ObservableCollection<IShape> _shapes;
    private SnapShotDownloadItem _selectedDownloadItem;

    //for textbox
    private string _textInput;
    private bool _isTextBoxActive;
    private TextShape _currentTextShape;
    private TextboxModel _currentTextboxModel;

    // bouding box, might be unused
    private bool _isBoundingBoxActive;

    private IShape _hoveredShape;
    private bool _isShapeHovered;

    /// <summary>
    /// Gets or sets the current hover adorner.
    /// </summary>
    public HoverAdorner CurrentHoverAdorner { get; set; }

    private byte _red = 0;
    private byte _green = 0;
    private byte _blue = 0;
    private FrameworkElement _capturedElement;
    private bool _isUploading;
    private bool _isDarkModeManuallySet = false;
    private bool _isUpdatingDarkModeFromTimer = false;
    private bool _isDownloading;
    private double _selectedThickness = 2.0;
    private Color _selectedColor = Colors.Black;
    private Color _currentColor = Colors.Black;
    private bool _isPopupOpen;
    private string _snapShotFileName;
    private bool _isDarkMode;

    /// <summary>
    /// Gets or sets the background brush for the page.
    /// </summary>
    private Brush _pageBackground = new SolidColorBrush(Color.FromRgb(245, 245, 245)); // Light

    /// <summary>
    /// Gets or sets the background brush for the canvas.
    /// </summary>
    private Brush _canvasBackground = new SolidColorBrush(Color.FromRgb(245, 245, 245)); // Light
    private static MainPageViewModel s_whiteboardInstance;
    private readonly ReceivedDataService _receivedDataService;
    public string _userName;
    public int _userId;
    public string _userEmail;
    public string _profilePictureURL;

    private static readonly object s_padlock = new object();
    public static MainPageViewModel WhiteboardInstance
    {
        get {
            lock (s_padlock)
            {
                if (s_whiteboardInstance == null)
                {
                    s_whiteboardInstance = new MainPageViewModel();
                }

                return s_whiteboardInstance;
            }
        }
    }


    /// <summary>
    /// Gets or sets the default color as a string.
    /// Notifies listeners when the value changes.
    /// </summary>
    public string DefaultColor
    {
        get => _defaultColor;
        set {
            if (_defaultColor != value)
            {
                _defaultColor = value;
                OnPropertyChanged(nameof(DefaultColor));
            }
        }
    }

    /// <summary>
    /// Gets or sets the starting point of a shape or selection.
    /// </summary>
    public Point StartPoint
    {
        get => _startPoint;
        set => _startPoint = StartPoint;
    }

    /// <summary>
    /// Gets or sets a value indicating whether a selection is in progress.
    /// </summary>
    public bool IsSelecting
    {
        get => _isSelecting;
        set => _isSelecting = value;
    }

    /// <summary>
    /// Gets or sets the last known mouse position.
    /// </summary>
    public Point LastMousePosition
    {
        get => _lastMousePosition;
        set => _lastMousePosition = value;
    }

    /// <summary>
    /// Gets or sets the current textbox model.
    /// </summary>
    public TextboxModel CurrentTextboxModel
    {
        get => _currentTextboxModel;
        set => _currentTextboxModel = value;
    }

    /// <summary>
    /// Gets or sets the red component of the selected color.
    /// Updates the selected color when changed.
    /// </summary>
    public byte Red
    {
        get => _red;
        set {
            _red = value;
            OnPropertyChanged(nameof(Red));
            UpdateSelectedColor();
        }
    }

    /// <summary>
    /// Gets or sets the green component of the selected color.
    /// Updates the selected color when changed.
    /// </summary>
    public byte Green
    {
        get => _green;
        set {
            _green = value;
            OnPropertyChanged(nameof(Green));
            UpdateSelectedColor();
        }
    }

    /// <summary>
    /// Gets or sets the blue component of the selected color.
    /// Updates the selected color when changed.
    /// </summary>
    public byte Blue
    {
        get => _blue;
        set {
            _blue = value;
            OnPropertyChanged(nameof(Blue));
            UpdateSelectedColor();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether an upload is in progress.
    /// </summary>
    public bool IsUploading
    {
        get => _isUploading;
        set {
            _isUploading = value;
            OnPropertyChanged(nameof(IsUploading));
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether a drag operation is in progress.
    /// </summary>
    public bool IsDragging
    {
        get => _isDragging;
        set {
            _isDragging = value;
            OnPropertyChanged(nameof(IsDragging));
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether a download is in progress.
    /// </summary>
    public bool IsDownloading
    {
        get => _isDownloading;
        set {
            _isDownloading = value;
            OnPropertyChanged(nameof(IsDownloading));
        }
    }

    /// <summary>
    /// Gets or sets the thickness of the selected shape's stroke.
    /// Updates the shape and notifies the rendering service when changed.
    /// </summary>
    public double SelectedThickness
    {
        get => _selectedThickness;
        set {
            if (_selectedThickness != value)
            {
                _selectedThickness = value;
                OnPropertyChanged(nameof(SelectedThickness));
                if (SelectedShape is ShapeBase shapeBase)
                {
                    SelectedShape.StrokeThickness = _selectedThickness;
                    shapeBase.OnPropertyChanged(nameof(SelectedShape.StrokeThickness));
                    RenderingService.RenderShape(SelectedShape, "MODIFY");
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected color for drawing.
    /// Updates the shape and notifies the rendering service when changed.
    /// </summary>
    public Color SelectedColor
    {
        get => _selectedColor;
        set {
            if (_selectedColor != value)
            {
                _selectedColor = value;
                OnPropertyChanged(nameof(SelectedColor));
                if (SelectedShape is ShapeBase shapeBase)
                {
                    SelectedShape.Color = _selectedColor.ToString();
                    shapeBase.OnPropertyChanged(nameof(SelectedShape.Color));
                    RenderingService.RenderShape(SelectedShape, "MODIFY");
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the current color being used.
    /// </summary>
    public Color CurrentColor
    {
        get => _currentColor;
        set {
            if (_currentColor != value)
            {
                _currentColor = value;
                OnPropertyChanged(nameof(CurrentColor));
            }
        }
    }

    /// <summary>
    /// Handles changes to the Shapes collection.
    /// Updates the Z-indices of shapes when the collection changes.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event arguments.</param>
    private void Shapes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        Trace.TraceInformation("Shapes collection changed");
        _moveShapeZIndexing.UpdateZIndices();
    }

    /// <summary>
    /// Gets or sets the text input from the user.
    /// Writes the input to the debug output when changed.
    /// </summary>
    public string TextInput
    {
        get => _textInput;
        set {
            _textInput = value;
            OnPropertyChanged(nameof(TextInput));
            Debug.WriteLine(_textInput);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the textbox is active.
    /// Updates the visibility of the textbox when changed.
    /// </summary>
    public bool IsTextBoxActive
    {
        get => _isTextBoxActive;
        set {
            _isTextBoxActive = value;
            OnPropertyChanged(nameof(IsTextBoxActive));
            OnPropertyChanged(nameof(TextBoxVisibility));
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether a popup is open.
    /// </summary>
    public bool IsPopupOpen
    {
        get => _isPopupOpen;
        set {
            if (_isPopupOpen != value)
            {
                _isPopupOpen = value;
                OnPropertyChanged(nameof(IsPopupOpen));
            }
        }
    }

    /// <summary>
    /// Gets or sets the bounds of the textbox.
    /// </summary>
    public Rect TextBoxBounds { get; set; }

    /// <summary>
    /// Gets or sets the font size of the textbox.
    /// </summary>
    public double TextBoxFontSize { get; set; } = 16;

    /// <summary>
    /// Gets the visibility of the textbox based on its active state.
    /// </summary>
    public Visibility TextBoxVisibility =>
        IsTextBoxActive ? Visibility.Visible : Visibility.Collapsed;


    /// <summary>
    /// Gets or sets the collection of shapes on the canvas.
    /// Notifies listeners when the collection changes.
    /// </summary>
    public ObservableCollection<IShape> Shapes
    {
        get => _shapes;
        set {
            _shapes = value;
            OnPropertyChanged(nameof(Shapes));
        }
    }

    /// <summary>
    /// Gets or sets the filename for snapshots.
    /// </summary>
    public string SnapShotFileName
    {
        get => _snapShotFileName;
        set {
            if (_snapShotFileName != value)
            {
                _snapShotFileName = value;
                OnPropertyChanged(nameof(SnapShotFileName));
            }
        }
    }

    /// <summary>
    /// Gets or sets the currently selected shape.
    /// Manages selection state and notifies listeners when changed.
    /// </summary>
    public IShape SelectedShape
    {
        get => _selectedShape;
        set {
            if (_selectedShape != value)
            {
                if (_selectedShape != null)
                {
                    _selectedShape.IsSelected = false;
                }

                _selectedShape = value;

                if (_selectedShape != null)
                {
                    _selectedShape.IsSelected = true;
                }

                OnPropertyChanged(nameof(SelectedShape));
                OnPropertyChanged(nameof(IsShapeSelected));
                //UpdateColorAndThicknessFromSelectedShape();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether a shape is currently selected.
    /// </summary>
    public bool IsShapeSelected => SelectedShape != null;

    /// <summary>
    /// Gets or sets the current tool selected by the user.
    /// </summary>
    public ShapeType CurrentTool
    {
        get => _currentTool;
        set {
            //without textbox
            _currentTool = value;
            OnPropertyChanged(nameof(CurrentTool));
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the user is a host.
    /// </summary>
    public bool IsHost { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is a client.
    /// </summary>
    public bool IsClient { get; set; }
    private bool _isClearConfirmationOpen;

    /// <summary>
    /// Gets or sets the collection view for download items.
    /// </summary>
    public ListCollectionView DownloadItems { get; set; }



    /// <summary>
    /// Gets or sets the selected download item.
    /// Notifies listeners when changed.
    /// </summary>
    public SnapShotDownloadItem SelectedDownloadItem
    {
        get => _selectedDownloadItem;
        set {
            _selectedDownloadItem = value;
            OnPropertyChanged(nameof(SelectedDownloadItem));
            OnPropertyChanged(nameof(CanDownload)); // Notify change for CanDownload
        }
    }

    /// <summary>
    /// Gets a value indicating whether a download can be performed.
    /// </summary>
    public bool CanDownload => !(SelectedDownloadItem == null);

    /// <summary>
    /// Gets or sets a value indicating whether the download popup is open.
    /// </summary>
    public bool IsDownloadPopupOpen { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the clear confirmation popup is open.
    /// </summary>
    public bool IsClearConfirmationOpen
    {
        get => _isClearConfirmationOpen;
        set {
            if (_isClearConfirmationOpen != value)
            {
                _isClearConfirmationOpen = value;
                OnPropertyChanged(nameof(IsClearConfirmationOpen));
            }
        }
    }

    /// <summary>
    /// Command to open the clear confirmation popup.
    /// </summary>
    public ICommand OpenClearConfirmationCommand { get; }

    /// <summary>
    /// Command to confirm the clearing of shapes.
    /// </summary>
    public ICommand ConfirmClearCommand { get; }

    /// <summary>
    /// Command to cancel the clear action.
    /// </summary>
    public ICommand CancelClearCommand { get; }

    /// <summary>
    /// Command to start hosting the application.
    /// </summary>
    public ICommand StartHostCommand { get; }

    /// <summary>
    /// Command to start the client.
    /// </summary>
    public ICommand StartClientCommand { get; }

    /// <summary>
    /// Command to stop hosting the application.
    /// </summary>
    public ICommand StopHostCommand { get; }

    /// <summary>
    /// Command to stop the client.
    /// </summary>
    public ICommand StopClientCommand { get; }

    /// <summary>
    /// Command to select a drawing tool.
    /// </summary>
    public ICommand SelectToolCommand { get; }

    /// <summary>
    /// Command to draw a shape on the canvas.
    /// </summary>
    public ICommand DrawShapeCommand { get; }

    /// <summary>
    /// Command to select a shape on the canvas.
    /// </summary>
    public ICommand SelectShapeCommand { get; }

    /// <summary>
    /// Command to delete the selected shape.
    /// </summary>
    public ICommand DeleteShapeCommand { get; }

    /// <summary>
    /// Command triggered when the left mouse button is pressed on the canvas.
    /// </summary>
    public ICommand CanvasLeftMouseDownCommand { get; }

    /// <summary>
    /// Command triggered when the mouse moves over the canvas.
    /// </summary>
    public ICommand CanvasMouseMoveCommand { get; }

    /// <summary>
    /// Command triggered when the left mouse button is released on the canvas.
    /// </summary>
    public ICommand CanvasMouseUpCommand { get; }

    /// <summary>
    /// Command to finalize the textbox input.
    /// </summary>
    public ICommand FinalizeTextBoxCommand { get; }

    /// <summary>
    /// Command to cancel the textbox input.
    /// </summary>
    public ICommand CancelTextBoxCommand { get; }

    /// <summary>
    /// Command to perform an undo operation.
    /// </summary>
    public ICommand UndoCommand { get; }

    /// <summary>
    /// Command to perform a redo operation.
    /// </summary>
    public ICommand RedoCommand { get; }

    /// <summary>
    /// Command to select a color.
    /// </summary>
    public ICommand SelectColorCommand { get; }

    /// <summary>
    /// Command to submit a file name.
    /// </summary>
    public ICommand SubmitCommand { get; }

    /// <summary>
    /// Command to open a popup window.
    /// </summary>
    public ICommand OpenPopupCommand { get; }

    /// <summary>
    /// Command to clear all shapes from the canvas.
    /// </summary>
    public ICommand ClearShapesCommand { get; }

    /// <summary>
    /// Command to open the download popup.
    /// </summary>
    public ICommand OpenDownloadPopupCommand { get; }

    /// <summary>
    /// Command to download the selected item.
    /// </summary>
    public ICommand DownloadItemCommand { get; }

    /// <summary>
    /// Command to send a shape backward in the Z-order.
    /// </summary>
    public ICommand SendBackwardCommand { get; }

    /// <summary>
    /// Command to send a shape to the back in the Z-order.
    /// </summary>
    public ICommand SendToBackCommand { get; }

    /// <summary>
    /// Command to edit the text of a shape.
    /// </summary>
    public ICommand EditTextCommand { get; }

    /// <summary>
    /// Event triggered when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Event triggered when a shape is received from the network.
    /// </summary>
    public event Action<IShape> ShapeReceived;

    /// <summary>
    /// Event triggered when a shape is deleted.
    /// </summary>
    public event Action<IShape> ShapeDeleted;

    /// <summary>
    /// Instance of ServerOrClient class used for networking.
    /// </summary>
    private ServerOrClient _serverOrClient = ServerOrClient.ServerOrClientInstance;



    /// <summary>
    /// Initializes a new instance of the <see cref="MainPageViewModel"/> class.
    /// Sets up services, commands, event handlers, and initial state.
    /// </summary>
    public MainPageViewModel()
    {
        Trace.TraceInformation("Initializing MainPageViewModel");

        Shapes = new ObservableCollection<IShape>();
        _userId = _serverOrClient._userId;
        _userName = _serverOrClient._userName;
        _userEmail = _serverOrClient._userEmail;
        _profilePictureURL = _serverOrClient._profilePictureURL;
        _receivedDataService = new ReceivedDataService(_userId);
        _networkingService = new NetworkingService(_receivedDataService);
        if (_userId == 1)
        {
            _networkingService.StartHost();
        }
        else
        {
            _networkingService.StartClient();
        }
        RenderingService = new RenderingService(_networkingService, _undoRedoService, Shapes, _userId, _userName);
        _snapShotService = new SnapShotService(
            _networkingService,
            RenderingService,
            Shapes,
            _undoRedoService,
            _userEmail
        );
        _moveShapeZIndexing = new MoveShapeZIndexing(Shapes);

        DownloadItems = new ListCollectionView(new List<SnapShotDownloadItem>());
        _snapShotService.OnSnapShotUploaded += RefreshDownloadItems;

        _receivedDataService.ShapeReceived += OnShapeReceived;
        _receivedDataService.ShapeDeleted += OnShapeDeleted;
        _receivedDataService.ShapeModified += OnShapeModified;
        _receivedDataService.ShapesClear += OnShapeClear;
        _receivedDataService.ShapeSendBackward += _moveShapeZIndexing.MoveShapeBackward;
        _receivedDataService.ShapeSendToBack += _moveShapeZIndexing.MoveShapeBack;
        _receivedDataService.ShapeLocked += OnShapeLocked;
        _receivedDataService.ShapeUnlocked += OnShapeUnlocked;

        Shapes.CollectionChanged += Shapes_CollectionChanged;

        Debug.WriteLine("ViewModel init start");
        SelectToolCommand = new RelayCommand<ShapeType>(SelectTool);
        DrawShapeCommand = new RelayCommand<object>(parameter => {
            if (parameter is Tuple<IShape, string> args)
            {
                RenderingService.RenderShape(args.Item1, args.Item2);
            }
        });

        SelectShapeCommand = new RelayCommand<IShape>(SelectShape);
        DeleteShapeCommand = new RelayCommand(DeleteSelectedShape, () => SelectedShape != null);
        CanvasLeftMouseDownCommand = new RelayCommand<MouseButtonEventArgs>(
            OnCanvasLeftMouseDown
        );
        CanvasMouseMoveCommand = new RelayCommand<MouseEventArgs>(OnCanvasMouseMove);
        CanvasMouseUpCommand = new RelayCommand<MouseButtonEventArgs>(OnCanvasMouseUp);
        FinalizeTextBoxCommand = new RelayCommand(FinalizeTextBox);
        CancelTextBoxCommand = new RelayCommand(CancelTextBox);
        UndoCommand = new RelayCommand(CallUndo);
        RedoCommand = new RelayCommand(CallRedo);
        SelectColorCommand = new RelayCommand<string>(SelectColor);

        // Right-Click Menu Commands
        SendBackwardCommand = new RelayCommand<ShapeBase>(SendBackward);
        SendToBackCommand = new RelayCommand<ShapeBase>(SendToBack);
        EditTextCommand = new RelayCommand<ShapeBase>(EditText);

        SubmitCommand = new RelayCommand(async () => await SubmitFileName());
        OpenDownloadPopupCommand = new RelayCommand(OpenDownloadPopup);
        DownloadItemCommand = new RelayCommand(DownloadSelectedItem, () => CanDownload);

        OpenPopupCommand = new RelayCommand(OpenPopup);
        ClearShapesCommand = new RelayCommand(ClearShapes);

        OpenClearConfirmationCommand = new RelayCommand(OpenClearConfirmation);
        ConfirmClearCommand = new RelayCommand(ConfirmClear);
        CancelClearCommand = new RelayCommand(CancelClear);

        //Initialize Dark Mode to Light
        IsDarkMode = false;
        DefaultColor = "Black";
        Red = 0;
        Green = 0;
        Blue = 0;
        UpdateSelectedColor();

        // Initialize Dark Mode
        IsDarkMode = CheckIfDarkMode();

        // Set up timer to check time every minute
        _timer = new DispatcherTimer {
            Interval = TimeSpan.FromSeconds(10)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();

        //_whiteboardInstance = this;

        Trace.TraceInformation("MainPageViewModel initialized successfully");
    }

    /// <summary>
    /// Determines whether dark mode should be active based on the current time.
    /// Dark mode is active from 7 PM to 6 AM.
    /// </summary>
    /// <returns>True if dark mode should be active; otherwise, false.</returns>
    private bool CheckIfDarkMode()
    {
        Trace.TraceInformation("Entering CheckIfDarkMode");
        TimeSpan now = DateTime.Now.TimeOfDay;
        var start = new TimeSpan(19, 0, 0); // 7 PM
        var end = new TimeSpan(6, 0, 0); // 6 AM

        // Dark Mode is active from 7 PM to 6 AM
        if (now >= start || now < end)
        {
            Trace.TraceInformation("Dark mode is active");
            return true;
        }
        Trace.TraceInformation("Dark mode is not active");
        return false;
    }

    /// <summary>
    /// Event handler for the timer tick.
    /// Checks and updates dark mode based on the current time.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event arguments.</param>
    private void Timer_Tick(object sender, EventArgs e)
    {
        Trace.TraceInformation("Timer_Tick event triggered");
        if (!_isDarkModeManuallySet)
        {
            bool currentDarkMode = CheckIfDarkMode();
            if (currentDarkMode != IsDarkMode)
            {
                _isUpdatingDarkModeFromTimer = true; // Indicate that the change is from the timer
                IsDarkMode = currentDarkMode;
                _isUpdatingDarkModeFromTimer = false; // Reset the flag
            }
        }
    }

    /// <summary>
    /// Opens the clear confirmation popup.
    /// </summary>
    private void OpenClearConfirmation()
    {
        Trace.TraceInformation("Opening clear confirmation popup");
        IsClearConfirmationOpen = true;
    }

    /// <summary>
    /// Confirms the clearing of all shapes and closes the confirmation popup.
    /// </summary>
    private void ConfirmClear()
    {
        Trace.TraceInformation("Confirming clear shapes");
        ClearShapes(); // Existing method to clear shapes
        IsClearConfirmationOpen = false;
    }

    /// <summary>
    /// Cancels the clear action and closes the confirmation popup.
    /// </summary>
    private void CancelClear()
    {
        Trace.TraceInformation("Cancelling clear shapes");
        IsClearConfirmationOpen = false;
    }

    /// <summary>
    /// Updates the background colors based on the dark mode setting.
    /// </summary>
    /// <param name="isDarkMode">Indicates whether dark mode is active.</param>
    private void UpdateBackground(bool isDarkMode)
    {
        Trace.TraceInformation($"Updating background to {(isDarkMode ? "dark" : "light")} mode");
        if (isDarkMode)
        {
            PageBackground = new SolidColorBrush(Color.FromRgb(30, 30, 30)); // Dark gray
            CanvasBackground = new SolidColorBrush(Color.FromRgb(50, 50, 50)); // Slightly lighter
        }
        else
        {
            PageBackground = new SolidColorBrush(Color.FromRgb(245, 245, 245)); // Light gray
            CanvasBackground = new SolidColorBrush(Color.FromRgb(255, 255, 255)); // White
        }
    }

    /// <summary>
    /// Check whether downloadItems are initialized
    /// </summary>
    private bool _isDownloadItemsFilled = false;

    /// <summary>
    /// Opens the download popup.
    /// </summary>
    private void OpenDownloadPopup()
    {
        Trace.TraceInformation("Opening download popup");
        if (!_isDownloadItemsFilled)
        {
            InitializeDownloadItems();
        }
        _isDownloadItemsFilled = true;
        IsDownloadPopupOpen = true;
        OnPropertyChanged(nameof(IsDownloadPopupOpen));
    }

    /// <summary>
    /// Downloads the selected snapshot item.
    /// </summary>
    private void DownloadSelectedItem()
    {
        Trace.TraceInformation("Downloading selected item");
        if (SelectedDownloadItem != null)
        {
            IsDownloading = false;
            try
            {
                Debug.WriteLine($"Downloading item: {SelectedDownloadItem}");
                _snapShotService.DownloadSnapShot(SelectedDownloadItem);
                Debug.WriteLine("Download Complete");
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Download failed: {ex.Message}");
            }
            finally
            {
                // Re-enable UI elements
                IsDownloading = false;
            }
        }

        // Close the popup after download
        IsDownloadPopupOpen = false;
        OnPropertyChanged(nameof(IsDownloadPopupOpen));
    }

    /// <summary>
    /// Initializes the download items by fetching snapshots asynchronously.
    /// </summary>
    private async void InitializeDownloadItems()
    {
        Trace.TraceInformation("Initializing download items");
        List<SnapShotDownloadItem> newSnaps = await _snapShotService.GetSnaps("a", true);
        DownloadItems = new ListCollectionView(newSnaps);
        OnPropertyChanged(nameof(DownloadItems));
    }

    /// <summary>
    /// Refreshes the download items by fetching the latest snapshots asynchronously.
    /// </summary>
    private async void RefreshDownloadItems()
    {
        Trace.TraceInformation("Refreshing download items");
        List<SnapShotDownloadItem> newSnaps = await _snapShotService.GetSnaps("a", false);
        DownloadItems = new ListCollectionView(newSnaps);
        OnPropertyChanged(nameof(DownloadItems));
    }

    /// <summary>
    /// Sends the specified shape one step backward in the Z-order.
    /// </summary>
    /// <param name="shape">The shape to move backward.</param>
    private void SendBackward(IShape shape)
    {
        Trace.TraceInformation($"Sending shape {shape.ShapeId} backward");
        _moveShapeZIndexing.MoveShapeBackward(shape);
        RenderingService.RenderShape(shape, "INDEX-BACKWARD");
    }

    /// <summary>
    /// Sends the specified shape to the back of the Z-order.
    /// </summary>
    /// <param name="shape">The shape to send to the back.</param>
    private void SendToBack(IShape shape)
    {
        Trace.TraceInformation($"Sending shape {shape.ShapeId} to back");
        _moveShapeZIndexing.MoveShapeBack(shape);
        RenderingService.RenderShape(shape, "INDEX-BACK");
    }

    /// <summary>
    /// Updates the selected color based on the RGB components.
    /// </summary>
    private void UpdateSelectedColor()
    {
        Trace.TraceInformation("Updating selected color");
        SelectedColor = Color.FromRgb(Red, Green, Blue);
    }

    /// <summary>
    /// Selects a color based on the provided color name.
    /// Adjusts the current color if dark mode is active and the selected color is black.
    /// </summary>
    /// <param name="colorName">The name of the color to select.</param>
    private void SelectColor(string colorName)
    {
        Trace.TraceInformation($"Selecting color {colorName}");
        var color = (Color)ColorConverter.ConvertFromString(colorName);
        SelectedColor = color;
        if (IsDarkMode && colorName == "Black")
        {
            CurrentColor = Colors.White;
        }
        else
        {
            CurrentColor = color;
        }
    }

    /// <summary>
    /// Performs an undo operation if possible.
    /// </summary>
    private void CallUndo()
    {
        Trace.TraceInformation("Entering CallUndo");
        if (_undoRedoService._undoList.Count > 0)
        {
            Trace.TraceInformation("Undo list has items. Performing Undo.");
            RenderingService.RenderShape(null, "UNDO");
        }
        else
        {
            Trace.TraceWarning("Undo list is empty. Nothing to undo.");
        }
        Trace.TraceInformation("Exiting CallUndo");
    }

    /// <summary>
    /// Performs a redo operation if possible.
    /// </summary>
    private void CallRedo()
    {
        Trace.TraceInformation("Entering CallRedo");
        if (_undoRedoService._redoList.Count > 0)
        {
            Trace.TraceInformation("Redo list has items. Performing Redo.");
            RenderingService.RenderShape(null, "REDO");
        }
        else
        {
            Trace.TraceWarning("Redo list is empty. Nothing to redo.");
        }
        Trace.TraceInformation("Exiting CallRedo");
    }

    /// <summary>
    /// Opens the popup window for submitting a snapshot filename.
    /// </summary>
    private void OpenPopup()
    {
        Trace.TraceInformation("Entering OpenPopup");
        SnapShotFileName = "";
        IsPopupOpen = true;
        Trace.TraceInformation("Exiting OpenPopup");
    }

    /// <summary>
    /// Submits the snapshot filename and uploads the snapshot asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task SubmitFileName()
    {
        Trace.TraceInformation("Entering SubmitFileName");
        IsUploading = true;

        try
        {
            Trace.TraceInformation($"Uploading snapshot with filename: {SnapShotFileName}");
            // Call the asynchronous upload method
            await _snapShotService.UploadSnapShot(SnapShotFileName, Shapes, false);
            Trace.TraceInformation("Snapshot uploaded successfully.");
            IsPopupOpen = false;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Upload failed: {ex.Message}");
        }
        finally
        {
            // Re-enable UI elements
            IsUploading = false;
            Trace.TraceInformation("Exiting SubmitFileName");
        }
    }

    /// <summary>
    /// Clears all shapes from the canvas.
    /// </summary>
    private void ClearShapes()
    {
        Trace.TraceInformation("Entering ClearShapes");
        RenderingService.RenderShape(null, "CLEAR");
        Trace.TraceInformation("Exiting ClearShapes");
    }

    /// <summary>
    /// Stops the host and updates the hosting state.
    /// </summary>
    private void StopHost()
    {
        Trace.TraceInformation("Entering StopHost");
        IsHost = false;
        _networkingService.StopHost();
        Trace.TraceInformation("Exiting StopHost");
    }

    /// <summary>
    /// Selects the drawing tool based on the specified shape type.
    /// </summary>
    /// <param name="tool">The shape type to select as the current tool.</param>
    private void SelectTool(ShapeType tool)
    {
        Trace.TraceInformation($"Selecting tool: {tool}");
        CurrentTool = tool;
        //for textbox
        //TextInput = string.Empty;
        Trace.TraceInformation($"Exiting SelectTool: {tool}");
    }

    /// <summary>
    /// Selects the specified shape.
    /// </summary>
    /// <param name="shape">The shape to select.</param>
    private void SelectShape(IShape shape)
    {
        Trace.TraceInformation($"SelectShape called with shape ID: {shape.ShapeId}");
        // Method implementation (currently empty)
    }

    /// <summary>
    /// Deletes the specified shape by rendering a delete action.
    /// </summary>
    /// <param name="shape">The shape to delete.</param>
    private void DeleteShape(IShape shape)
    {
        Trace.TraceInformation($"Deleting shape with ID: {shape.ShapeId}");
        RenderingService.RenderShape(shape, "DELETE");
        Trace.TraceInformation($"Exiting DeleteShape for shape ID: {shape.ShapeId}");
    }


    /// <summary>
    /// Deletes the currently selected shape and clears the selection.
    /// </summary>
    private void DeleteSelectedShape()
    {
        Trace.TraceInformation("Entering DeleteSelectedShape");
        if (SelectedShape != null)
        {
            Trace.TraceInformation($"Deleting selected shape with ID: {SelectedShape.ShapeId}");
            RenderingService.RenderShape(SelectedShape, "DELETE");
            SelectedShape = null;
        }
        else
        {
            Trace.TraceWarning("No shape is selected to delete.");
        }
        Trace.TraceInformation("Exiting DeleteSelectedShape");
    }

    /// <summary>
    /// Determines whether a given point is over the specified shape.
    /// Uses simple bounding box hit testing.
    /// </summary>
    /// <param name="shape">The shape to test against.</param>
    /// <param name="point">The point to test.</param>
    /// <returns>True if the point is over the shape; otherwise, false.</returns>
    public bool IsPointOverShape(IShape shape, Point point)
    {
        Trace.TraceInformation($"Checking if point {point} is over shape ID: {shape.ShapeId}");
        // Simple bounding box hit testing
        Rect bounds = shape.GetBounds();
        bool isOver = bounds.Contains(point);
        Trace.TraceInformation($"IsPointOverShape result for shape ID {shape.ShapeId}: {isOver}");
        return isOver;
    }

    /// <summary>
    /// Edits the text of the specified shape if it is a <see cref="TextShape"/>.
    /// Activates the textbox for editing.
    /// </summary>
    /// <param name="shape">The shape to edit.</param>
    private void EditText(IShape shape)
    {
        Trace.TraceInformation($"Entering EditText for shape ID: {shape.ShapeId}");
        if (shape is TextShape textShape)
        {
            if (CurrentTool == ShapeType.Select)
            {
                Trace.TraceInformation($"Editing text shape with ID: {textShape.ShapeId}");
                // Found a TextShape under the click
                _currentTextShape = textShape;
                IsTextBoxActive = true;
                //Shapes.Remove(_currentTextShape);

                _currentTextShape.Color = "#FFFFFF";
                _currentTextShape.OnPropertyChanged(null);
                //OnPropertyChanged(nameof(textShape.Color));
                // Create a TextboxModel over the existing TextShape
                var textboxModel = new TextboxModel {
                    X = textShape.X,
                    Y = textShape.Y,
                    Width = textShape.Width,
                    Height = textShape.Height,
                    Text = textShape.Text,
                };
                _currentTextboxModel = textboxModel;
                Shapes.Add(textboxModel);
                OnPropertyChanged(nameof(TextBoxVisibility));
                Trace.TraceInformation($"Activated textbox for shape ID: {shape.ShapeId}");
            }
        }
        Trace.TraceInformation($"Exiting EditText for shape ID: {shape.ShapeId}");
    }

    /// <summary>
    /// Handles the left mouse button down event on the canvas.
    /// Initiates shape drawing, selection, or textbox activation based on the current tool.
    /// </summary>
    /// <param name="e">Mouse button event arguments.</param>
    private void OnCanvasLeftMouseDown(MouseButtonEventArgs e)
    {
        Trace.TraceInformation("Entering OnCanvasLeftMouseDown");
        // Pass the canvas as the element
        if (IsTextBoxActive == true)
        {
            Trace.TraceInformation("TextBox is active. Finalizing TextBox before proceeding.");
            FinalizeTextBox();
        }
        if (e.Source is FrameworkElement canvas)
        {
            Trace.TraceInformation("Canvas element detected. Capturing mouse.");
            canvas.CaptureMouse(); // Capture the mouse
            _capturedElement = canvas; // Store the captured element
            _startPoint = e.GetPosition(canvas);
            Trace.TraceInformation($"Mouse down at position: {_startPoint}");
            if (CurrentTool == ShapeType.Select)
            {
                Trace.TraceInformation("Current tool is Select. Initiating selection logic.");
                // Implement selection logic
                //if (SelectedShape != null)
                //{
                //    SelectedShape.IsSelected = false;
                //}
                _isSelecting = true;
                bool loopBreaker = false;
                foreach (IShape? shape in Shapes.Reverse())
                {
                    if (IsPointOverShape(shape, _startPoint))
                    {
                        Trace.TraceInformation($"Shape found under cursor: {shape.ShapeId}");
                        if (shape.IsLocked && shape.LockedByUserID != _networkingService._clientID)
                        {
                            Trace.TraceWarning($"Shape ID: {shape.ShapeId} is locked by another user (UserID: {shape.LockedByUserID}).");

                            if (SelectedShape != null)
                            {
                                Trace.TraceInformation($"Unlocking previously selected shape ID: {SelectedShape.ShapeId}");
                                SelectedShape.LockedByUserID = -1;

                                RenderingService.RenderShape(SelectedShape, "UNLOCK");
                            }

                            SelectedShape = null;
                            _isSelecting = false;
                            MessageBox.Show("This shape is locked by another user.", "Locked", MessageBoxButton.OK, MessageBoxImage.Information);
                            loopBreaker = true;
                            break;
                        }
                        else
                        {
                            Trace.TraceInformation($"Selecting shape ID: {shape.ShapeId}");
                            if (SelectedShape != null)
                            {
                                Trace.TraceInformation($"Unlocking previously selected shape ID: {SelectedShape.ShapeId}");
                                SelectedShape.LockedByUserID = -1;

                                RenderingService.RenderShape(SelectedShape, "UNLOCK");
                            }
                            SelectedShape = shape;

                            Debug.WriteLine(shape.IsSelected);
                            _lastMousePosition = _startPoint;
                            _isSelecting = true;
                            shape.LockedByUserID = _networkingService._clientID;
                            RenderingService.RenderShape(shape, "LOCK");
                            loopBreaker = true;
                            break;
                        }
                    }
                }

                if (loopBreaker == false)
                {
                    Trace.TraceInformation("No shape found under cursor. Clearing selection.");
                    _isSelecting = false;
                    if (SelectedShape != null)
                    {
                        Trace.TraceInformation($"Unlocking previously selected shape ID: {SelectedShape.ShapeId}");
                        SelectedShape.LockedByUserID = -1;

                        RenderingService.RenderShape(SelectedShape, "UNLOCK");
                    }
                    SelectedShape = null;
                }
            }
            else if (CurrentTool == ShapeType.Text)
            {
                Trace.TraceInformation("Current tool is Text. Activating TextBox.");
                // Get the position of the click
                Point position = e.GetPosition((IInputElement)e.Source);
                var textboxModel = new TextboxModel {
                    X = position.X,
                    Y = position.Y,
                    Width = 150,
                    Height = 30,
                };

                _currentTextboxModel = textboxModel;
                TextInput = string.Empty;
                IsTextBoxActive = true;
                Shapes.Add(textboxModel);
                OnPropertyChanged(nameof(TextBoxVisibility));
                Trace.TraceInformation($"TextBox activated at position: {position}");
            }
            else
            {
                Trace.TraceInformation($"Current tool is {CurrentTool}. Initiating shape creation.");
                // Start drawing a new shape
                IShape newShape = CreateShape(_startPoint);
                if (newShape != null)
                {
                    Trace.TraceInformation($"New shape created with ID: {newShape.ShapeId}");
                    newShape.BoundingBoxColor = "blue";
                    Shapes.Add(newShape);
                    if (SelectedShape != null)
                    {
                        Trace.TraceInformation($"Unlocking previously selected shape ID: {SelectedShape.ShapeId}");
                        SelectedShape.LockedByUserID = -1;

                        RenderingService.RenderShape(SelectedShape, "UNLOCK");
                    }
                    SelectedShape = newShape;
                    Trace.TraceInformation($"Shape ID: {newShape.ShapeId} is now selected.");
                }
                else
                {
                    Trace.TraceError("Failed to create a new shape.");
                }
            }
        }
        Trace.TraceInformation("Exiting OnCanvasLeftMouseDown");
    }

    /// <summary>
    /// Moves the specified shape based on the current mouse position.
    /// Updates the shape's position properties accordingly.
    /// </summary>
    /// <param name="shape">The shape to move.</param>
    /// <param name="currentPoint">The current mouse position.</param>
    private void MoveShape(IShape shape, Point currentPoint)
    {
        Trace.TraceInformation($"Entering MoveShape for shape ID: {shape.ShapeId}");
        Vector delta = currentPoint - _lastMousePosition;
        Trace.TraceInformation($"Mouse moved by delta: {delta}");
        switch (shape)
        {
            case LineShape line:
                line.StartX += delta.X;
                line.StartY += delta.Y;
                line.EndX += delta.X;
                line.EndY += delta.Y;
                Trace.TraceInformation($"Moved LineShape ID: {shape.ShapeId} by delta: {delta}");
                break;
            case CircleShape circle:
                circle.CenterX += delta.X;
                circle.CenterY += delta.Y;
                Trace.TraceInformation($"Moved CircleShape ID: {shape.ShapeId} by delta: {delta}");
                break;
            case ScribbleShape scribble:
                for (int i = 0; i < scribble.Points.Count; i++)
                {
                    scribble.Points[i] = new Point(
                        scribble.Points[i].X + delta.X,
                        scribble.Points[i].Y + delta.Y
                    );
                }
                Trace.TraceInformation($"Moved ScribbleShape ID: {shape.ShapeId} by delta: {delta}");
                break;
            case TextShape text:
                text.X += delta.X;
                text.Y += delta.Y;
                Trace.TraceInformation($"Moved TextShape ID: {shape.ShapeId} by delta: {delta}");
                break;
        }

        // Notify property changes
        if (shape is ShapeBase shapeBase)
        {
            shapeBase.OnPropertyChanged(null); // Notify all properties have changed
            Trace.TraceInformation($"Property changes notified for shape ID: {shape.ShapeId}");
        }
        Trace.TraceInformation($"Exiting MoveShape for shape ID: {shape.ShapeId}");
    }

    /// <summary>
    /// Handles the mouse move event on the canvas.
    /// Updates the shape being drawn or moved based on the current tool and mouse state.
    /// </summary>
    /// <param name="e">Mouse event arguments.</param>
    private void OnCanvasMouseMove(MouseEventArgs e)
    {
        Trace.TraceInformation("Entering OnCanvasMouseMove");
        //without textbox
        if (e.LeftButton == MouseButtonState.Pressed && SelectedShape != null)
        {
            Trace.TraceInformation($"Mouse move with selected shape ID: {SelectedShape.ShapeId}");
            if (e.Source is FrameworkElement canvas)
            {
                Point currentPoint = e.GetPosition(canvas);
                Trace.TraceInformation($"Current mouse position: {currentPoint}");
                if (_isDragging)
                {
                    Trace.TraceInformation($"Dragging flag is set. Current tool: {CurrentTool}");
                    if (CurrentTool == ShapeType.Select && SelectedShape != null)
                    {
                        Trace.TraceInformation($"Moving selected shape ID: {SelectedShape.ShapeId}");
                        MoveShape(SelectedShape, currentPoint);
                        _lastMousePosition = currentPoint;
                    }
                    else if (SelectedShape != null)
                    {
                        Trace.TraceInformation($"Updating shape ID: {SelectedShape.ShapeId}");
                        UpdateShape(SelectedShape, currentPoint);
                    }
                }
            }
        }
        Trace.TraceInformation("Exiting OnCanvasMouseMove");
    }

    /// <summary>
    /// Handles the left mouse button up event on the canvas.
    /// Finalizes shape drawing or selection based on the current state.
    /// </summary>
    /// <param name="e">Mouse button event arguments.</param>
    private void OnCanvasMouseUp(MouseButtonEventArgs e)
    {
        Trace.TraceInformation("Entering OnCanvasMouseUp");
        if (_capturedElement != null)
        {
            Trace.TraceInformation("Releasing captured mouse.");
            _capturedElement.ReleaseMouseCapture(); // Release the mouse capture
            _capturedElement = null;
        }

        //without textbox
        if (SelectedShape != null && !_isSelecting)
        {
            Trace.TraceInformation($"Finalizing shape creation for shape ID: {SelectedShape.ShapeId}");
            // Finalize shape drawing
            RenderingService.RenderShape(SelectedShape, "CREATE");
            SelectedShape = null;
        }
        else if (IsShapeSelected)
        {
            Trace.TraceInformation($"Modifying selected shape ID: {SelectedShape.ShapeId}");
            RenderingService.RenderShape(SelectedShape, "MODIFY");
            Debug.WriteLine(SelectedShape.IsSelected);
            //SelectedShape = null;
        }
        _isSelecting = false;
        Trace.TraceInformation("Exiting OnCanvasMouseUp");
    }

    /// <summary>
    /// Creates a new shape based on the current tool and starting point.
    /// Initializes shape properties such as color, thickness, and position.
    /// </summary>
    /// <param name="startPoint">The starting point of the shape.</param>
    /// <returns>A new instance of a shape implementing <see cref="IShape"/>.</returns>
    public IShape CreateShape(Point startPoint)
    {
        Trace.TraceInformation($"Entering CreateShape with tool: {CurrentTool} at startPoint: {startPoint}");
        IShape shape = null;
        switch (CurrentTool)
        {
            case ShapeType.Pencil:
                var scribbleShape = new ScribbleShape {
                    Color = SelectedColor.ToString(),
                    StrokeThickness = SelectedThickness,
                    Points = new System.Collections.Generic.List<Point> { startPoint },
                };

                shape = scribbleShape;
                Trace.TraceInformation($"Created ScribbleShape with ID: {shape.ShapeId}");
                break;
            case ShapeType.Line:
                var lineShape = new LineShape {
                    StartX = startPoint.X,
                    StartY = startPoint.Y,
                    EndX = startPoint.X,
                    EndY = startPoint.Y,
                    Color = SelectedColor.ToString(),
                    StrokeThickness = SelectedThickness,
                };

                shape = lineShape;
                Trace.TraceInformation($"Created LineShape with ID: {shape.ShapeId}");
                break;
            case ShapeType.Circle:
                var circleShape = new CircleShape {
                    CenterX = startPoint.X,
                    CenterY = startPoint.Y,
                    RadiusX = 0,
                    RadiusY = 0,
                    Color = SelectedColor.ToString(),
                    StrokeThickness = SelectedThickness,
                };

                shape = circleShape;
                Trace.TraceInformation($"Created CircleShape with ID: {shape.ShapeId}");
                break;
        }
        if (shape != null)
        {
            shape.UserID = _networkingService._clientID;
            shape.UserName = _userName;
            shape.ProfilePictureURL = _profilePictureURL;
            shape.ShapeId = Guid.NewGuid();
            Trace.TraceInformation($"Initialized shape properties for shape ID: {shape.ShapeId}");
        }
        Trace.TraceInformation($"Exiting CreateShape with shape ID: {shape?.ShapeId}");
        return shape;
    }

    /// <summary>
    /// Updates the shape's properties based on the current point.
    /// </summary>
    /// <param name="shape">The shape to update.</param>
    /// <param name="currentPoint">The current position of the mouse.</param>
    private void UpdateShape(IShape shape, Point currentPoint)
    {
        Trace.TraceInformation($"Entering UpdateShape for shape ID: {shape.ShapeId} with currentPoint: {currentPoint}");
        switch (shape)
        {
            case ScribbleShape scribble:
                scribble.AddPoint(currentPoint);
                Trace.TraceInformation($"Added point to ScribbleShape ID: {shape.ShapeId}");
                break;
            case LineShape line:
                line.EndX = currentPoint.X;
                line.EndY = currentPoint.Y;
                Trace.TraceInformation($"Updated LineShape ID: {shape.ShapeId} to EndX: {line.EndX}, EndY: {line.EndY}");
                break;
            case CircleShape circle:
                circle.RadiusX = Math.Abs(currentPoint.X - circle.CenterX);
                circle.RadiusY = Math.Abs(currentPoint.Y - circle.CenterY);
                Trace.TraceInformation($"Updated CircleShape ID: {shape.ShapeId} to RadiusX: {circle.RadiusX}, RadiusY: {circle.RadiusY}");
                break;
        }
        Trace.TraceInformation($"Exiting UpdateShape for shape ID: {shape.ShapeId}");
    }

    /// <summary>
    /// Handles the event when a shape is received from the network.
    /// Adds the shape to the canvas and updates undo history if required.
    /// </summary>
    /// <param name="shape">The received shape.</param>
    /// <param name="addToUndo">Indicates whether to update the undo history.</param>
    private void OnShapeReceived(IShape shape, bool addToUndo)
    {
        Trace.TraceInformation($"Entering OnShapeReceived for shape ID: {shape.ShapeId}, addToUndo: {addToUndo}");
        Application.Current.Dispatcher.Invoke(() => {
            Trace.TraceInformation($"Adding received shape ID: {shape.ShapeId} to Shapes collection");
            shape.IsSelected = false;
            Shapes.Add(shape);
            IShape newShape = shape.Clone();
            _networkingService._synchronizedShapes.Add(newShape);
            Trace.TraceInformation($"Cloned shape ID: {newShape.ShapeId} and added to synchronized shapes");
            if (addToUndo)
            {
                Trace.TraceInformation($"Removing last modified entry for shape ID: {shape.ShapeId} from undo history");
                _undoRedoService.RemoveLastModified(_networkingService, shape);
            }
        });
        Trace.TraceInformation($"Exiting OnShapeReceived for shape ID: {shape.ShapeId}");
    }

    /// <summary>
    /// Locks the specified shape and updates its appearance based on the locking user.
    /// </summary>
    /// <param name="shape">The shape to lock.</param>
    private void OnShapeLocked(IShape shape)
    {
        Trace.TraceInformation($"Entering OnShapeLocked for shape ID: {shape.ShapeId}");
        IShape? existingShape = Shapes.FirstOrDefault(s =>
               s.ShapeId == shape.ShapeId && s.UserID == shape.UserID
           );
        if (existingShape != null)
        {
            Trace.TraceInformation($"Locking shape ID: {existingShape.ShapeId} by UserID: {shape.LockedByUserID}");
            existingShape.IsLocked = true;
            existingShape.LockedByUserID = shape.LockedByUserID;
            existingShape.IsSelected = true;

            if (existingShape.LockedByUserID != ClientID)
            {
                existingShape.BoundingBoxColor = "red";
                Trace.TraceInformation($"Shape ID: {existingShape.ShapeId} is locked by another user. BoundingBoxColor set to red.");
            }
            else if (existingShape.LockedByUserID == ClientID)
            {
                existingShape.BoundingBoxColor = "blue";
                Trace.TraceInformation($"Shape ID: {existingShape.ShapeId} is locked by the current user. BoundingBoxColor set to blue.");
            }
        }
        else
        {
            Trace.TraceWarning($"Shape ID: {shape.ShapeId} not found in Shapes collection during lock operation.");
        }
        Trace.TraceInformation($"Exiting OnShapeLocked for shape ID: {shape.ShapeId}");
    }

    /// <summary>
    /// Unlocks the specified shape and updates its appearance.
    /// </summary>
    /// <param name="shape">The shape to unlock.</param>
    private void OnShapeUnlocked(IShape shape)
    {
        Trace.TraceInformation($"Entering OnShapeUnlocked for shape ID: {shape.ShapeId}");
        IShape? existingShape = Shapes.FirstOrDefault(s =>
               s.ShapeId == shape.ShapeId && s.UserID == shape.UserID
           );
        if (existingShape != null)
        {
            Trace.TraceInformation($"Unlocking shape ID: {existingShape.ShapeId}");
            existingShape.IsLocked = false;
            existingShape.LockedByUserID = -1;
            existingShape.IsSelected = false;
            if (existingShape.LockedByUserID != ClientID)
            {
                existingShape.BoundingBoxColor = "blue";
                Trace.TraceInformation($"Shape ID: {existingShape.ShapeId} BoundingBoxColor set to blue.");
            }
        }
        else
        {
            Trace.TraceWarning($"Shape ID: {shape.ShapeId} not found in Shapes collection during unlock operation.");
        }
        Trace.TraceInformation($"Exiting OnShapeUnlocked for shape ID: {shape.ShapeId}");
    }


    /// <summary>
    /// Modifies the properties of an existing shape based on a received shape update.
    /// </summary>
    /// <param name="shape">The modified shape.</param>
    private void OnShapeModified(IShape shape)
    {
        Trace.TraceInformation($"Entering OnShapeModified for shape ID: {shape.ShapeId}");
        Application.Current.Dispatcher.Invoke(() => {
            Trace.TraceInformation($"Modifying shape ID: {shape.ShapeId}");
            shape.IsSelected = false;
            //Shapes.Add(shape);

            IShape? existingShape = Shapes.FirstOrDefault(s =>
                s.ShapeId == shape.ShapeId && s.UserID == shape.UserID
            );

            if (existingShape == null)
            {
                Trace.TraceWarning($"Shape ID: {shape.ShapeId} not found in Shapes collection during modification.");
                return;
            }

            switch (shape.ShapeType)
            {
                case "Circle":
                    if (
                        existingShape is CircleShape existingCircle
                        && shape is CircleShape modifiedCircle
                    )
                    {
                        Trace.TraceInformation($"Modifying CircleShape ID: {existingCircle.ShapeId}");
                        existingCircle.LastModifierID = modifiedCircle.LastModifierID;
                        existingCircle.Color = modifiedCircle.Color;
                        existingCircle.StrokeThickness = modifiedCircle.StrokeThickness;

                        // Update Circle-specific properties
                        existingCircle.CenterX = modifiedCircle.CenterX;
                        existingCircle.CenterY = modifiedCircle.CenterY;
                        existingCircle.RadiusX = modifiedCircle.RadiusX;
                        existingCircle.RadiusY = modifiedCircle.RadiusY;
                        existingCircle.ZIndex = modifiedCircle.ZIndex;
                        Trace.TraceInformation($"CircleShape ID: {existingCircle.ShapeId} updated.");
                    }
                    break;
                case "Line":
                    if (
                        existingShape is LineShape existingLine
                        && shape is LineShape modifiedLine
                    )
                    {
                        Trace.TraceInformation($"Modifying LineShape ID: {existingLine.ShapeId}");
                        // Update common properties
                        existingLine.LastModifierID = modifiedLine.LastModifierID;
                        existingLine.Color = modifiedLine.Color;
                        existingLine.StrokeThickness = modifiedLine.StrokeThickness;

                        // Update Line-specific properties
                        existingLine.StartX = modifiedLine.StartX;
                        existingLine.StartY = modifiedLine.StartY;
                        existingLine.EndX = modifiedLine.EndX;
                        existingLine.EndY = modifiedLine.EndY;
                        existingLine.ZIndex = modifiedLine.ZIndex;
                        Trace.TraceInformation($"LineShape ID: {existingLine.ShapeId} updated.");
                    }
                    break;
                case "Scribble":
                    if (
                        existingShape is ScribbleShape existingScribble
                        && shape is ScribbleShape modifiedScribble
                    )
                    {
                        Trace.TraceInformation($"Modifying ScribbleShape ID: {existingScribble.ShapeId}");
                        // Update common properties
                        existingScribble.LastModifierID = modifiedScribble.LastModifierID;
                        existingScribble.Color = modifiedScribble.Color;
                        existingScribble.StrokeThickness = modifiedScribble.StrokeThickness;

                        // Update Scribble-specific properties
                        existingScribble.Points = new List<Point>(modifiedScribble.Points);
                        existingScribble.ZIndex = modifiedScribble.ZIndex;
                        Trace.TraceInformation($"ScribbleShape ID: {existingScribble.ShapeId} updated.");
                    }
                    break;
                case "TextShape":
                    if (
                        existingShape is TextShape existingText
                        && shape is TextShape modifiedText
                    )
                    {
                        Trace.TraceInformation($"Modifying TextShape ID: {existingText.ShapeId}");
                        // Update common properties
                        existingText.LastModifierID = modifiedText.LastModifierID;
                        existingText.Color = modifiedText.Color;
                        existingText.StrokeThickness = modifiedText.StrokeThickness;

                        // Update TextShape-specific properties
                        existingText.Text = modifiedText.Text;
                        existingText.X = modifiedText.X;
                        existingText.Y = modifiedText.Y;
                        existingText.FontSize = modifiedText.FontSize;
                        existingText.ZIndex = modifiedText.ZIndex;
                        Trace.TraceInformation($"TextShape ID: {existingText.ShapeId} updated.");
                    }
                    break;
            }

            IShape newShape = shape.Clone();
            Trace.TraceInformation($"Cloning modified shape ID: {newShape.ShapeId}");
            _undoRedoService.RemoveLastModified(_networkingService, shape);
            Trace.TraceInformation($"Removed last modified entry for shape ID: {shape.ShapeId} from undo history");
        });
        Trace.TraceInformation($"Exiting OnShapeModified for shape ID: {shape.ShapeId}");
    }

    /// <summary>
    /// Deletes the specified shape from the canvas and updates the synchronized shapes.
    /// </summary>
    /// <param name="shape">The shape to delete.</param>
    private void OnShapeDeleted(IShape shape)
    {
        Trace.TraceInformation($"Entering OnShapeDeleted for shape ID: {shape.ShapeId}");
        Application.Current.Dispatcher.Invoke(() => {
            Trace.TraceInformation($"Attempting to remove shape ID: {shape.ShapeId} from Shapes collection");
            foreach (IShape s in Shapes)
            {
                if (s.ShapeId == shape.ShapeId)
                {
                    Shapes.Remove(s);
                    Trace.TraceInformation($"Shape ID: {s.ShapeId} removed from Shapes collection");
                    break;
                }
            }
            _networkingService._synchronizedShapes.Remove(shape);
            Trace.TraceInformation($"Shape ID: {shape.ShapeId} removed from synchronized shapes");
        });
        Trace.TraceInformation($"Exiting OnShapeDeleted for shape ID: {shape.ShapeId}");
    }

    /// <summary>
    /// Clears all shapes from the canvas and resets undo/redo history and synchronized shapes.
    /// </summary>
    private void OnShapeClear()
    {
        Trace.TraceInformation("Entering OnShapeClear");
        Application.Current.Dispatcher.Invoke(() => {
            Trace.TraceInformation("Clearing all shapes from Shapes collection");
            Shapes.Clear();
            Trace.TraceInformation("Cleared Shapes collection");
            Trace.TraceInformation("Clearing undo and redo lists");
            _undoRedoService._redoList.Clear();
            _undoRedoService._undoList.Clear();
            Trace.TraceInformation("Cleared undo and redo lists");
            Trace.TraceInformation("Clearing synchronized shapes");
            _networkingService._synchronizedShapes.Clear();
            Trace.TraceInformation("Cleared synchronized shapes");
        });
        Trace.TraceInformation("Exiting OnShapeClear");
    }

    /// <summary>
    /// Cancels text box input and clears the current text box model.
    /// </summary>
    public void CancelTextBox()
    {
        Trace.TraceInformation("Entering CancelTextBox");
        TextInput = string.Empty;
        IsTextBoxActive = false;
        _currentTextShape = null;
        OnPropertyChanged(nameof(TextBoxVisibility));
        Trace.TraceInformation("Exiting CancelTextBox");
    }

    /// <summary>
    /// Finalizes text box input and updates or creates a text shape accordingly.
    /// </summary>
    public void FinalizeTextBox()
    {
        Trace.TraceInformation("Entering FinalizeTextBox");
        if ((_currentTextboxModel != null))
        {
            if (!string.IsNullOrEmpty(_currentTextboxModel.Text))
            {
                if (_currentTextShape != null)
                {
                    Trace.TraceInformation($"Finalizing text update for TextShape ID: {_currentTextShape.ShapeId}");
                    // We are editing an existing TextShape
                    _currentTextShape.Text = _currentTextboxModel.Text;
                    _currentTextShape.Color = "black";
                    _currentTextShape.LastModifierID = _networkingService._clientID;
                    _currentTextShape.X = _currentTextboxModel.X;
                    _currentTextShape.Y = _currentTextboxModel.Y;


                    _currentTextShape.OnPropertyChanged(null);
                    RenderingService.RenderShape(_currentTextShape, "MODIFY");
                    Trace.TraceInformation($"TextShape ID: {_currentTextShape.ShapeId} modified with new text.");
                }
                else
                {
                    Trace.TraceInformation("Creating a new TextShape.");
                    // Create a new TextShape
                    var textShape = new TextShape {
                        X = _currentTextboxModel.X,
                        Y = _currentTextboxModel.Y,
                        Text = _currentTextboxModel.Text,
                        Color = SelectedColor.ToString(),
                        FontSize = TextBoxFontSize,
                        ShapeId = Guid.NewGuid(),
                        UserID = _networkingService._clientID,
                        UserName = _userName,
                        LastModifiedBy = _userName,
                        LastModifierID = _networkingService._clientID,
                        ProfilePictureURL = _profilePictureURL
                    };
                    Shapes.Add(textShape);
                    RenderingService.RenderShape(textShape, "CREATE");
                    Trace.TraceInformation($"Created new TextShape with ID: {textShape.ShapeId}");
                }
            }
            TextInput = string.Empty;
            IsTextBoxActive = false;
            Shapes.Remove(_currentTextboxModel);
            _currentTextboxModel = null;
            _currentTextShape = null;
            OnPropertyChanged(nameof(TextBoxVisibility));
            Trace.TraceInformation("Exiting FinalizeTextBox");
        }
        else
        {
            Trace.TraceWarning("FinalizeTextBox called but _currentTextboxModel is null.");
            Trace.TraceInformation("Exiting FinalizeTextBox without changes.");
        }
    }

    /// <summary>
    /// Notifies listeners of property value changes.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected void OnPropertyChanged(string propertyName)
    {
        Trace.TraceInformation($"Property changed: {propertyName}");
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public IShape HoveredShape
    {
        get => _hoveredShape;
        set {
            if (_hoveredShape != value)
            {
                Trace.TraceInformation($"HoveredShape changed from ID: {_hoveredShape?.ShapeId} to ID: {value?.ShapeId}");
                _hoveredShape = value;
                OnPropertyChanged(nameof(HoveredShape));
                OnPropertyChanged(nameof(HoveredShapeDetails));
            }
        }
    }

    /// <summary>
    /// Gets details about the currently hovered shape, such as creator and modifier.
    /// </summary>
    public string HoveredShapeDetails
    {
        get {
            if (HoveredShape == null)
            {
                Trace.TraceInformation("HoveredShapeDetails accessed but no shape is hovered.");
                return string.Empty;
            }
            // Customize the details based on the shape type
            string details =
                $"Created By: {HoveredShape.UserName}\n"
                + $"Last Modified By: {HoveredShape.LastModifiedBy}\n";
            Trace.TraceInformation($"HoveredShapeDetails: {details}");
            return details;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether a shape is currently hovered over.
    /// </summary>
    public bool IsShapeHovered
    {
        get => _isShapeHovered;
        set {
            if (_isShapeHovered != value)
            {
                Trace.TraceInformation($"IsShapeHovered set to: {value}");
                _isShapeHovered = value;
                OnPropertyChanged(nameof(IsShapeHovered));
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether dark mode is enabled.
    /// </summary>
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set {
            if (_isDarkMode != value)
            {
                Trace.TraceInformation($"IsDarkMode changing from {IsDarkMode} to {value}");
                _isDarkMode = value;
                OnPropertyChanged(nameof(IsDarkMode));
                UpdateBackground(IsDarkMode);
                if (value == true)
                {
                    DefaultColor = "White";
                    if (_selectedColor == Colors.Black)
                    {
                        CurrentColor = Colors.White;
                        Trace.TraceInformation("Selected color changed to White due to dark mode.");
                    }
                }
                else
                {
                    DefaultColor = "Black";
                    if (_selectedColor == Colors.Black)
                    {
                        CurrentColor = Colors.Black;
                        Trace.TraceInformation("Selected color changed to Black due to light mode.");
                    }
                }

                // Set manual override flag only if the change is user-initiated
                if (!_isUpdatingDarkModeFromTimer)
                {
                    _isDarkModeManuallySet = true;
                    Trace.TraceInformation("Dark mode manually set by the user.");
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the background color for the page.
    /// </summary>
    public Brush PageBackground
    {
        get => _pageBackground;
        set {
            if (_pageBackground != value)
            {
                Trace.TraceInformation($"PageBackground changed to: {value}");
                _pageBackground = value;
                OnPropertyChanged(nameof(PageBackground));
            }
        }
    }

    /// <summary>
    /// Gets or sets the background color for the canvas.
    /// </summary>
    public Brush CanvasBackground
    {
        get => _canvasBackground;
        set {
            if (_canvasBackground != value)
            {
                Trace.TraceInformation($"CanvasBackground changed to: {value}");
                _canvasBackground = value;
                OnPropertyChanged(nameof(CanvasBackground));
            }
        }
    }
}
