using System.Collections.Generic;
using LogiQCLI.Core.Models.Modes;

namespace LogiQCLI.Core.Models.Modes.Interfaces
{
    public interface IModeManager
    {
        Mode GetCurrentMode();
        bool SetCurrentMode(string modeId);
        List<Mode> GetAvailableModes();
        Mode? GetMode(string modeId);
        bool AddCustomMode(Mode mode);
        bool RemoveCustomMode(string modeId);
        bool IsToolAllowedInCurrentMode(string toolName);
        int GetBuiltInModeCount();
    }
}
