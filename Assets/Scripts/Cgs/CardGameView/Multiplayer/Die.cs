/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.CardGameView.Viewer;
using Cgs.Menu;
using Cgs.Play;
using Cgs.Play.Multiplayer;
using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cgs.CardGameView.Multiplayer
{
    public class Die : CgsNetPlayable
    {
        public const int DefaultMin = 1;
        public const int DefaultMax = 6;
        public const string DeletePrompt = "Delete die?";

        private const float RollTime = 1.0f;
        private const float RollDelay = 0.05f;

        public Text valueText;

        public int Min
        {
            get => _min.Value;
            set => _min.Value = value;
        }

        private readonly NetworkVariable<int> _min = new();

        public int Max
        {
            get => _max.Value;
            set => _max.Value = value;
        }

        private readonly NetworkVariable<int> _max = new();

        public override string ViewValue => $"Value: {Value}";

        private int Value
        {
            get => _value.Value;
            set
            {
                var newValue = value;
                if (newValue > Max)
                    newValue = Min;
                if (newValue < Min)
                    newValue = Max;

                if (CgsNetManager.Instance.IsOnline)
                    UpdateValueServerRpc(newValue);
                else
                {
                    _value.Value = newValue;
                    OnChangeValue(value, newValue);
                }
            }
        }

        private readonly NetworkVariable<int> _value = new();

        private float _rollTime;
        private float _rollDelay;

        protected override void OnAwakePlayable()
        {
            _value.OnValueChanged += OnChangeValue;
        }

        protected override void OnStartPlayable()
        {
            Max = DefaultMax;
            Min = DefaultMin;
            _value.Value = Min;
            valueText.text = _value.Value.ToString();
            if (!NetworkManager.Singleton.IsConnectedClient || IsServer)
                _rollTime = RollTime;
        }

        protected override void OnUpdatePlayable()
        {
            if (_rollTime <= 0 || (NetworkManager.Singleton.IsConnectedClient && !IsServer))
                return;

            _rollTime -= Time.deltaTime;
            _rollDelay += Time.deltaTime;
            if (_rollDelay < RollDelay)
                return;

            Value = Random.Range(Min, Max + 1);
            _rollDelay = 0;
        }

        protected override void OnPointerUpSelectPlayable(PointerEventData eventData)
        {
            if (CurrentPointerEventData == null || CurrentPointerEventData.pointerId != eventData.pointerId ||
                eventData.dragging ||
                eventData.button is PointerEventData.InputButton.Middle or PointerEventData.InputButton.Right)
                return;

            if (PlaySettings.DoubleClickToRollDice && EventSystem.current.currentSelectedGameObject == gameObject)
                Roll();
            else if (!EventSystem.current.alreadySelecting &&
                     EventSystem.current.currentSelectedGameObject != gameObject)
                EventSystem.current.SetSelectedGameObject(gameObject, eventData);
        }

        protected override void OnPointerEnterPlayable(PointerEventData eventData)
        {
            if (Settings.PreviewOnMouseOver && CardViewer.Instance != null && !CardViewer.Instance.IsVisible
                && PlayableViewer.Instance != null && !PlayableViewer.Instance.IsVisible)
                PlayableViewer.Instance.Preview(this);
        }

        protected override void OnPointerExitPlayable(PointerEventData eventData)
        {
            if (PlayableViewer.Instance != null)
                PlayableViewer.Instance.HidePreview();
        }

        protected override void OnSelectPlayable(BaseEventData eventData)
        {
            if (PlayableViewer.Instance != null)
                PlayableViewer.Instance.SelectedPlayable = this;
        }

        protected override void OnDeselectPlayable(BaseEventData eventData)
        {
            if (PlayableViewer.Instance != null)
                PlayableViewer.Instance.IsVisible = false;
        }

        protected override void OnBeginDragPlayable(PointerEventData eventData)
        {
            if (CgsNetManager.Instance.IsOnline)
                RequestChangeOwnership();
        }

        protected override void OnDragPlayable(PointerEventData eventData)
        {
            if (LacksOwnership)
                RequestChangeOwnership();
            else
                UpdatePosition();
        }

        protected override void OnEndDragPlayable(PointerEventData eventData)
        {
            if (!LacksOwnership)
                UpdatePosition();
        }

        [ServerRpc(RequireOwnership = false)]
        private void UpdateValueServerRpc(int value)
        {
            _value.Value = value;
        }

        [PublicAPI]
        public void OnChangeValue(int oldValue, int newValue)
        {
            valueText.text = newValue.ToString();
        }

        [UsedImplicitly]
        public void Decrement()
        {
            Value -= 1;
        }

        [UsedImplicitly]
        public void Increment()
        {
            Value += 1;
        }

        [UsedImplicitly]
        public void Roll()
        {
            if (CgsNetManager.Instance.IsOnline)
                RollServerRpc();
            else
                _rollTime = RollTime;
        }

        [ServerRpc(RequireOwnership = false)]
        private void RollServerRpc()
        {
            _rollTime = RollTime;
        }

        [UsedImplicitly]
        public void PromptDelete()
        {
            CardGameManager.Instance.Messenger.Prompt(DeletePrompt, RequestDelete);
        }
    }
}
