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

public partial class ProductEditWindowViewModel : ViewModelBase
{
    private readonly DatabaseService _databaseService;
    private readonly Product? _originalProduct;
    
    [ObservableProperty]
    private string _article = string.Empty;
    
    [ObservableProperty]
    private ObservableCollection<ProductName> _productNames = new();
    
    [ObservableProperty]
    private ProductName? _selectedProductName;
    
    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();
    
    [ObservableProperty]
    private Category? _selectedCategory;
    
    [ObservableProperty]
    private ObservableCollection<Manufacturer> _manufacturers = new();
    
    [ObservableProperty]
    private Manufacturer? _selectedManufacturer;
    
    [ObservableProperty]
    private ObservableCollection<Supplier> _suppliers = new();
    
    [ObservableProperty]
    private Supplier? _selectedSupplier;
    
    [ObservableProperty]
    private ObservableCollection<UnitOfMeasure> _unitOfMeasures = new();
    
    [ObservableProperty]
    private UnitOfMeasure? _selectedUnitOfMeasure;
    
    [ObservableProperty]
    private decimal _price = 0;
    
    [ObservableProperty]
    private decimal? _discount = 0;
    
    [ObservableProperty]
    private int _stockQuantity = 0;
    
    [ObservableProperty]
    private string _description = string.Empty;
    
    [ObservableProperty]
    private string _statusMessage = string.Empty;
    
    [ObservableProperty]
    private bool _isLoading = false;
    
    // Имя файла или полный путь к изображению товара
    [ObservableProperty]
    private string? _photoFileName;
    
    public bool IsEditMode => _originalProduct != null;
    public string WindowTitle => IsEditMode ? "Редактирование товара" : "Добавление товара";
    
    public event EventHandler? ProductSaved;
    public event EventHandler? ProductCancelled;
    
    public ProductEditWindowViewModel(Product? product = null)
    {
        _databaseService = new DatabaseService();
        _originalProduct = product;
        _ = LoadData();
    }
    
    private async Task LoadData()
    {
        IsLoading = true;
        try
        {
            var categories = await _databaseService.GetAllCategories();
            Categories.Clear();
            foreach (var cat in categories)
            {
                Categories.Add(cat);
            }
            
            var manufacturers = await _databaseService.GetAllManufacturers();
            Manufacturers.Clear();
            foreach (var man in manufacturers)
            {
                Manufacturers.Add(man);
            }
            
            var suppliers = await _databaseService.GetAllSuppliers();
            Suppliers.Clear();
            foreach (var sup in suppliers)
            {
                Suppliers.Add(sup);
            }
            
            var productNames = await _databaseService.GetAllProductNames();
            ProductNames.Clear();
            foreach (var pn in productNames)
            {
                ProductNames.Add(pn);
            }
            
            var units = await _databaseService.GetAllUnitOfMeasure();
            UnitOfMeasures.Clear();
            foreach (var unit in units)
            {
                UnitOfMeasures.Add(unit);
            }
            
            if (_originalProduct != null)
            {
                Article = _originalProduct.Article;
                SelectedCategory = Categories.FirstOrDefault(c => c.Id == _originalProduct.CategoryId);
                SelectedManufacturer = Manufacturers.FirstOrDefault(m => m.Id == _originalProduct.ManufacturerId);
                SelectedSupplier = Suppliers.FirstOrDefault(s => s.Id == _originalProduct.SupplierId);
                SelectedUnitOfMeasure = UnitOfMeasures.FirstOrDefault(u => u.Id == _originalProduct.UnitId);
                Price = _originalProduct.Price;
                Discount = _originalProduct.CurrentDiscount;
                StockQuantity = _originalProduct.StockQuantity;
                Description = _originalProduct.Description ?? string.Empty;
                PhotoFileName = _originalProduct.Photo;
                
                var productName = ProductNames.FirstOrDefault(pn => pn.Id == _originalProduct.ProductNameId);
                if (productName != null)
                {
                    SelectedProductName = productName;
                }
            }
            else
            {
                // При добавлении нового товара выбираем первую единицу измерения по умолчанию
                if (UnitOfMeasures.Count > 0)
                {
                    SelectedUnitOfMeasure = UnitOfMeasures[0];
                }
            }
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
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Article))
        {
            StatusMessage = "Введите артикул";
            return;
        }
        
