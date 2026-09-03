using Microsoft.Maui.Controls;
using VBotMauiApp.ViewModels;

namespace VBotMauiApp.Views;

public partial class CallPage : ContentPage
{
    private readonly CallViewModel _viewModel;

    public CallPage(CallViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }

    protected override bool OnBackButtonPressed()
    {
        // Chặn phím Back cứng khi đang trong cuộc gọi (người dùng phải bấm nút Gác máy)
        return true;
    }
}
