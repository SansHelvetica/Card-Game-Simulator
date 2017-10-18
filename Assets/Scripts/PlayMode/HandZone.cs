﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HandZone : MonoBehaviour, IDropHandler
{
    public GameObject cardPrefab;
    public RectTransform extension;
    public Text countText;

    public bool IsExtended { get; private set; }

    void Start()
    {
        extension.gameObject.GetOrAddComponent<CardStack>().OnAddCardActions.Add(CardModel.ShowCard);
        extension.gameObject.GetOrAddComponent<CardStack>().OnAddCardActions.Add(CardModel.ResetRotation);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        CardModel cardModel = eventData.pointerDrag.GetComponent<CardModel>();
        if (cardModel != null) {
            CardModel draggedCardModel;
            if (cardModel.DraggedClones.TryGetValue(eventData.pointerId, out draggedCardModel))
                cardModel = draggedCardModel;
            AddCard(cardModel.Card);
        }
    }

    public void AddCard(Card card)
    {
        CardModel newCardModel = Instantiate(cardPrefab, extension).GetOrAddComponent<CardModel>();
        newCardModel.Card = card;
        newCardModel.DoubleClickEvent = CardModel.ToggleFacedown;
        newCardModel.SecondaryDragAction = null;
        newCardModel.CanvasGroup.blocksRaycasts = true;
    }

    void Update()
    {
        countText.text = extension.childCount.ToString();
    }

    public void ToggleExtension()
    {
        IsExtended = !IsExtended;
        extension.gameObject.GetOrAddComponent<CanvasGroup>().alpha = IsExtended ? 1 : 0;
        extension.gameObject.GetOrAddComponent<CanvasGroup>().blocksRaycasts = IsExtended;
    }
}
