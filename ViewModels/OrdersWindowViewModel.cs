using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AvaloniaApplication1.Models;
using AvaloniaApplication1.Services;
using System.Diagnostics.CodeAnalysis;

namespace AvaloniaApplication1.ViewModels;

public partial class OrdersWindowViewModel : ViewModelBase
{
    private readonly DatabaseService _databaseService;
   
    private readonly List<Order> _allOrders = new();
    private const string AllOrderTypesOption = "Все виды";
    
    [ObservableProperty]
    private ObservableCollection<Order> _orders = new();
    
    [ObservableProperty]
    private Order? _selectedOrder;
    
    [ObservableProperty]
    private ObservableCollection<OrderDetail> _orderDetails = new();
    
    [ObservableProperty]
    private bool _isLoading = false;
    
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    // Avalonia DatePicker.SelectedDate использует DateTimeOffset?
    [ObservableProperty]
    private DateTimeOffset? _startDate;

    [ObservableProperty]
    private DateTimeOffset? _endDate;

    [ObservableProperty]
    private string? _selectedOrderType;
    
    [ObservableProperty]
    private string? _editStatus;
    
    public bool CanEdit => CurrentUserService.Instance.CanEditOrders;
    public bool CanChangeStatus => CurrentUserService.Instance.CanEditOrders;
    public bool CanGenerateReport => CurrentUserService.Instance.IsAdmin;
    public bool CanUseFilters => !_showOnlyMyOrders && (CurrentUserService.Instance.IsAdmin || CurrentUserService.Instance.IsManager);
    
    public bool HasNoOrders => !IsLoading && Orders.Count == 0;
    public ObservableCollection<string> OrderTypes { get; } = new();
    
    private readonly bool _showOnlyMyOrders;
    
    public OrdersWindowViewModel(bool showOnlyMyOrders = false)
    {
        _databaseService = new DatabaseService();
        _showOnlyMyOrders = showOnlyMyOrders;
        OrderTypes.Add(AllOrderTypesOption);
        SelectedOrderType = AllOrderTypesOption;
        System.Diagnostics.Debug.WriteLine($"=== OrdersWindowViewModel создан ===");
        System.Diagnostics.Debug.WriteLine($"showOnlyMyOrders: {showOnlyMyOrders}");
        _ = LoadOrders();
    }
    
    
    private async Task LoadOrders()
    {
        IsLoading = true;
        StatusMessage = "Загрузка заказов...";
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(HasNoOrders));
        
