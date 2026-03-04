using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using AvaloniaApplication1.Models;
using AvaloniaApplication1.Services;
using AvaloniaApplication1.ViewModels;

namespace AvaloniaApplication1.Views;

public partial class OrdersWindow : Window
{
    private OrdersWindowViewModel? _viewModel;
    
    // Публичный конструктор без параметров для XAML loader
    public OrdersWindow() : this(false)
    {
    }
    
    public OrdersWindow(bool showOnlyMyOrders)
    {
        InitializeComponent();
        
        _viewModel = new OrdersWindowViewModel(showOnlyMyOrders);
        DataContext = _viewModel;
        
        // Изменяем заголовок для клиента
        if (showOnlyMyOrders)
        {
            Title = "Мои заказы - ООО Обувь";
        }
        
        // Отладочная информация
        System.Diagnostics.Debug.WriteLine($"=== OrdersWindow открыт ===");
        System.Diagnostics.Debug.WriteLine($"showOnlyMyOrders: {showOnlyMyOrders}");
        System.Diagnostics.Debug.WriteLine($"Пользователь: {CurrentUserService.Instance.CurrentUser?.Login ?? "null"}");
        System.Diagnostics.Debug.WriteLine($"Роль: {CurrentUserService.Instance.CurrentUser?.RoleName ?? "null"}");
        
        _viewModel.DeleteOrderRequested += async (s, order) =>
        {
            var confirmWindow = new ConfirmDeleteWindow(new ConfirmDeleteWindowViewModel(
                $"Заказ №{order.OrderNumber}", "заказ"));
            var result = await confirmWindow.ShowDialog<bool?>(this);
            if (result == true)
            {
                await _viewModel.DeleteOrderConfirmed(order);
            }
        };

        _viewModel.CreateOrderRequested += async (s, e) =>
        {
            var createWindow = new CreateOrderWindow(new CreateOrderWindowViewModel());
            var result = await createWindow.ShowDialog<bool?>(this);
            if (result == true)
            {
                await _viewModel.Refresh();
            }
        };

        _viewModel.EditOrderRequested += async (s, order) =>
        {
            var editWindow = new EditOrderWindow(new EditOrderWindowViewModel(order));
            var result = await editWindow.ShowDialog<bool?>(this);
            if (result == true)
            {
                await _viewModel.Refresh();
            }
        };
    }

    public void OnBackClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    public void OrderCard_Tapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel == null)
        {
            return;
        }

        if (sender is Control control && control.DataContext is Order order)
        {
            _viewModel.SelectedOrder = order;
        }
    }
}

