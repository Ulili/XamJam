﻿#region

using System;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading;
using FFImageLoading.Forms;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Plugin.XamJam.BugHound;
using PropertyChanged;
using Xamarin.Forms;

#endregion

namespace XamJam.PicSelector
{
    [ImplementPropertyChanged]
    public class PicSelectorViewModel
    {
        private static readonly IBugHound logger = BugHound.ByType(typeof(PicSelectorViewModel));

        private readonly PhotoSelectorOptions options;

        public PicSelectorViewModel(Page page, PhotoSelectorOptions options)
        {
            this.options = options;
            CancelCommand = new Command(async () =>
            {
                CropViewModel.PicSelectionResult.UserCancelled = true;
                await Application.Current.MainPage.Navigation.PopModalAsync();
            });
            DoneCommand = new Command(async () =>
            {
                CropViewModel.PicSelectionResult.UserCancelled = false;
                await Application.Current.MainPage.Navigation.PopModalAsync();
            });
            ChangePhotoCommand = new Command(async fake =>
            {
                await CrossMedia.Current.Initialize();

                var canTakePhoto = CrossMedia.Current.IsTakePhotoSupported;
                var canSelectPhoto = CrossMedia.Current.IsPickPhotoSupported;
                string selectedOption = null;
                if (canTakePhoto && canSelectPhoto)
                {
                    var buttonText = new[] { options.TakePhotoText, options.SelectPhotoText };
                    selectedOption = await page.DisplayActionSheet(null, CancelText, null, buttonText);
                }
                else if (canTakePhoto)
                {
                    selectedOption = options.TakePhotoText;
                }
                else if (canSelectPhoto)
                {
                    selectedOption = options.SelectPhotoText;
                }
                else
                {
                    //TODO: How to internationalize these strings
                    await page.DisplayAlert("No Photo Support", "This device does not support taking or selecting photos", "Ok");
                    selectedOption = null;
                }

                MediaFile selected = null;
                if (selectedOption == options.TakePhotoText)
                {
                    selected = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions());
                }
                else if (selectedOption == options.SelectPhotoText)
                {
                    selected = await CrossMedia.Current.PickPhotoAsync();
                }
                if (selected != null)
                {
                    var ci = new CachedImage
                    {
                        Source = new FileImageSource { File = selected.Path }
                    };
                    CropViewModel.LoadImage(ci);
                    logger.Info($"Setting selected photo to: {selected.Path}");
                }
            });
        }

        public void Initialize()
        {
            if (options.InitialPhoto != null)
            {
                logger.Debug($"Loading initial photo: {options.InitialPhoto}");
                var ci = new CachedImage
                {
                    Source = new UriImageSource { Uri = options.InitialPhoto }
                };
                CropViewModel.LoadImage(ci);
                //CropViewModel.PicSelectionResult.Selected = initialPic;
                logger.Debug($"Done loading initial photo: {options.InitialPhoto}");
            }
        }

        public string CancelText => options.CancelText;

        public string DoneText => options.DoneText;

        public string ChangePhotoText => options.ChangePhotoText;

        public bool CanLoadOrTakePhoto => options.AllowSelectPhoto || options.AllowTakePhoto;

        public Command CancelCommand { get; }

        public Command DoneCommand { get; }

        public Command ChangePhotoCommand { get; }

        public PicSelectorCropViewModel CropViewModel { get; } = new PicSelectorCropViewModel();
    }
}