        try
        {
            List<Order> orders;
            
            if (_showOnlyMyOrders && CurrentUserService.Instance.CurrentUser != null)
            {
                // Загружаем только заказы текущего клиента
                orders = await _databaseService.GetClientOrders(CurrentUserService.Instance.CurrentUser.Id);
                StatusMessage = "Загрузка ваших заказов...";
                System.Diagnostics.Debug.WriteLine($"Загрузка заказов клиента ID: {CurrentUserService.Instance.CurrentUser.Id}");
            }
            else
            {
                // Загружаем все заказы (для админа и менеджера)
                orders = await _databaseService.GetAllOrders();
                StatusMessage = "Загрузка всех заказов...";
                System.Diagnostics.Debug.WriteLine($"Загрузка всех заказов (не клиент)");
            }
            
            System.Diagnostics.Debug.WriteLine($"Получено заказов из БД: {orders?.Count ?? 0}");
            
            _allOrders.Clear();
            if (orders != null)
            {
                _allOrders.AddRange(orders);
            }

            if (_allOrders.Count > 0)
            {
                UpdateOrderTypes();
                ApplyFilters();
                System.Diagnostics.Debug.WriteLine($"Добавлено заказов в коллекцию: {Orders.Count}");
                System.Diagnostics.Debug.WriteLine($"Orders.Count после добавления: {Orders.Count}");
            }
            else
            {
                if (_showOnlyMyOrders)
                {
                    StatusMessage = "У вас пока нет заказов.";
                }
                else
                {
                    StatusMessage = "Заказы не найдены. Убедитесь, что данные загружены в БД.";
                }
                System.Diagnostics.Debug.WriteLine("Заказы не найдены в БД");
                OnPropertyChanged(nameof(HasNoOrders));
                OnPropertyChanged(nameof(StatusMessage));
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"ОШИБКА загрузки заказов: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            OnPropertyChanged(nameof(StatusMessage));
            OnPropertyChanged(nameof(HasNoOrders));
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsLoading));
            OnPropertyChanged(nameof(HasNoOrders));
            OnPropertyChanged(nameof(Orders));
            System.Diagnostics.Debug.WriteLine($"LoadOrders завершен. IsLoading={IsLoading}, Orders.Count={Orders.Count}, HasNoOrders={HasNoOrders}");
        }
    }
    
    partial void OnSelectedOrderChanged(Order? value)
    {
        if (value != null)
        {
            _ = LoadOrderDetails(value.OrderNumber);
            EditStatus = value.OrderStatus;
        }
        else
        {
            OrderDetails.Clear();
            EditStatus = null;
        }
    }
    
    private async Task LoadOrderDetails(int orderNumber)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"Загрузка деталей заказа №{orderNumber}");
            var details = await _databaseService.GetOrderDetails(orderNumber);
            System.Diagnostics.Debug.WriteLine($"Получено деталей из БД: {details?.Count ?? 0}");
            
            OrderDetails.Clear();
            if (details != null && details.Count > 0)
            {
                foreach (var detail in details)
                {
                    OrderDetails.Add(detail);
                }
                System.Diagnostics.Debug.WriteLine($"Добавлено деталей в коллекцию: {OrderDetails.Count}");
                
                // Принудительное обновление UI
                OnPropertyChanged(nameof(OrderDetails));
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Детали заказа не найдены в БД");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки деталей: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Ошибка загрузки деталей заказа: {ex}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
    
    public event EventHandler<Order>? DeleteOrderRequested;
    public event EventHandler<Order>? EditOrderRequested;
    public event EventHandler? CreateOrderRequested;
    
    [RelayCommand]
    private void CreateOrder()
    {
        CreateOrderRequested?.Invoke(this, EventArgs.Empty);
    }
    
    [RelayCommand]
    private void EditOrder()
    {
        if (SelectedOrder == null)
        {
            StatusMessage = "Выберите заказ для редактирования";
            return;
        }
        
        EditOrderRequested?.Invoke(this, SelectedOrder);
    }
    
    [RelayCommand]
    private void DeleteOrder()
    {
        if (SelectedOrder == null)
        {
            StatusMessage = "Выберите заказ для удаления";
            return;
        }
        
        DeleteOrderRequested?.Invoke(this, SelectedOrder);
    }
    
    public async Task DeleteOrderConfirmed(Order order)
    {
        try
        {
            var result = await _databaseService.DeleteOrder(order.OrderNumber);
            if (result)
            {
                StatusMessage = "Заказ удален";
                await LoadOrders();
            }
            else
            {
                StatusMessage = "Не удалось удалить заказ";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {ex.Message}";
        }
    }
    
    [RelayCommand]
    public async Task Refresh()
    {
        System.Diagnostics.Debug.WriteLine("=== Команда Refresh вызвана ===");
        await LoadOrders();
    }

    [RelayCommand]
    private void ApplyFiltersFromUI()
    {
        ApplyFilters();
    }

    [RelayCommand]
    private void ClearFilters()
    {
        StartDate = null;
        EndDate = null;
        SelectedOrderType = AllOrderTypesOption;
        ApplyFilters();
    }

    [RelayCommand]
    private async Task UpdateOrderStatus()
    {
        if (!CanChangeStatus)
        {
            StatusMessage = "Изменение заказа доступно только администратору.";
            return;
        }
        
        if (SelectedOrder == null)
        {
            StatusMessage = "Выберите заказ для редактирования.";
            return;
        }
        
        if (string.IsNullOrWhiteSpace(EditStatus))
        {
            StatusMessage = "Выберите новый статус заказа.";
            return;
        }
        
        if (string.Equals(SelectedOrder.OrderStatus, EditStatus, StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = "Статус заказа не изменился.";
            return;
        }
        
        try
        {
            var success = await _databaseService.UpdateOrderStatus(SelectedOrder.OrderNumber, EditStatus);
            if (!success)
            {
                StatusMessage = "Не удалось обновить статус заказа.";
                return;
            }
            
            // Обновляем данные в текущем и общем списках
            SelectedOrder.OrderStatus = EditStatus;
            var inAll = _allOrders.FirstOrDefault(o => o.OrderNumber == SelectedOrder.OrderNumber);
            if (inAll != null)
            {
                inAll.OrderStatus = EditStatus;
            }
            
            ApplyFilters();
            StatusMessage = $"Статус заказа №{SelectedOrder.OrderNumber} изменен на \"{EditStatus}\".";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка обновления статуса: {ex.Message}";
        }
    }

    partial void OnStartDateChanged(DateTimeOffset? value)
    {
        if (!_showOnlyMyOrders && CanUseFilters)
        {
            ApplyFilters();
        }
    }

    partial void OnEndDateChanged(DateTimeOffset? value)
    {
        if (!_showOnlyMyOrders && CanUseFilters)
        {
            ApplyFilters();
        }
    }

    partial void OnSelectedOrderTypeChanged(string? value)
    {
        if (!_showOnlyMyOrders && CanUseFilters)
        {
            ApplyFilters();
        }
    }

    private void UpdateOrderTypes()
    {
        var previousSelection = SelectedOrderType;
        OrderTypes.Clear();
        OrderTypes.Add(AllOrderTypesOption);

        foreach (var type in _allOrders
                     .Select(o => o.OrderStatus)
                     .Where(s => !string.IsNullOrWhiteSpace(s))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(s => s))
        {
            OrderTypes.Add(type);
        }

        if (!string.IsNullOrWhiteSpace(previousSelection) &&
            OrderTypes.Any(t => string.Equals(t, previousSelection, StringComparison.OrdinalIgnoreCase)))
        {
            SelectedOrderType = OrderTypes.First(t =>
                string.Equals(t, previousSelection, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            SelectedOrderType = AllOrderTypesOption;
        }
    }

    private void ApplyFilters()
    {
        if (_allOrders.Count == 0)
        {
            Orders.Clear();
            OnPropertyChanged(nameof(HasNoOrders));
            return;
        }

        IEnumerable<Order> query = _allOrders;

        if (StartDate.HasValue)
        {
            var start = StartDate.Value.Date;
            query = query.Where(o => o.OrderDate.Date >= start);
        }

        if (EndDate.HasValue)
        {
            var end = EndDate.Value.Date;
            query = query.Where(o => o.OrderDate.Date <= end);
        }

        if (!IsDefaultOrderTypeSelected)
        {
            query = query.Where(o => string.Equals(o.OrderStatus, SelectedOrderType, StringComparison.OrdinalIgnoreCase));
        }

        UpdateOrdersCollection(query);
        var filteredCount = Orders.Count;

        if (filteredCount == 0)
        {
            StatusMessage = "Заказы не найдены по текущим фильтрам.";
        }
        else if (FiltersApplied())
        {
            StatusMessage = $"Отфильтровано заказов: {filteredCount} из {_allOrders.Count}";
        }
        else
        {
            StatusMessage = $"Загружено заказов: {filteredCount}";
        }
    }

    private void UpdateOrdersCollection(IEnumerable<Order> orders)
    {
        Orders.Clear();
        foreach (var order in orders)
        {
            Orders.Add(order);
        }

        OnPropertyChanged(nameof(HasNoOrders));
        OnPropertyChanged(nameof(Orders));
    }

    private bool IsDefaultOrderTypeSelected =>
        string.IsNullOrWhiteSpace(SelectedOrderType) ||
        string.Equals(SelectedOrderType, AllOrderTypesOption, StringComparison.OrdinalIgnoreCase);

    private bool FiltersApplied() =>
        StartDate.HasValue || EndDate.HasValue || !IsDefaultOrderTypeSelected;
}

