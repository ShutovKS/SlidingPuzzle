﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Data.GameType;
using Data.PlayerPrefs;
using Data.PuzzleInformation;
using Infrastructure.ProjectStateMachine.Core;
using Services.Factories.UIFactory;
using UI.FoldingThePuzzle;
using Units.Image;
using Units.Piece;
using Units.PuzzleGenerator;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Infrastructure.ProjectStateMachine.States
{
    public class FoldingThePuzzleState : IState<Bootstrap>, IEnter<PuzzleInformation>, IExit
    {
        public FoldingThePuzzleState(Bootstrap initializer, IUIFactory uiFactory)
        {
            _uiFactory = uiFactory;
            Initializer = initializer;
        }

        public Bootstrap Initializer { get; }
        private readonly IUIFactory _uiFactory;

        private FoldingThePuzzleImageSampleUI _imageSampleUI;
        private FoldingThePuzzleGameOverUI _gameOverUI;
        private FoldingThePuzzlePuzzlesUI _puzzlesUI;
        private FoldingThePuzzleMenuUI _menuUI;

        private UnityAction<string> _onTimerUpdate;

        private Stopwatch _timerWatch;
        private PiecesListTwoDimensional _piecesListTwoDimensional;
        private GameObject _foldingThePuzzleInstance;
        private Vector2Int[,] _currentPositions;
        private Vector2Int _emptyPosition;
        private Texture2D[,] _textures2D;
        private Texture2D _texture2D;
        private int _partsAmount;
        private bool _isStopTimer;

        public async void OnEnter(PuzzleInformation puzzleInformation)
        {
            _partsAmount = puzzleInformation.ElementsCount;
            _texture2D = puzzleInformation.Image;

            _textures2D = ImageCutter.CutImage(
                _texture2D,
                ImageCutterType.NumberOfParts,
                _partsAmount,
                _partsAmount);

            _emptyPosition = new Vector2Int(_partsAmount - 1, 0);
            _currentPositions = GenerationPositions.GetRandomPositions(_partsAmount, _emptyPosition);

            InitialisePiecesListTwoDimensional();

            await CreatedUI();
            CreatedParts();

            _onTimerUpdate += Debug.Log;
            TimerStart();
        }

        public void OnExit()
        {
            DestroyUI();
            TimerStop();
        }

        #region UI

        private async Task CreatedUI()
        {
            var foldingThePuzzleInstance = await _uiFactory.CreatedFoldingThePuzzle();

            _imageSampleUI = foldingThePuzzleInstance.GetComponentInChildren<FoldingThePuzzleImageSampleUI>();
            _gameOverUI = foldingThePuzzleInstance.GetComponentInChildren<FoldingThePuzzleGameOverUI>();
            _puzzlesUI = foldingThePuzzleInstance.GetComponentInChildren<FoldingThePuzzlePuzzlesUI>();
            _menuUI = foldingThePuzzleInstance.GetComponentInChildren<FoldingThePuzzleMenuUI>();

            SetUpGameOverUI();
            SetUpImageSampleUI();
            SetUpMenuUI();
        }

        private void DestroyUI()
        {
            _uiFactory.DestroyFoldingThePuzzle();
        }

        private void SetUpGameOverUI()
        {
            _gameOverUI.SetImage(_texture2D);
            _gameOverUI.RegisterExitListener(ExitInMainMenu);
            _gameOverUI.SetActiveFullImagePanel(false);
        }

        private void SetUpImageSampleUI()
        {
            _imageSampleUI.SetImageSample(_texture2D);
        }

        private void SetUpMenuUI()
        {
            _menuUI.RegisterExitListener(ExitInMainMenu);
            _menuUI.RegisterResetListener(ResetParts);
            _onTimerUpdate += _menuUI.UpdateTimer;
        }

        private void OnAllPartsInPlace()
        {
            TimerStop();
            _gameOverUI?.SetActiveFullImagePanel(true);
        }

        #endregion

        #region Part

        private void CreatedParts()
        {
            _puzzlesUI.CreatedParts(_partsAmount);
            _puzzlesUI.RegisteringButtonsEvents(MovePart);
            _puzzlesUI.FillWithPartsOfCutsImages(_textures2D, _currentPositions);
            RemovePart(_emptyPosition);
        }

        private void ResetParts()
        {
            RemoveAllParts();
            InitialisePiecesListTwoDimensional();
            CreatedParts();
            _timerWatch.Restart();
        }

        private void RemoveAllParts()
        {
            var position = new Vector2Int(0, 0);
            for (var y = 0; y < _partsAmount; y++)
            {
                for (var x = 0; x < _partsAmount; x++)
                {
                    RemovePart(position);
                    position.x++;
                }

                position.x = 0;
                position.y++;
            }
        }

        private void RemovePart(Vector2Int position)
        {
            _puzzlesUI.RemovePart(position);
            _piecesListTwoDimensional.RemovePiece(position);
        }

        private void MovePart(Vector2Int currentPosition)
        {
            if (_piecesListTwoDimensional.TryMovePiece(currentPosition, out var newPosition))
            {
                _puzzlesUI.MovePart(currentPosition, newPosition, _partsAmount);
            }
        }

        #endregion

        #region Other

        private void InitialisePiecesListTwoDimensional()
        {
            _piecesListTwoDimensional = new PiecesListTwoDimensional(_partsAmount, _partsAmount);
            _piecesListTwoDimensional.RegisterOnAllPartsInPlace(OnAllPartsInPlace);

            for (var y = 0; y < _partsAmount; y++)
            for (var x = 0; x < _partsAmount; x++)
            {
                var currentPosition = _currentPositions[y, x];

                var targetPosition = new Vector2Int(x, y);
                var piece = new Piece(targetPosition, currentPosition);
                _piecesListTwoDimensional.AddPiece(piece);
            }
        }

        private void ExitInMainMenu()
        {
            Initializer.StateMachine.SwitchState<MainMenuState>();
        }
        
        private void TimerStart()
        {
            _isStopTimer = false;
            _timerWatch = new Stopwatch();
            _timerWatch?.Start();
            TimerUpdate();
        }

        private void TimerStop()
        {
            _timerWatch?.Stop();
            _isStopTimer = true;
        }

        private async void TimerUpdate()
        {
            while (_isStopTimer == false)
            {
                _onTimerUpdate?.Invoke(_timerWatch.Elapsed.ToString(@"mm\:ss"));
                await Task.Delay(1000);
            }
        }

        #endregion
    }
}