﻿#region

using UnityEngine;

#endregion

namespace Services.Factories.UIFactory
{
    public interface IUIInfo
    {
        GameObject LoadingScreen { get; }
        GameObject MainMenuScreen { get; }
        GameObject InGameMenuScreen { get; }
        GameObject FoldingThePuzzle { get; }
    }
}