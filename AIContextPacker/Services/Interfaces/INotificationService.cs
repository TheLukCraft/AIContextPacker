namespace AIContextPacker.Services.Interfaces;

public interface INotificationService
{
    void ShowError(string message);
    void ShowSuccess(string message);
    void ShowWarning(string message);
    void ShowInfo(string message);
}
