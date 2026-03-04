using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AvaloniaApplication1.Models;
using AvaloniaApplication1.Services;

namespace AvaloniaApplication1.ViewModels;

    public partial class ProductsWindowViewModel : ViewModelBase
{
    private readonly DatabaseService _databaseService;
    private List<Product> _allProducts = new();
    
    [ObservableProperty]
    private ObservableCollection<Product> _products = new();
    
    [ObservableProperty]
    private ObservableCollection<string> _categories = new();
    
    [ObservableProperty]
    private ObservableCollection<string> _manufacturers = new();
    
    [ObservableProperty]
    private string? _selectedCategory;
    
    [ObservableProperty]
    private string? _selectedManufacturer;
    
    [ObservableProperty]
    private string _searchText = string.Empty;
    
    [ObservableProperty]
    private string _sortOption = "По умолчанию";
    
    [ObservableProperty]
    private Product? _selectedProduct;
    
    [ObservableProperty]
    private bool _isLoading = false;
    
    [ObservableProperty]
    private string _statusMessage = string.Empty;
    
        public bool CanFilterAndSearch => CurrentUserService.Instance.CanFilterAndSearch;
        public bool CanEdit => CurrentUserService.Instance.CanEditProducts;
        public bool CanViewOrders => CurrentUserService.Instance.CanViewOrders;
        public bool CanViewMyOrders => CurrentUserService.Instance.CanViewMyOrders;
    
    [ObservableProperty]
    private bool _canCreateOrder;
    
    public ProductsWindowViewModel()
    {
        _databaseService = new DatabaseService();
        UpdatePermissions();
        _ = LoadData();
    }
    
        public void UpdatePermissions()
        {
            CanCreateOrder = CurrentUserService.Instance.CanCreateOrder;
            OnPropertyChanged(nameof(CanFilterAndSearch));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(CanViewOrders));
            OnPropertyChanged(nameof(CanViewMyOrders));
            OnPropertyChanged(nameof(CanCreateOrder));
            
            // Отладочная информация
            System.Diagnostics.Debug.WriteLine($"UpdatePermissions: CanCreateOrder = {CanCreateOrder}");
            System.Diagnostics.Debug.WriteLine($"CurrentUser: {CurrentUserService.Instance.CurrentUser?.Login ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"IsClient: {CurrentUserService.Instance.IsClient}");
            System.Diagnostics.Debug.WriteLine($"IsAdmin: {CurrentUserService.Instance.IsAdmin}");
        }
    
    public ObservableCollection<string> SortOptions { get; } = new()
    {
        "По умолчанию",
        "По возрастанию цены",
        "По убыванию цены"
    };
    
