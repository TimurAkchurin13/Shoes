using Avalonia.Controls;
using System;
using AvaloniaApplication1.ViewModels;

namespace AvaloniaApplication1.Views;

public partial class ConfirmDeleteWindow : Window
{
    public ConfirmDeleteWindow()
    {
        InitializeComponent();
    }
    
    public ConfirmDeleteWindow(ConfirmDeleteWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
        
        viewModel.DeleteConfirmed += (s, confirmed) =>
        {
            Close(confirmed);
        };
    }
}

