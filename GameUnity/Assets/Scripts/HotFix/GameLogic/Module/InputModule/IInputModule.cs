using System.Collections.Generic;

namespace GameLogic
{
    public interface IInputModule
    {
        T AddInputHandler<T>();

        void RemoveInputHandler<T>();

        T AddInputLayer<T>();

        void RemoveInputLayer<T>();

        void SetInputAction();

        void SetInputAxis();

        float GetInputAxis();
    }
}