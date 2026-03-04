using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaApplication1.ViewModels;

public partial class ConfirmDeleteWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _message = string.Empty;
    
    [ObservableProperty]
    private string _itemName = string.Empty;
    
    public event EventHandler<bool>? DeleteConfirmed;
    
    public ConfirmDeleteWindowViewModel(string itemName, string itemType)
    {
        ItemName = itemName;
        Message = $"Вы уверены, что хотите удалить {itemType.ToLower()} '{itemName}'?";
    }
    
    [RelayCommand]
    private void Confirm()
    {
        DeleteConfirmed?.Invoke(this, true);
    }
    
    [RelayCommand]
    private void Cancel()
    {
        DeleteConfirmed?.Invoke(this, false);
    }
}

