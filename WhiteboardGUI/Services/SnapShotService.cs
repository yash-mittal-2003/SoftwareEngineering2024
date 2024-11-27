using SECloud.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WhiteboardGUI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using SECloud.Models;
using System.Net.Http;
using SECloud.Interfaces;
using System.Windows.Data;

namespace WhiteboardGUI.Services;

/// <summary>
/// The SnapShotService class manages the creation, uploading, downloading, and management of snapshots
/// containing shapes in the Whiteboard application.
/// </summary>
public class SnapShotService
{
    private readonly NetworkingService _networkingService;
    private readonly RenderingService _renderingService;
    private readonly UndoRedoService _undoRedoService;
    private ObservableCollection<IShape> _shapes;
    private List<SnapShotDownloadItem> _snapShotDownloadItems = new();
    public event Action OnSnapShotUploaded;
    private ICloud _cloudService;
    int _limit = 5;
    string _userEmail;

    public SnapShotService(NetworkingService networkingService, RenderingService renderingService, ObservableCollection<IShape> shapes, UndoRedoService undoRedoService, string userEmail)
    {
        Trace.TraceInformation("Initializing SnapShotService");
        _networkingService = networkingService;
        _renderingService = renderingService;
        _undoRedoService = undoRedoService;
        _shapes = shapes;
        _userEmail = userEmail;
        InitializeCloudService();
        Trace.TraceInformation("SnapShotService initialized successfully");
    }

    private void InitializeCloudService()
    {
        Trace.TraceInformation("Entering InitializeCloudService");
        ServiceProvider serviceProvider = new ServiceCollection()
        .AddLogging(builder => {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        })
        .AddHttpClient()
        .BuildServiceProvider();

        ILogger<CloudService> logger = serviceProvider.GetRequiredService<ILogger<CloudService>>();
        var httpClient = new HttpClient();

        string baseUrl = "https://secloudapp-2024.azurewebsites.net/api";
        string team = "whiteboard";
        string sasToken = "sp=racwdli&st=2024-11-14T21:02:09Z&se=2024-11-30T05:02:09Z&spr=https&sv=2022-11-02&sr=c&sig=tSw6pO8%2FgqiG2MgU%2FoepmRkFuuJrTerVy%2BDn91Y0WH8%3D";

        _cloudService = new CloudService(baseUrl, team, sasToken, httpClient, logger);
        Trace.TraceInformation("Exiting InitializeCloudService");
    }

