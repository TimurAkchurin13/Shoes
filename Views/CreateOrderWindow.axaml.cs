using Avalonia.Controls;
using AvaloniaApplication1.ViewModels;

namespace AvaloniaApplication1.Views;

public partial class CreateOrderWindow : Window
{
    public CreateOrderWindow()
    {
        InitializeComponent();
    }
    
    public CreateOrderWindow(CreateOrderWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
        
        viewModel.OrderCreated += (s, e) =>
        {
            Close(true);
        };
        
        viewModel.OrderCancelled += (s, e) =>
        {
            Close(false);
        };
    }
}  