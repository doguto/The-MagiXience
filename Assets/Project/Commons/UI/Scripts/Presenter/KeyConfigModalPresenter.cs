using System;
using Cysharp.Threading.Tasks;
using Project.Commons.UI.Scripts.View;
using Project.Scripts.Extensions;
using Project.Scripts.Model;
using Project.Scripts.Presenter;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Commons.UI.Scripts.Presenter
{
    public class KeyConfigModalPresenter : MonoPresenter
    {
        [SerializeField] KeyConfigModalView keyConfigModalView;

        readonly Subject<Unit> onClosed = new();
        public IObservable<Unit> OnClosed => onClosed;

        KeyConfigEditingModel editingModel;
        InputAction rebindingCaptureAction;
        InputActionRebindingExtensions.RebindingOperation rebindingOperation;
        KeyConfigModel.KeyConfigAction? waitingAction;
        KeyConfigModel.KeySlot? waitingSlot;

        protected override void Start()
        {
            base.Start();

            var displayOrder = globalScenePresenter.KeyConfigModel.GetDisplayOrder();
            var rowCount = Mathf.Min(displayOrder.Count, keyConfigModalView.RowCount);

            for (var i = 0; i < rowCount; i++)
            {
                var rowIndex = i;
                var action = displayOrder[rowIndex];

                keyConfigModalView.OnPressedPrimary(rowIndex).Subscribe(_ =>
                {
                    soundManager.PlaySEAsync(SeType.Click).Forget();
                    BeginRebind(action, KeyConfigModel.KeySlot.Primary);
                }).AddTo(this);

                keyConfigModalView.OnPressedSecondary(rowIndex).Subscribe(_ =>
                {
                    soundManager.PlaySEAsync(SeType.Click).Forget();
                    BeginRebind(action, KeyConfigModel.KeySlot.Secondary);
                }).AddTo(this);

                keyConfigModalView.OnPressedReset(rowIndex).Subscribe(_ =>
                {
                    soundManager.PlaySEAsync(SeType.Cancel).Forget();
                    if (editingModel == null) return;

                    editingModel.ResetAction(action);
                    RefreshView();
                }).AddTo(this);
            }

            keyConfigModalView.OnPressedSave.Subscribe(_ =>
            {
                soundManager.PlaySEAsync(SeType.Click).Forget();
                SaveAndClose();
            }).AddTo(this);

            keyConfigModalView.OnPressedCancel.Subscribe(_ =>
            {
                soundManager.PlaySEAsync(SeType.Cancel).Forget();
                CloseWithoutSave();
            }).AddTo(this);
        }

        public void Open()
        {
            editingModel = new KeyConfigEditingModel(globalScenePresenter.KeyConfigModel);
            waitingAction = null;
            waitingSlot = null;

            gameObject.SetActive(true);
            keyConfigModalView.InitStart();
            RefreshView();
        }

        void SaveAndClose()
        {
            CancelCurrentRebind();

            editingModel?.Apply();
            editingModel = null;

            gameObject.SetActive(false);
            onClosed.OnNext(Unit.Default);
        }

        void CloseWithoutSave()
        {
            CancelCurrentRebind();

            editingModel = null;
            gameObject.SetActive(false);
            onClosed.OnNext(Unit.Default);
        }

        void RefreshView()
        {
            if (editingModel == null) return;

            var displayOrder = editingModel.GetDisplayOrder();
            var rowCount = Mathf.Min(displayOrder.Count, keyConfigModalView.RowCount);

            for (var i = 0; i < rowCount; i++)
            {
                var action = displayOrder[i];

                keyConfigModalView.SetRow(
                    i,
                    editingModel.GetDisplayName(action),
                    editingModel.GetDisplayString(action, KeyConfigModel.KeySlot.Primary),
                    editingModel.GetDisplayString(action, KeyConfigModel.KeySlot.Secondary)
                );

                if (waitingAction == action && waitingSlot == KeyConfigModel.KeySlot.Primary)
                {
                    keyConfigModalView.SetPrimaryWaiting(i);
                }

                if (waitingAction == action && waitingSlot == KeyConfigModel.KeySlot.Secondary)
                {
                    keyConfigModalView.SetSecondaryWaiting(i);
                }
            }
        }

        void BeginRebind(KeyConfigModel.KeyConfigAction action, KeyConfigModel.KeySlot slot)
        {
            if (editingModel == null) return;

            CancelCurrentRebind();

            waitingAction = action;
            waitingSlot = slot;
            RefreshView();

            rebindingCaptureAction = new InputAction(type: InputActionType.Button);
            rebindingOperation = rebindingCaptureAction.PerformInteractiveRebinding()
                .WithControlsHavingToMatchPath("<Keyboard>")
                .OnComplete(operation =>
                {
                    var selectedPath = operation.selectedControl?.path;
                    CompleteRebind(selectedPath);
                })
                .OnCancel(_ =>
                {
                    CompleteRebind(null);
                });

            rebindingOperation.Start();
        }

        void CompleteRebind(string selectedPath)
        {
            var action = waitingAction;
            var slot = waitingSlot;

            CancelCurrentRebind();

            if (action.HasValue && slot.HasValue && !string.IsNullOrEmpty(selectedPath) && editingModel != null)
            {
                editingModel.SetBindingPath(action.Value, slot.Value, selectedPath);
            }

            waitingAction = null;
            waitingSlot = null;
            RefreshView();
        }

        void CancelCurrentRebind()
        {
            rebindingOperation?.Dispose();
            rebindingOperation = null;

            rebindingCaptureAction?.Dispose();
            rebindingCaptureAction = null;
        }
    }
}
