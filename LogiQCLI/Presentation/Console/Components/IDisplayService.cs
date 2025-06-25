using System;

namespace LogiQCLI.Presentation.Console.Components
{
    public interface IDisplayService
    {
        void InitializeDisplay();
        Action GetInitializeDisplayAction();
    }
} 