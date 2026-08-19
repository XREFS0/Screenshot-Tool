using System.Drawing;
using System.Windows;
using MASA.Screenshot.App.ViewModels;

namespace MASA.Screenshot.App.Views;

public partial class HistoryWindow : Window
{
    private readonly HistoryViewModel _viewModel;
    public event Action<Bitmap>? OpenInEditorRequested;

    public HistoryWindow(HistoryViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();

        _viewModel.OpenInEditorRequested += (bmp) =>
        {
            OpenInEditorRequested?.Invoke(bmp);
        };
    }

    public void RefreshList()
    {
        _viewModel.LoadItems();
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        RefreshList();
    }
}
