using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AvaloniaApplication1.Models;
using AvaloniaApplication1.Services;

namespace AvaloniaApplication1.ViewModels;

public partial class CreateOrderWindowViewModel : ViewModelBase
{
    private readonly DatabaseService _databaseService;
    
    [ObservableProperty]
    private ObservableCollection<PickupPoint> _pickupPoints = new();
    
    [ObservableProperty]
    private PickupPoint? _selectedPickupPoint;
    
    [ObservableProperty]
    private DateTimeOffset? _deliveryDate = DateTimeOffset.Now.AddDays(7);
    
    [ObservableProperty]
    private ObservableCollection<CartItem> _cartItems = new();
    
    [ObservableProperty]
    private decimal _totalAmount = 0;
    
    [ObservableProperty]
    private string _statusMessage = string.Empty;
    
    [ObservableProperty]
    private bool _isLoading = false;
    
    public Product? SelectedProduct { get; set; }
    
    [ObservableProperty]
    private ObservableCollection<Product> _availableProducts = new();
    
    [ObservableProperty]
    private Product? _selectedProductToAdd;
    
    public bool HasAvailableProducts => AvailableProducts.Count > 0;
    
    public event EventHandler? OrderCreated;
    public event EventHandler? OrderCancelled;
    
    public CreateOrderWindowViewModel(Product product)
    {
        _databaseService = new DatabaseService();
        SelectedProduct = product;
        _ = LoadData();
    }
    
    public CreateOrderWindowViewModel()
    {
        _databaseService = new DatabaseService();
        SelectedProduct = null;
        _ = LoadData();
    }
    
    private async Task LoadData()
    {
        IsLoading = true;
        try
        {
            var points = await _databaseService.GetAllPickupPoints();
            PickupPoints.Clear();
            foreach (var point in points)
            {
                PickupPoints.Add(point);
            }
            
            // Если товар передан, добавляем его в корзину
            if (SelectedProduct != null)
            {
                CartItems.Add(new CartItem
                {
                    Product = SelectedProduct,
                    Quantity = 1
                });
            }
            else
            {
                // Загружаем список товаров для выбора
                var products = await _databaseService.GetAllProducts();
                AvailableProducts.Clear();
                foreach (var product in products)
                {
                    AvailableProducts.Add(product);
                }
                OnPropertyChanged(nameof(HasAvailableProducts));
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
    private void AddProductToCart()
    {
        if (SelectedProductToAdd == null)
        {
            StatusMessage = "Выберите товар для добавления";
            return;
        }
        
        // Проверяем, не добавлен ли уже этот товар
        var existingItem = CartItems.FirstOrDefault(item => item.Product.Article == SelectedProductToAdd.Article);
        if (existingItem != null)
        {
            if (existingItem.Quantity < existingItem.Product.StockQuantity)
            {
                existingItem.Quantity++;
            }
            else
            {
                StatusMessage = "Достигнуто максимальное количество товара на складе";
                return;
            }
        }
        else
        {
            CartItems.Add(new CartItem
            {
                Product = SelectedProductToAdd,
                Quantity = 1
            });
        }
        
        SelectedProductToAdd = null;
        UpdateTotal();
        StatusMessage = string.Empty;
    }
    
    [RelayCommand]
    private void IncreaseQuantity(CartItem item)
    {
        if (item.Quantity < item.Product.StockQuantity)
        {
            item.Quantity++;
            UpdateTotal();
        }
    }
    
    [RelayCommand]
    private void DecreaseQuantity(CartItem item)
    {
        if (item.Quantity > 1)
        {
            item.Quantity--;
            UpdateTotal();
        }
    }
    
    [RelayCommand]
    private void RemoveItem(CartItem item)
    {
        CartItems.Remove(item);
        UpdateTotal();
    }
    
    private void UpdateTotal()
    {
        TotalAmount = CartItems.Sum(item => item.TotalPrice);
    }
    
    [RelayCommand]
    private async Task CreateOrder()
    {
        // Дополнительная проверка прав
        if (!CurrentUserService.Instance.CanCreateOrder)
        {
            StatusMessage = "У вас нет прав на оформление заказа.";
            return;
        }

        if (SelectedPickupPoint == null)
        {
            StatusMessage = "Выберите пункт выдачи";
            return;
        }
        
        if (CartItems.Count == 0)
        {
            StatusMessage = "Корзина пуста";
            return;
        }
        
        if (!DeliveryDate.HasValue)
        {
            StatusMessage = "Укажите дату доставки";
            return;
        }
        
        if (DeliveryDate.Value.Date < DateTime.Now.Date)
        {
            StatusMessage = "Дата доставки не может быть в прошлом";
            return;
        }
        
        IsLoading = true;
        StatusMessage = "Создание заказа...";
        
        try
        {
            var user = CurrentUserService.Instance.CurrentUser;
            if (user == null)
            {
                StatusMessage = "Пользователь не авторизован";
                return;
            }
            
            var order = new Order
            {
                OrderDate = DateTime.Now,
                DeliveryDate = DeliveryDate.Value.DateTime,
                PickupPointId = SelectedPickupPoint.Id,
                ClientId = user.Id,
                ReceiptCode = GenerateReceiptCode(),
                OrderStatus = "В обработке",
                TotalAmount = TotalAmount
            };
            
            var details = CartItems.Select(item => new OrderDetail
            {
                Article = item.Product.Article,
                Quantity = item.Quantity,
                UnitPrice = item.Product.Price,
                Discount = item.Product.CurrentDiscount ?? 0,
                TotalPrice = item.TotalPrice
            }).ToList();
            
            var orderNumber = await _databaseService.CreateOrder(order, details);
            
            StatusMessage = $"Заказ №{orderNumber} успешно создан!";
            await Task.Delay(1500);
            
            OrderCreated?.Invoke(this, EventArgs.Empty);
        }
        catch (Npgsql.PostgresException pgEx)
        {
            if (pgEx.SqlState == "23503") // Foreign key violation
            {
                StatusMessage = "Ошибка: выбранные данные не существуют в базе. Проверьте пункт выдачи.";
            }
            else
            {
                StatusMessage = $"Ошибка БД: {pgEx.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка создания заказа: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private string GenerateReceiptCode()
    {
        return $"RCP{DateTime.Now:yyyyMMddHHmmss}";
    }
    
    [RelayCommand]
    private void Cancel()
    {
        OrderCancelled?.Invoke(this, EventArgs.Empty);
    }
}

public class CartItem : ObservableObject
{
    public Product Product { get; set; } = null!;
    
    private int _quantity = 1;
    public int Quantity
    {
        get => _quantity;
        set
        {
            SetProperty(ref _quantity, value);
            OnPropertyChanged(nameof(TotalPrice));
        }
    }
    
    public decimal TotalPrice => Product.PriceWithDiscount * Quantity;
}