    public event EventHandler? OrdersRequested;
    public event EventHandler? MyOrdersRequested;
    
    
    private async Task LoadData()
    {
        IsLoading = true;
        StatusMessage = "Загрузка...";
        
        try
        {
            _allProducts = await _databaseService.GetAllProducts();
            
            // Load categories
            var categoryList = await _databaseService.GetAllCategories();
            Categories.Clear();
            Categories.Add("Все категории");
            foreach (var cat in categoryList)
            {
                Categories.Add(cat.CategoryName);
            }
            SelectedCategory = "Все категории";
            
            // Load manufacturers
            var manufacturerList = await _databaseService.GetAllManufacturers();
            Manufacturers.Clear();
            Manufacturers.Add("Все производители");
            foreach (var man in manufacturerList)
            {
                Manufacturers.Add(man.ManufacturerName);
            }
            SelectedManufacturer = "Все производители";
            
            ApplyFilters();
            
            // Принудительное обновление UI
            OnPropertyChanged(nameof(Products));
            
            StatusMessage = $"Загружено товаров: {Products.Count}";
            System.Diagnostics.Debug.WriteLine($"Загружено товаров: {Products.Count}");
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
    
    partial void OnSelectedCategoryChanged(string? value)
    {
        if (CanFilterAndSearch)
        {
            ApplyFilters();
        }
    }
    
    partial void OnSelectedManufacturerChanged(string? value)
    {
        if (CanFilterAndSearch)
        {
            ApplyFilters();
        }
    }
    
    partial void OnSearchTextChanged(string value)
    {
        if (CanFilterAndSearch)
        {
            ApplyFilters();
        }
    }
    
    partial void OnSortOptionChanged(string value)
    {
        if (CanFilterAndSearch)
        {
            ApplyFilters();
        }
    }
    
    private void ApplyFilters()
    {
        var filtered = _allProducts.AsEnumerable();
        
        // Filter by category
        if (CanFilterAndSearch && !string.IsNullOrEmpty(SelectedCategory) && SelectedCategory != "Все категории")
        {
            filtered = filtered.Where(p => p.CategoryName == SelectedCategory);
        }
        
        // Filter by manufacturer
        if (CanFilterAndSearch && !string.IsNullOrEmpty(SelectedManufacturer) && SelectedManufacturer != "Все производители")
        {
            filtered = filtered.Where(p => p.ManufacturerName == SelectedManufacturer);
        }
        
        // Search
        if (CanFilterAndSearch && !string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLower();
            filtered = filtered.Where(p => 
                p.ProductName.ToLower().Contains(searchLower) ||
                p.Description?.ToLower().Contains(searchLower) == true ||
                p.Article.ToLower().Contains(searchLower));
        }
        
        // Sort
        if (CanFilterAndSearch)
        {
            filtered = SortOption switch
            {
                "По возрастанию цены" => filtered.OrderBy(p => p.PriceWithDiscount),
                "По убыванию цены" => filtered.OrderByDescending(p => p.PriceWithDiscount),
                _ => filtered.OrderBy(p => p.ProductName)
            };
        }
        
        Products.Clear();
        foreach (var product in filtered)
        {
            Products.Add(product);
        }
        
        // Принудительное обновление UI
        OnPropertyChanged(nameof(Products));
        
        StatusMessage = $"Показано товаров: {Products.Count} из {_allProducts.Count}";
    }
    
    public event EventHandler<Product>? AddProductRequested;
    public event EventHandler<Product>? EditProductRequested;
    public event EventHandler<Product>? DeleteProductRequested;
    public event EventHandler<Product>? CreateOrderRequested;
    
    [RelayCommand]
    private void AddProduct()
    {
        AddProductRequested?.Invoke(this, null!);
    }
    
    [RelayCommand]
    private void EditProduct()
    {
        if (SelectedProduct == null)
        {
            StatusMessage = "Выберите товар для редактирования";
            return;
        }
        
        EditProductRequested?.Invoke(this, SelectedProduct);
    }
    
    [RelayCommand]
    private void DeleteProduct()
    {
        if (SelectedProduct == null)
        {
            StatusMessage = "Выберите товар для удаления";
            return;
        }
        
        DeleteProductRequested?.Invoke(this, SelectedProduct);
    }
    
    [RelayCommand]
    private void CreateOrder()
    {
        if (SelectedProduct == null)
        {
            StatusMessage = "Выберите товар для заказа (кликните по карточке товара)";
            return;
        }
        
        if (SelectedProduct.StockQuantity <= 0)
        {
            StatusMessage = "Товар отсутствует на складе";
            return;
        }
        
        CreateOrderRequested?.Invoke(this, SelectedProduct);
    }
    
    public async Task DeleteProductConfirmed(Product product)
    {
        try
        {
            var result = await _databaseService.DeleteProduct(product.Article);
            if (result)
            {
                StatusMessage = "Товар удален";
                await LoadData();
            }
            else
            {
                StatusMessage = "Не удалось удалить товар";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {ex.Message}";
        }
    }
    
    [RelayCommand]
    private void ShowOrders()
    {
        OrdersRequested?.Invoke(this, EventArgs.Empty);
    }
    
    [RelayCommand]
    private void ShowMyOrders()
    {
        MyOrdersRequested?.Invoke(this, EventArgs.Empty);
    }
    
    [RelayCommand]
    public async Task Refresh()
    {
        await LoadData();
    }
}

