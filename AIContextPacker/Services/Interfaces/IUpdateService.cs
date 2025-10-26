using System.Threading.Tasks;
using AIContextPacker.Models;

namespace AIContextPacker.Services.Interfaces;

public interface IUpdateService
{
    Task<UpdateInfo> CheckForUpdatesAsync();
    string GetCurrentVersion();
}