        if (SelectedProductName == null)
        {
            StatusMessage = "Выберите наименование товара";
            return;
        }
        
        if (SelectedCategory == null)
        {
            StatusMessage = "Выберите категорию";
            return;
        }
        
        if (SelectedManufacturer == null)
        {
            StatusMessage = "Выберите производителя";
            return;
        }
        
        if (SelectedSupplier == null)
        {
            StatusMessage = "Выберите поставщика";
            return;
        }
        
        if (Price <= 0)
        {
            StatusMessage = "Цена должна быть больше 0";
            return;
        }
        
        if (StockQuantity < 0)
        {
            StatusMessage = "Количество не может быть отрицательным";
            return;
        }
        
        IsLoading = true;
        StatusMessage = IsEditMode ? "Сохранение изменений..." : "Добавление товара...";
        
        try
        {
            // Проверка всех значений перед сохранением
            if (SelectedProductName == null || SelectedProductName.Id <= 0)
            {
                StatusMessage = "Ошибка: не выбрано наименование товара";
                return;
            }
            
            if (SelectedCategory == null || SelectedCategory.Id <= 0)
            {
                StatusMessage = "Ошибка: не выбрана категория";
                return;
            }
            
            if (SelectedManufacturer == null || SelectedManufacturer.Id <= 0)
            {
                StatusMessage = "Ошибка: не выбран производитель";
                return;
            }
            
            if (SelectedSupplier == null || SelectedSupplier.Id <= 0)
            {
                StatusMessage = "Ошибка: не выбран поставщик";
                return;
            }
            
            if (SelectedUnitOfMeasure == null || SelectedUnitOfMeasure.Id <= 0)
            {
                StatusMessage = "Ошибка: не выбрана единица измерения";
                return;
            }
            
            var product = new Product
            {
                Article = Article.Trim().ToUpper(), // Артикул в верхнем регистре
                ProductNameId = SelectedProductName.Id,
                UnitId = SelectedUnitOfMeasure.Id,
                Price = Price,
                SupplierId = SelectedSupplier.Id,
                ManufacturerId = SelectedManufacturer.Id,
                CategoryId = SelectedCategory.Id,
                CurrentDiscount = Discount > 0 && Discount <= 100 ? Discount : null,
                StockQuantity = StockQuantity,
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                Photo = string.IsNullOrWhiteSpace(PhotoFileName) ? null : PhotoFileName
            };
            
            bool success = false;
            try
            {
                if (IsEditMode)
                {
                    success = await _databaseService.UpdateProduct(product);
                    if (success)
                    {
                        StatusMessage = "Товар успешно обновлен!";
                    }
                }
                else
                {
                    // Проверка на существующий артикул
                    var existingProducts = await _databaseService.GetAllProducts();
                    if (existingProducts.Any(p => p.Article == product.Article))
                    {
                        StatusMessage = $"Товар с артикулом '{product.Article}' уже существует!";
                        return;
                    }
                    
                    success = await _databaseService.AddProduct(product);
                    if (success)
                    {
                        StatusMessage = "Товар успешно добавлен!";
                    }
                }
                
                if (success)
                {
                    await Task.Delay(1000);
                    ProductSaved?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения товара: {ex}");
            }
        }
        catch (Npgsql.PostgresException pgEx)
        {
            if (pgEx.SqlState == "23503") // Foreign key violation
            {
                StatusMessage = "Ошибка: выбранное значение не существует в базе данных. Выберите другое значение.";
            }
            else
            {
                StatusMessage = $"Ошибка БД: {pgEx.Message}";
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
        ProductCancelled?.Invoke(this, EventArgs.Empty);
    }
}

