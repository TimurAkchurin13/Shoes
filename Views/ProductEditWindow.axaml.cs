using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using AvaloniaApplication1.Models;
using AvaloniaApplication1.ViewModels;

namespace AvaloniaApplication1.Views;

public partial class ProductEditWindow : Window
{
    private string? _pendingPhotoPath;

    public ProductEditWindow()
    {
        InitializeComponent();
    }
    
    public ProductEditWindow(ProductEditWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
        
        // Загружаем изображение товара, если он редактируется
        if (viewModel.IsEditMode && !string.IsNullOrWhiteSpace(viewModel.Article))
        {
            _ = LoadProductImageAsync(viewModel.Article);
        }
        else
        {
            // При добавлении нового товара показываем placeholder
            _ = LoadPlaceholderAsync();
        }
        
        viewModel.ProductSaved += (s, e) =>
        {
            Close(true);
        };
        
        viewModel.ProductCancelled += (s, e) =>
        {
            Close(false);
        };
    }

    private async Task LoadProductImageAsync(string article)
    {
        try
        {
            var dbService = new Services.DatabaseService();
            var products = await dbService.GetAllProducts();
            var product = products.FirstOrDefault(p => p.Article == article);
            
            if (product != null && !string.IsNullOrWhiteSpace(product.Photo))
            {
                await LoadImagePreviewAsync(product.Photo);
            }
            else
            {
                await LoadPlaceholderAsync();
            }
        }
        catch
        {
            await LoadPlaceholderAsync();
        }
    }

    private async void OnChoosePhoto(object? sender, RoutedEventArgs e)
    {
        var storageProvider = StorageProvider;
        if (storageProvider is null)
        {
            return;
        }

        var fileType = new FilePickerFileType("Изображения")
        {
            Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" }
        };

        var options = new FilePickerOpenOptions
        {
            Title = "Выберите изображение",
            AllowMultiple = false,
            FileTypeFilter = new[] { fileType }
        };

        var files = await storageProvider.OpenFilePickerAsync(options);
        var file = files.FirstOrDefault();
        if (file is null)
        {
            return;
        }

        var path = file.Path.LocalPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        _pendingPhotoPath = path;

        // Сохраняем путь к выбранному изображению во ViewModel,
        // чтобы он записался в БД при сохранении товара
        if (DataContext is ProductEditWindowViewModel vm)
        {
            vm.PhotoFileName = path;
        }

        await LoadImagePreviewAsync(path);
    }

    private async void OnClearPhoto(object? sender, RoutedEventArgs e)
    {
        _pendingPhotoPath = null;

        if (DataContext is ProductEditWindowViewModel vm)
        {
            vm.PhotoFileName = null;
        }

        await LoadPlaceholderAsync();
    }

    private async Task LoadImagePreviewAsync(string? path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                await LoadPlaceholderAsync();
                return;
            }

            if (Path.IsPathRooted(path) && File.Exists(path))
            {
                await using var file = File.OpenRead(path);
                PhotoPreview.Source = new Bitmap(file);
                return;
            }

            var absolute = Path.Combine(AppContext.BaseDirectory,
                path.Replace("/", Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal));
            if (File.Exists(absolute))
            {
                await using var file = File.OpenRead(absolute);
                PhotoPreview.Source = new Bitmap(file);
                return;
            }

            await LoadPlaceholderAsync();
        }
        catch
        {
            await LoadPlaceholderAsync();
        }
    }

    private async Task LoadPlaceholderAsync()
    {
        var placeholderUri = new Uri("avares://AvaloniaApplication1/Images/picture.png");
        await using var stream = AssetLoader.Open(placeholderUri);
        PhotoPreview.Source = new Bitmap(stream);
    }
}

