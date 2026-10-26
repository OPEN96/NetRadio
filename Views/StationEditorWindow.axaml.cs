using System;
using Avalonia.Controls;
using NetRadio.Models;
using NetRadio.ViewModels;

namespace NetRadio.Views;

public partial class StationEditorWindow : Window
{
    private readonly StationEditorViewModel _vm;

    public StationEditorWindow()
        : this(new StationEditorViewModel())
    {
    }

    public StationEditorWindow(StationEditorViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(null);
    }

    private void OnSaveClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_vm.TryBuild(out var station))
        {
            Close(station);
        }
    }
}
