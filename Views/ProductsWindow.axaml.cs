using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaApplication1.Models;
using AvaloniaApplication1.Services;
using AvaloniaApplication1.ViewModels;
using AvaloniaApplication1.Views;

namespace AvaloniaApplication1.Views;

public partial class ProductsWindow : Window
{
    private ProductsWindowViewModel? _viewModel;
    
    public ProductsWindow()
    {
        InitializeComponent();
        
        _viewModel = new ProductsWindowViewModel();
        DataContext = _viewModel;
        
        // Обновляем права при открытии окна (важно для клиента!)
        _viewModel.UpdatePermissions();
        
        // Отладочная информация
        System.Diagnostics.Debug.WriteLine($"=== ProductsWindow открыт ===");
        System.Diagnostics.Debug.WriteLine($"Пользователь: {CurrentUserService.Instance.CurrentUser?.Login ?? "null"}");
        System.Diagnostics.Debug.WriteLine($"Роль: {CurrentUserService.Instance.CurrentUser?.RoleName ?? "null"}");
        System.Diagnostics.Debug.WriteLine($"IsClient: {CurrentUserService.Instance.IsClient}");
        System.Diagnostics.Debug.WriteLine($"CanCreateOrder (Service): {CurrentUserService.Instance.CanCreateOrder}");
        System.Diagnostics.Debug.WriteLine($"CanCreateOrder (ViewModel): {_viewModel.CanCreateOrder}");
        System.Diagnostics.Debug.WriteLine($"CanViewMyOrders: {CurrentUserService.Instance.CanViewMyOrders}");
        
        _viewModel.OrdersRequested += (s, e) =>
        {
            var ordersWindow = new OrdersWindow(false); // Все заказы
            ordersWindow.Show();
        };
        
        _viewModel.MyOrdersRequested += (s, e) =>
        {
            var ordersWindow = new OrdersWindow(true); // Только заказы клиента
            ordersWindow.Show();
        };
        
        _viewModel.AddProductRequested += async (s, product) =>
        {
            var editWindow = new ProductEditWindow(new ProductEditWindowViewModel(null));
            var result = await editWindow.ShowDialog<bool?>(this);
            if (result == true)
            {
                await _viewModel.Refresh();
            }
        };
        
        _viewModel.EditProductRequested += async (s, product) =>
        {
            var editWindow = new ProductEditWindow(new ProductEditWindowViewModel(product));
            var result = await editWindow.ShowDialog<bool?>(this);
            if (result == true)
            {
                await _viewModel.Refresh();
            }
        };
        
        _viewModel.DeleteProductRequested += async (s, product) =>
        {
            var confirmWindow = new ConfirmDeleteWindow(new ConfirmDeleteWindowViewModel(
                product.ProductName, "товар"));
            var result = await confirmWindow.ShowDialog<bool?>(this);
            if (result == true)
            {
                await _viewModel.DeleteProductConfirmed(product);
            }
        };
        
        _viewModel.CreateOrderRequested += async (s, product) =>
        {
            try
            {
                var orderWindow = new CreateOrderWindow(new CreateOrderWindowViewModel(product));
                var result = await orderWindow.ShowDialog<bool?>(this);
                if (result == true)
                {
                    _viewModel.StatusMessage = "Заказ успешно создан!";
                    // Можно обновить список товаров, если нужно
                }
            }
            catch (Exception ex)
            {
                _viewModel.StatusMessage = $"Ошибка открытия окна заказа: {ex.Message}";
            }
        };
    }

    private void ProductCard_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.DataContext is Product product)
        {
            if (_viewModel != null)
            {
                _viewModel.SelectedProduct = product;
            }
        }
    }
    
    private async void ProductCard_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.DataContext is Product product)
        {
            if (_viewModel != null && _viewModel.CanCreateOrder)
            {
                _viewModel.SelectedProduct = product;
                // Автоматически открываем окно оформления заказа при двойном клике
                var orderWindow = new CreateOrderWindow(new CreateOrderWindowViewModel(product));
                await orderWindow.ShowDialog<bool?>(this);
            }
        }
    }
    
    private void BackButton_Click(object? sender, RoutedEventArgs e)
    {
        CurrentUserService.Instance.CurrentUser = null;
        CurrentUserService.Instance.IsGuest = false;
        
        var loginWindow = new LoginWindow();
        loginWindow.Show();
        Close();
    }
}

