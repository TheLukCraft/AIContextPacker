using System.Collections.Generic;
using System.Threading.Tasks;
using AIContextPacker.Models;

namespace AIContextPacker.Services.Interfaces;

public interface ISettingsService
{
    Task<AppSettings> LoadSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);
    Task<List<string>> GetRecentProjectsAsync();
    Task AddRecentProjectAsync(string projectPath);
    Task<SessionState> LoadSessionStateAsync();
    Task SaveSessionStateAsync(SessionState state);
}
