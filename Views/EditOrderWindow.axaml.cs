using Avalonia.Controls;
using AvaloniaApplication1.ViewModels;

namespace AvaloniaApplication1.Views;

public partial class EditOrderWindow : Window
{
    public EditOrderWindow()
    {
        InitializeComponent();
    }
    
    public EditOrderWindow(EditOrderWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
        
        viewModel.OrderSaved += (s, e) =>
        {
            Close(true);
        };
        
        viewModel.OrderCancelled += (s, e) =>
        {
            Close(false);
        };
    }
}









