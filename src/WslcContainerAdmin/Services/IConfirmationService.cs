namespace WslcContainerAdmin.Services;

public interface IConfirmationService
{
    Task<bool> ConfirmAsync(string title, string message);
}
