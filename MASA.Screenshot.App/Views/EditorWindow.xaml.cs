using System.Drawing;
using System.Windows;
using System.Windows.Input;
using MASA.Screenshot.App.ViewModels;
using MASA.Screenshot.Core.Interfaces;

namespace MASA.Screenshot.App.Views;

public partial class EditorWindow : Window
{
    private readonly EditorViewModel _viewModel;
    private readonly IImageProcessingService _imageProcessor;

    public EditorWindow(EditorViewModel viewModel, IImageProcessingService imageProcessor)
    {
        _viewModel = viewModel;
        _imageProcessor = imageProcessor;
        DataContext = _viewModel;
        InitializeComponent();

        _viewModel.RequestUndo += () => CanvasControl.Undo();
        _viewModel.RequestRedo += () => CanvasControl.Redo();
        _viewModel.RequestClear += () => CanvasControl.ClearAnnotations();
        _viewModel.RequestFlatten += () => CanvasControl.GetFlattenedBitmap(_imageProcessor);

        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(EditorViewModel.SelectedTool))
            {
                CanvasControl.ActiveTool = _viewModel.SelectedTool;
            }
        };

        CanvasControl.MouseHoverPixel += (s, pt) =>
        {
            _viewModel.UpdateHoverColor(pt.X, pt.Y);
        };
    }

    public void LoadBitmap(Bitmap bitmap)
    {
        _viewModel.LoadBitmap(bitmap);
        CanvasControl.SetImage(bitmap);
    }

    private void CanvasControl_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var pos = e.GetPosition(CanvasControl);
        _viewModel.UpdateHoverColor((int)pos.X, (int)pos.Y);
    }
}
