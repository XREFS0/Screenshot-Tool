using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MASA.Screenshot.Core.Interfaces;
using MASA.Screenshot.Core.Models;

namespace MASA.Screenshot.App.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly IHistoryService _historyService;
    private readonly IClipboardService _clipboardService;
    private readonly IImageProcessingService _imageProcessor;

    [ObservableProperty]
    private ObservableCollection<HistoryItem> _items = new();

    [ObservableProperty]
    private HistoryItem? _selectedItem;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public event Action<Bitmap>? OpenInEditorRequested;

    public HistoryViewModel(
        IHistoryService historyService,
        IClipboardService clipboardService,
        IImageProcessingService imageProcessor)
    {
        _historyService = historyService;
        _clipboardService = clipboardService;
        _imageProcessor = imageProcessor;
    }

    public void LoadItems()
    {
        Items.Clear();
        foreach (var item in _historyService.Items)
        {
            Items.Add(item);
        }
        StatusMessage = $"{Items.Count} screenshots in history";
    }

    [RelayCommand]
    private async Task CopySelectedAsync(HistoryItem? item)
    {
        var target = item ?? SelectedItem;
        if (target == null) return;

        var bmp = await _historyService.LoadImageAsync(target.FilePath);
        if (bmp != null)
        {
            await _clipboardService.CopyImageAsync(bmp);
            StatusMessage = "Copied screenshot to clipboard";
        }
    }

    [RelayCommand]
    private async Task EditSelectedAsync(HistoryItem? item)
    {
        var target = item ?? SelectedItem;
        if (target == null) return;

        var bmp = await _historyService.LoadImageAsync(target.FilePath);
        if (bmp != null)
        {
            OpenInEditorRequested?.Invoke(bmp);
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync(HistoryItem? item)
    {
        var target = item ?? SelectedItem;
        if (target == null) return;

        await _historyService.DeleteItemAsync(target.Id);
        Items.Remove(target);
        StatusMessage = "Screenshot deleted";
    }

    [RelayCommand]
    private async Task ClearAllHistoryAsync()
    {
        await _historyService.ClearAllAsync();
        Items.Clear();
        StatusMessage = "All history cleared";
    }

    [RelayCommand]
    private void OpenFileLocation(HistoryItem? item)
    {
        var target = item ?? SelectedItem;
        if (target == null || !File.Exists(target.FilePath)) return;

        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{target.FilePath}\"");
    }
}
