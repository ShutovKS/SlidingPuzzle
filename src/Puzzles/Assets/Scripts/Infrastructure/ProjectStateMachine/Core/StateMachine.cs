#region

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace Infrastructure.ProjectStateMachine.Core
{
    public class StateMachine<TInitializer>
    {
        public StateMachine(params IState<TInitializer>[] states)
        {
            _states = new Dictionary<Type, IState<TInitializer>>(states.Length);

            _states = states.ToDictionary(state => state.GetType(), state => state);
        }

        private readonly Dictionary<Type, IState<TInitializer>> _states;

        private IState<TInitializer> _currentState;

        public void SwitchState<TState>() where TState : IState<TInitializer>
        {
            TryExitPreviousState<TState>();

            GetNewState<TState>();

            TryEnterNewState<TState>();
        }

        public void SwitchState<TState, T0>(T0 arg) where TState : IState<TInitializer>
        {
            TryExitPreviousState<TState>();

            GetNewState<TState>();

            TryEnterNewState<TState, T0>(arg);
        }

        private void TryExitPreviousState<TState>() where TState : IState<TInitializer>
        {
            if (_currentState is IExit exit)
            {
                exit.OnExit();
            }
        }

        private void TryEnterNewState<TState>() where TState : IState<TInitializer>
        {
            if (_currentState is IEnter enter)
            {
                enter.OnEnter();
            }
        }

        private void TryEnterNewState<TState, T0>(T0 arg) where TState : IState<TInitializer>
        {
            if (_currentState is IEnter<T0> enter)
            {
                enter.OnEnter(arg);
            }
        }

        private void GetNewState<TState>() where TState : IState<TInitializer>
        {
            var newState = GetState<TState>();
            _currentState = newState;
        }

        private TState GetState<TState>() where TState : IState<TInitializer>
        {
            return (TState)_states[typeof(TState)];
        }
    }
}