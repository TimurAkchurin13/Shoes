using Avalonia.Controls;
using AvaloniaApplication1.ViewModels;

namespace AvaloniaApplication1.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        
        var viewModel = new LoginWindowViewModel();
        DataContext = viewModel;
        
        viewModel.LoginSuccessful += (s, e) =>
        {
            var productsWindow = new ProductsWindow();
            productsWindow.Show();
            Close();
        };
        
        viewModel.GuestLoginRequested += (s, e) =>
        {
            var productsWindow = new ProductsWindow();
            productsWindow.Show();
            Close();
        };
    }
}