    public async Task UploadSnapShot(string snapShotFileName, ObservableCollection<IShape> shapes, bool isTest)
    {
        Trace.TraceInformation($"Entering UploadSnapShot with fileName: {snapShotFileName} and isTest: {isTest}");
        await Task.Run(async () => {
            var snapShot = new SnapShot();
            snapShotFileName = ParseSnapShotName(snapShotFileName, snapShot);
            Debug.WriteLine($"Uploading snapshot '{snapShotFileName}' with {shapes.Count} shapes.");
            SendToCloud(snapShotFileName, snapShot, shapes);

            if (!isTest)
            {
                MessageBox.Show($"Filename '{snapShotFileName}' has been set.", "Filename Set", MessageBoxButton.OK);
            }
        });

        if (!isTest)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => OnSnapShotUploaded?.Invoke());
        }
        Trace.TraceInformation("Exiting UploadSnapShot");
    }

    private async void SendToCloud(string snapShotFileName, SnapShot snapShot, ObservableCollection<IShape> shapes)
    {
        Trace.TraceInformation("Entering SendToCloud");
        CheckLimit();
        snapShot._userID = _userEmail.ToString();
        snapShot._shapes = new ObservableCollection<IShape>(shapes);
        string snapShotSerialized = SerializationService.SerializeSnapShot(snapShot);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(snapShotSerialized));
        ServiceResponse<string> response = await _cloudService.UploadAsync(snapShotFileName + ".json", stream, "application/json");
        Debug.WriteLine("RESPONSE:" + response.ToString());
        Trace.TraceInformation("Exiting SendToCloud");
    }

    private void CheckLimit()
    {
        Trace.TraceInformation("Entering CheckLimit");
        while (_snapShotDownloadItems.Count >= _limit)
        {
            SnapShotDownloadItem lastSnapName = FindLastSnap();
            DeleteSnap(lastSnapName);
        }
        Trace.TraceInformation("Exiting CheckLimit");
    }

    private void DeleteSnap(SnapShotDownloadItem lastSnap)
    {
        Trace.TraceInformation($"Entering DeleteSnap with fileName: {lastSnap.FileName}");
        _snapShotDownloadItems.RemoveAll(item => item.FileName == lastSnap.FileName && item.Time == lastSnap.Time);
        string snapName = $"{lastSnap.FileName}_{((DateTimeOffset)lastSnap.Time.ToUniversalTime()).ToUnixTimeSeconds()}";
        _cloudService.DeleteAsync(snapName + ".json");
        Trace.TraceInformation("Exiting DeleteSnap");
    }

    private SnapShotDownloadItem FindLastSnap()
    {
        Trace.TraceInformation("Entering FindLastSnap");
        SnapShotDownloadItem result = _snapShotDownloadItems.OrderBy(item => item.Time).FirstOrDefault();
        Trace.TraceInformation("Exiting FindLastSnap");
        return result;
    }

    private string ParseSnapShotName(string snapShotFileName, SnapShot snapShot)
    {
        Trace.TraceInformation("Entering ParseSnapShotName");
        if (string.IsNullOrWhiteSpace(snapShotFileName))
        {
            DateTime currentDateTime = DateTime.Now;
            snapShotFileName = currentDateTime.ToString("yyyyMMdd-HHmmss");
        }
        DateTimeOffset currentDateTimeEpoch = DateTimeOffset.UtcNow;
        snapShot._dateTime = currentDateTimeEpoch.LocalDateTime;
        long epochTime = currentDateTimeEpoch.ToUnixTimeSeconds();
        string newSnapShotFileName = $"{snapShotFileName}_{epochTime}";
        snapShot._fileName = snapShotFileName;

        _snapShotDownloadItems.Add(new SnapShotDownloadItem(snapShotFileName, snapShot._dateTime));
        Trace.TraceInformation("Exiting ParseSnapShotName");
        return newSnapShotFileName;
    }

    public async Task<List<SnapShotDownloadItem>> GetSnaps(string v, bool isInit)
    {
        Trace.TraceInformation($"Entering GetSnaps with isInit: {isInit}");
        if (isInit)
        {
            ServiceResponse<JsonSearchResponse> response = await _cloudService.SearchJsonFilesAsync("_userID", _userEmail.ToString());
            if (response != null && response.Data != null && response.Data.Matches != null)
            {
                PopulateSnapShotDownloadItems(response);
                Trace.TraceInformation("Exiting GetSnaps with populated SnapShotDownloadItems");
                return _snapShotDownloadItems;
            }
            Trace.TraceWarning("GetSnaps: No snapshots found");
            return new List<SnapShotDownloadItem>();
        }
        Trace.TraceInformation("Exiting GetSnaps with existing SnapShotDownloadItems");
        return _snapShotDownloadItems;
    }

    public async void DownloadSnapShot(SnapShotDownloadItem selectedDownloadItem)
    {
        Trace.TraceInformation($"Entering DownloadSnapShot with fileName: {selectedDownloadItem.FileName}");
        ObservableCollection<IShape> snapShot = await GetSnapShot(selectedDownloadItem);
        _renderingService.RenderShape(null, "CLEAR");
        AddShapes(snapShot);
        _undoRedoService._redoList.Clear();
        _undoRedoService._undoList.Clear();
        Trace.TraceInformation("Exiting DownloadSnapShot");
    }

    private void AddShapes(ObservableCollection<IShape> snapShot)
    {
        Trace.TraceInformation("Entering AddShapes");
        foreach (IShape shape in snapShot)
        {
            _shapes.Add(shape);
            _renderingService.RenderShape(shape, "DOWNLOAD");
            Debug.WriteLine($"Added Shape {shape.GetType()}");
        }
        Trace.TraceInformation("Exiting AddShapes");
    }

    public bool IsValidFilename(string filename)
    {
        Trace.TraceInformation($"Checking validity of filename: {filename}");
        return !_snapShotDownloadItems.Any(item => item.FileName == filename);
    }

    private async Task<ObservableCollection<IShape>> GetSnapShot(SnapShotDownloadItem selectedDownloadItem)
    {
        Trace.TraceInformation($"Entering GetSnapShot with fileName: {selectedDownloadItem.FileName}");
        string snapName = $"{selectedDownloadItem.FileName}_{((DateTimeOffset)selectedDownloadItem.Time.ToUniversalTime()).ToUnixTimeSeconds()}.json";
        ServiceResponse<Stream> snapdownload = await _cloudService.DownloadAsync(snapName);
        if (snapdownload.Success && snapdownload.Data != null)
        {
            var sr = new StreamReader(snapdownload.Data);
            string snapData = sr.ReadToEnd();
            Trace.TraceInformation("Exiting GetSnapShot with success");
            return SerializationService.DeserializeSnapShot(snapData)._shapes;
        }
        Trace.TraceWarning("GetSnapShot failed, returning empty collection");
        return new ObservableCollection<IShape>(_shapes);
    }

    private void PopulateSnapShotDownloadItems(ServiceResponse<JsonSearchResponse> response)
    {
        Trace.TraceInformation("Entering PopulateSnapShotDownloadItems");
        _snapShotDownloadItems = response.Data.Matches
            .Select(match => {
                string fileName = SerializationService.DeserializeSnapShot(match.Content.ToString())._fileName;
                DateTime time = SerializationService.DeserializeSnapShot(match.Content.ToString())._dateTime;
                return new SnapShotDownloadItem(fileName, time);
            })
            .ToList();
        Trace.TraceInformation("Exiting PopulateSnapShotDownloadItems");
    }
}
