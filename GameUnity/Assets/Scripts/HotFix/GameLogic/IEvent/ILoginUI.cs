using System.Collections;
using System.Collections.Generic;
using DGame;
using UnityEngine;

namespace GameLogic
{
    [EventInterface(EEventGroup.GroupUI)]
    public interface ILoginUI
    {
        void ShowLoginUI();
    }
}