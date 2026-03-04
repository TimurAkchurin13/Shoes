using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AvaloniaApplication1.Models;
using AvaloniaApplication1.Services;

namespace AvaloniaApplication1.ViewModels;

public partial class EditOrderWindowViewModel : ViewModelBase
{
    private readonly DatabaseService _databaseService;
    private readonly Order _originalOrder;
    
    [ObservableProperty]
    private int _orderNumber;
    
    [ObservableProperty]
    private DateTimeOffset? _orderDate;
    
    [ObservableProperty]
    private DateTimeOffset? _deliveryDate;
    
    [ObservableProperty]
    private ObservableCollection<PickupPoint> _pickupPoints = new();
    
    [ObservableProperty]
    private PickupPoint? _selectedPickupPoint;
    
    [ObservableProperty]
    private ObservableCollection<User> _clients = new();
    
    [ObservableProperty]
    private User? _selectedClient;
    
    [ObservableProperty]
    private string _receiptCode = string.Empty;
    
    [ObservableProperty]
    private ObservableCollection<string> _orderStatuses = new();
    
    [ObservableProperty]
    private string _selectedOrderStatus = string.Empty;
    
    [ObservableProperty]
    private ObservableCollection<OrderDetailItem> _orderDetails = new();
    
    [ObservableProperty]
    private decimal _totalAmount;
    
    [ObservableProperty]
    private string _statusMessage = string.Empty;
    
    [ObservableProperty]
    private bool _isLoading = false;
    
    [ObservableProperty]
    private ObservableCollection<Product> _availableProducts = new();
    
    [ObservableProperty]
    private Product? _selectedProduct;
    
    public event EventHandler? OrderSaved;
    public event EventHandler? OrderCancelled;
    
    public EditOrderWindowViewModel(Order order)
    {
        _databaseService = new DatabaseService();
        _originalOrder = order;
        OrderNumber = order.OrderNumber;
        OrderDate = new DateTimeOffset(order.OrderDate);
        DeliveryDate = new DateTimeOffset(order.DeliveryDate);
        ReceiptCode = order.ReceiptCode;
        SelectedOrderStatus = order.OrderStatus;
        TotalAmount = order.TotalAmount;
        
        OrderStatuses.Add("Новый");
        OrderStatuses.Add("В обработке");
        OrderStatuses.Add("Завершен");
        OrderStatuses.Add("Отменен");
        
        _ = LoadData();
    }
    
