using Belarus.Launcher.Core.Manager;
using Belarus.Launcher.Core.Validators;


namespace Belarus.Launcher.ViewModels.Validators;

public sealed class AuthenticationViewModelValidator(IAuthenticationValidator validator, IApplicationLocaleManager localeManager)
{
    private readonly IAuthenticationValidator _validator = validator;
    private readonly IApplicationLocaleManager _localeManager = localeManager;

    public ValidationHelper EnsureUsernameNotEmpty(AuthorizationViewModel authorizationViewModel)
    {
        return authorizationViewModel.ValidationRule(viewModel => viewModel.Username,
            username => username is not null && _validator.IsUsernameNotEmpty(username),
            _localeManager.GetStringByKey("LocalizedStrings.EnterNickName"));
    }

    public ValidationHelper EnsureUsernameCorrectLength(AuthorizationViewModel authorizationViewModel)
    {
        return authorizationViewModel.ValidationRule(viewModel => viewModel.Username,
            username => username is not null && _validator.IsUsernameCorrectLength(username),
            _localeManager.GetStringByKey("LocalizedStrings.TooLongNickname"));
    }

    public ValidationHelper EnsureUsernameCorrectCharacters(AuthorizationViewModel authorizationViewModel)
    {
        return authorizationViewModel.ValidationRule(viewModel => viewModel.Username,
            username => username is not null && _validator.IsUsernameCorrectCharacters(username),
            _localeManager.GetStringByKey("LocalizedStrings.InvalidCharacters"));
    }
}
