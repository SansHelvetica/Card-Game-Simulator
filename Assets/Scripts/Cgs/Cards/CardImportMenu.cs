/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CardGameDef;
using CardGameDef.Unity;
using Cgs.Menu;
using JetBrains.Annotations;
using SFB;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Cards
{
    public class CardImportMenu : Modal
    {
        public const string DownloadCardImage = "Download Card Image";
        public const string DownloadCardImagePrompt = "Enter card image url...";
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        public const string ImportImage = "Import Image";
#else
        public const string SelectCardImageFilePrompt = "Select Card Image File";
#endif
        public const string ImportImageWarningMessage = "No image file selected for import!";
        public const string ImageImportFailedWarningMessage = "Failed to get the image! Unable to import the card.";

        public GameObject downloadMenuPrefab;
        public List<InputField> inputFields;
        public InputField cardIdInputField;
        public InputField setCodeInputField;
        public Image cardImage;
        public Button createButton;

        [UsedImplicitly] public string CardName { get; set; }

        private Uri CardImageUri
        {
            get => _cardImageUri;
            set
            {
                _cardImageUri = value;
                ValidateImportButton();
            }
        }

        private Uri _cardImageUri;

        private Sprite CardImageSprite
        {
            get => _cardImageSprite;
            set
            {
                if (_cardImageSprite != null)
                    Destroy(_cardImageSprite);
                _cardImageSprite = value;
            }
        }

        private Sprite _cardImageSprite;

        private DownloadMenu Downloader => _downloader
            ? _downloader
            : (_downloader = Instantiate(downloadMenuPrefab)
                .GetOrAddComponent<DownloadMenu>());

        private DownloadMenu _downloader;

        private UnityAction _onCreationCallback;

        private void Update()
        {
            if (!IsFocused || inputFields.Any(inputField => inputField.isFocused))
                return;

            if ((Inputs.IsSubmit || Inputs.IsNew) && createButton.interactable)
                StartImport();
            if (Inputs.IsLoad && createButton.interactable)
                DownloadCardImageFromWeb();
            if (Inputs.IsSave && createButton.interactable)
                ImportCardImageFromFile();
            else if (Inputs.IsCancel || Inputs.IsOption)
                Hide();
        }

        public void Show(UnityAction onCreationCallback)
        {
            Show();
            setCodeInputField.text = string.Concat(CardGameManager.Current.Name.Where(char.IsLetterOrDigit));
            cardImage.sprite = CardImageSprite != null ? CardImageSprite : CardGameManager.Current.CardBackImageSprite;
            _onCreationCallback = onCreationCallback;
        }

        [UsedImplicitly]
        public void DownloadCardImageFromWeb()
        {
            Downloader.Show(DownloadCardImage, DownloadCardImagePrompt, DownloadCardImageFromWeb);
        }

        private IEnumerator DownloadCardImageFromWeb(string url)
        {
            CardImageUri = new Uri(url);
            yield return UpdateCardImage();
        }

        [UsedImplicitly]
        public void ImportCardImageFromFile()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            NativeGallery.GetImageFromGallery(ImportCardImageFromFile, ImportImage);
#elif ENABLE_WINMD_SUPPORT
            ImportCardImageFromFile(UwpFileBrowser.OpenFilePanel());
#elif UNITY_STANDALONE_LINUX
            var paths =
 StandaloneFileBrowser.OpenFilePanel(SelectCardImageFilePrompt, string.Empty, string.Empty, false);
            if (paths.Length > 0)
                ImportCardImageFromFile(paths[0]);
            else
                Debug.LogWarning(ImportImageWarningMessage);
#else
            StandaloneFileBrowser.OpenFilePanelAsync(SelectCardImageFilePrompt, string.Empty, string.Empty, false,
                paths => { ImportCardImageFromFile(paths?.Length > 0 ? paths[0] : string.Empty); });
#endif
        }

#if ENABLE_WINMD_SUPPORT
        private async void ImportCardImageFromFile(string uri)
#else
        private void ImportCardImageFromFile(string uri)
#endif
        {
            if (string.IsNullOrEmpty(uri))
            {
                Debug.LogWarning(ImportImageWarningMessage);
                return;
            }
#if ENABLE_WINMD_SUPPORT
            CardImageUri = new Uri(await UnityFileMethods.CacheFileAsync(uri));
#elif UNITY_STANDALONE
            CardImageUri = new Uri(UnityFileMethods.CacheFile(uri));
#else
            CardImageUri = new Uri(uri);
#endif
            StartCoroutine(UpdateCardImage());
        }

        private IEnumerator UpdateCardImage()
        {
            // NOTE: Memory Leak Potential
            yield return UnityFileMethods.RunOutputCoroutine<Sprite>(
                UnityFileMethods.CreateAndOutputSpriteFromImageFile(CardImageUri?.AbsoluteUri)
                , output => CardImageSprite = output);
            if (CardImageSprite != null)
                cardImage.sprite = CardImageSprite;
            else
                Debug.LogWarning(ImageImportFailedWarningMessage);
        }

        [UsedImplicitly]
        public void ValidateImportButton()
        {
            createButton.interactable =
                !string.IsNullOrEmpty(CardName) && CardImageUri != null && CardImageUri.IsAbsoluteUri;
        }

        [UsedImplicitly]
        public void StartImport()
        {
            if (CardImageUri != null && !CardImageUri.AbsoluteUri.EndsWith(CardGameManager.Current.CardImageFileType))
                CardGameManager.Instance.Messenger.Show(
                    "WARNING!: Image file type does not match " + CardGameManager.Current.CardImageFileType, true);

            StartCoroutine(ImportCard());
        }

        private IEnumerator ImportCard()
        {
            ValidateImportButton();
            if (!createButton.interactable)
                yield break;

            createButton.interactable = false;

            var card = new UnityCard(CardGameManager.Current,
                    string.IsNullOrEmpty(cardIdInputField.text)
                        ? Guid.NewGuid().ToString().ToUpper()
                        : cardIdInputField.text, CardName,
                    string.IsNullOrEmpty(setCodeInputField.text) ? Set.DefaultCode : setCodeInputField.text, null,
                    false)
                {ImageWebUrl = CardImageUri.AbsoluteUri};
            yield return UnityFileMethods.SaveUrlToFile(CardImageUri.AbsoluteUri, card.ImageFilePath);

            if (!File.Exists(card.ImageFilePath))
            {
                Debug.LogWarning(ImageImportFailedWarningMessage);
                yield break;
            }

            CardGameManager.Current.Add(card);
            _onCreationCallback?.Invoke();

            ValidateImportButton();
            Hide();
        }
    }
}