    private async Task LoadData()
    {
        IsLoading = true;
        try
        {
            // Загружаем пункты выдачи
            var points = await _databaseService.GetAllPickupPoints();
            PickupPoints.Clear();
            foreach (var point in points)
            {
                PickupPoints.Add(point);
            }
            SelectedPickupPoint = PickupPoints.FirstOrDefault(p => p.Id == _originalOrder.PickupPointId);
            
            // Загружаем клиентов
            var clients = await _databaseService.GetAllUsers();
            Clients.Clear();
            foreach (var client in clients.Where(u => u.RoleId == 3)) // Только клиенты
            {
                Clients.Add(client);
            }
            SelectedClient = Clients.FirstOrDefault(c => c.Id == _originalOrder.ClientId);
            
            // Загружаем товары для добавления
            var products = await _databaseService.GetAllProducts();
            AvailableProducts.Clear();
            foreach (var product in products)
            {
                AvailableProducts.Add(product);
            }
            
            // Загружаем детали заказа
            var details = await _databaseService.GetOrderDetails(OrderNumber);
            OrderDetails.Clear();
            foreach (var detail in details)
            {
                var product = AvailableProducts.FirstOrDefault(p => p.Article == detail.Article);
                OrderDetails.Add(new OrderDetailItem
                {
                    Id = detail.Id,
                    Article = detail.Article,
                    ProductName = detail.ProductName,
                    Quantity = detail.Quantity,
                    UnitPrice = detail.UnitPrice,
                    Discount = detail.Discount,
                    TotalPrice = detail.TotalPrice,
                    Product = product
                });
            }
            
            UpdateTotal();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private void AddProduct()
    {
        if (SelectedProduct == null)
        {
            StatusMessage = "Выберите товар для добавления";
            return;
        }
        
        // Проверяем, не добавлен ли уже этот товар
        if (OrderDetails.Any(d => d.Article == SelectedProduct.Article))
        {
            StatusMessage = "Этот товар уже добавлен в заказ";
            return;
        }
        
        var detail = new OrderDetailItem
        {
            Article = SelectedProduct.Article,
            ProductName = SelectedProduct.ProductName,
            Quantity = 1,
            UnitPrice = SelectedProduct.Price,
            Discount = SelectedProduct.CurrentDiscount ?? 0,
            Product = SelectedProduct
        };
        detail.UpdateTotalPrice();
        
        OrderDetails.Add(detail);
        UpdateTotal();
        StatusMessage = string.Empty;
    }
    
    [RelayCommand]
    private void RemoveProduct(OrderDetailItem? item)
    {
        if (item == null)
            return;
        
        OrderDetails.Remove(item);
        UpdateTotal();
    }
    
    [RelayCommand]
    private void IncreaseQuantity(OrderDetailItem? item)
    {
        if (item == null || item.Product == null)
            return;
        
        if (item.Quantity < item.Product.StockQuantity)
        {
            item.Quantity++;
            item.UpdateTotalPrice();
            UpdateTotal();
        }
        else
        {
            StatusMessage = "Недостаточно товара на складе";
        }
    }
    
    [RelayCommand]
    private void DecreaseQuantity(OrderDetailItem? item)
    {
        if (item == null)
            return;
        
        if (item.Quantity > 1)
        {
            item.Quantity--;
            item.UpdateTotalPrice();
            UpdateTotal();
        }
    }
    
    private void UpdateTotal()
    {
        TotalAmount = OrderDetails.Sum(d => d.TotalPrice);
    }
    
    [RelayCommand]
    private async Task Save()
    {
        if (SelectedPickupPoint == null)
        {
            StatusMessage = "Выберите пункт выдачи";
            return;
        }
        
        if (SelectedClient == null)
        {
            StatusMessage = "Выберите клиента";
            return;
        }
        
        if (string.IsNullOrWhiteSpace(SelectedOrderStatus))
        {
            StatusMessage = "Выберите статус заказа";
            return;
        }
        
        if (OrderDetails.Count == 0)
        {
            StatusMessage = "Добавьте хотя бы один товар в заказ";
            return;
        }
        
        if (DeliveryDate.HasValue && OrderDate.HasValue && DeliveryDate.Value < OrderDate.Value)
        {
            StatusMessage = "Дата доставки не может быть раньше даты заказа";
            return;
        }
        
        if (!OrderDate.HasValue)
        {
            StatusMessage = "Укажите дату заказа";
            return;
        }
        
        if (!DeliveryDate.HasValue)
        {
            StatusMessage = "Укажите дату доставки";
            return;
        }
        
        IsLoading = true;
        StatusMessage = "Сохранение изменений...";
        
        try
        {
            var order = new Order
            {
                OrderNumber = OrderNumber,
                OrderDate = OrderDate.Value.DateTime,
                DeliveryDate = DeliveryDate.Value.DateTime,
                PickupPointId = SelectedPickupPoint.Id,
                ClientId = SelectedClient.Id,
                ReceiptCode = ReceiptCode,
                OrderStatus = SelectedOrderStatus,
                TotalAmount = TotalAmount
            };
            
            var details = OrderDetails.Select(d => new OrderDetail
            {
                Id = d.Id,
                OrderNumber = OrderNumber,
                Article = d.Article,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                Discount = d.Discount,
                TotalPrice = d.TotalPrice
            }).ToList();
            
            var success = await _databaseService.UpdateOrder(order, details);
            
            if (success)
            {
                StatusMessage = "Заказ успешно обновлен!";
                await Task.Delay(1000);
                OrderSaved?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                StatusMessage = "Не удалось обновить заказ";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private void Cancel()
    {
        OrderCancelled?.Invoke(this, EventArgs.Empty);
    }
}

public class OrderDetailItem : ObservableObject
{
    public int Id { get; set; }
    public string Article { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Product? Product { get; set; }
    
    private int _quantity = 1;
    public int Quantity
    {
        get => _quantity;
        set
        {
            SetProperty(ref _quantity, value);
            UpdateTotalPrice();
        }
    }
    
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    
    private decimal _totalPrice;
    public decimal TotalPrice
    {
        get => _totalPrice;
        set => SetProperty(ref _totalPrice, value);
    }
    
    public void UpdateTotalPrice()
    {
        var priceWithDiscount = UnitPrice * (1 - Discount / 100);
        TotalPrice = priceWithDiscount * Quantity;
    }
}

