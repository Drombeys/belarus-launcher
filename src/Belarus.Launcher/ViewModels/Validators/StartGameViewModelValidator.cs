using Belarus.Launcher.Core.Manager;
using Belarus.Launcher.Core.Validators;


namespace Belarus.Launcher.ViewModels.Validators;

public sealed class StartGameViewModelValidator(IStartGameValidator validator, IApplicationLocaleManager localeManager)
{
    private readonly IStartGameValidator _validator = validator;
    private readonly IApplicationLocaleManager _localeManager = localeManager;

    public ValidationHelper EnsureIpAddressNotEmpty(StartGameViewModel startGameViewModel)
    {

        return startGameViewModel.ValidationRule(viewModel => viewModel.IpAddress,
            serverAddress => serverAddress is not null && _validator.IsIpAddressNotEmpty(serverAddress),
            _localeManager.GetStringByKey("LocalizedStrings.IpAddressNotEntered"));
    }

    public ValidationHelper EnsureValidIpAddressOrUrl(StartGameViewModel startGameViewModel)
    {
        return startGameViewModel.ValidationRule(viewModel => viewModel.IpAddress,
                serverAddress => serverAddress is not null && _validator.IsValidIpAddressOrUrl(serverAddress),
                _localeManager.GetStringByKey("LocalizedStrings.InvalidIpAddress"));
    }
}
