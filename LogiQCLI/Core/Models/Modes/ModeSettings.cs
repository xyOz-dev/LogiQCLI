using System.Collections.Generic;
using LogiQCLI.Core.Models.Modes;

namespace LogiQCLI.Core.Models.Modes
{
    public class ModeSettings
    {
        public string ActiveModeId { get; set; } = "default";
        public List<Mode> DefaultModes { get; set; } = new List<Mode>();
        public List<Mode> CustomModes { get; set; } = new List<Mode>();

        public ModeSettings()
        {
            DefaultModes = BuiltInModes.GetBuiltInModes();
        }
    }
}