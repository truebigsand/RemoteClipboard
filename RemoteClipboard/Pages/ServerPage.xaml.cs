// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WatsonWebsocket;
using RemoteClipboard.Helper;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Storage;
using Microsoft.UI.Text;
using System.Text;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace RemoteClipboard.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [ObservableObject]
    public sealed partial class ServerPage : Page
    {
        public ServerPage()
        {
            this.InitializeComponent();

            IsBoldButtonChecked = false;
            IsItalicButtonChecked = false;

            NavigationCacheMode = NavigationCacheMode.Enabled;
        }
        private WatsonWsServer ws = null;
        [ObservableProperty]
        private int clientCount = 0;
        private ArraySegment<byte> lastData = null;
        private async void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            var open = Win32Helper.GetFileOpenPicker();
            open.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            open.FileTypeFilter.Add(".rtf");

            Windows.Storage.StorageFile file = await open.PickSingleFileAsync();

            if (file != null)
            {
                using (Windows.Storage.Streams.IRandomAccessStream randAccStream =
                    await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    // Load the file into the Document property of the RichEditBox.
                    editor.Document.LoadFromStream(Microsoft.UI.Text.TextSetOptions.FormatRtf, randAccStream);
                }
            }
        }
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker savePicker = Win32Helper.GetFileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("Rich Text", new List<string>() { ".rtf" });

            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = "New Document";

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Prevent updates to the remote version of the file until we
                // finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);
                // write to file
                using (Windows.Storage.Streams.IRandomAccessStream randAccStream =
                    await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                {
                    editor.Document.SaveToStream(Microsoft.UI.Text.TextGetOptions.FormatRtf, randAccStream);
                }

                // Let Windows know that we're finished changing the file so the
                // other app can update the remote version of the file.
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                if (status != FileUpdateStatus.Complete)
                {
                    Windows.UI.Popups.MessageDialog errorBox =
                        new Windows.UI.Popups.MessageDialog("File " + file.Name + " couldn't be saved.");
                    await errorBox.ShowAsync();
                }
            }
        }
        [ObservableProperty]
        private bool? isBoldButtonChecked;
        [ObservableProperty]
        private bool? isItalicButtonChecked;

        private void BoldButton_Click(object sender, RoutedEventArgs e)
        {
            editor.Document.Selection.CharacterFormat.Bold = FormatEffect.Toggle;
            IsBoldButtonChecked = !IsBoldButtonChecked.Value;
        }
        private void ItalicButton_Click(object sender, RoutedEventArgs e)
        {
            editor.Document.Selection.CharacterFormat.Italic = FormatEffect.Toggle;
            IsItalicButtonChecked = !IsItalicButtonChecked.Value;
        }
        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            // Extract the color of the button that was clicked.
            Button clickedColor = (Button)sender;
            var rectangle = (Microsoft.UI.Xaml.Shapes.Rectangle)clickedColor.Content;
            var color = ((Microsoft.UI.Xaml.Media.SolidColorBrush)rectangle.Fill).Color;

            editor.Document.Selection.CharacterFormat.ForegroundColor = color;

            fontColorButton.Flyout.Hide();
            editor.Focus(Microsoft.UI.Xaml.FocusState.Keyboard);
        }
        private void OpenServerSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (OpenServerSwitch.IsOn)
            {
                try
                {
                    ws = new WatsonWsServer(new Uri(ServerAddressTextBox.Text.Replace("ws", "http")));
                    ws.ClientConnected += (sender, e) =>
                    {
                        clientCountTextBlock.DispatcherQueue.TryEnqueue(() =>
                        {
                            ClientCount++;
                        });
                        editor.DispatcherQueue.TryEnqueue(() =>
                        {
                            editor.Document.GetText(TextGetOptions.FormatRtf, out var text);
                            ws.SendAsync(e.Client.Guid, Encoding.UTF8.GetBytes(text));
                        });
                    };
                    ws.ClientDisconnected += (sender, e) =>
                    {
                        clientCountTextBlock.DispatcherQueue.TryEnqueue(() =>
                        {
                            ClientCount--;
                        });
                    };
                }
                catch (Exception ex)
                {
                    Win32Helper.GetMessageDialog(ex.Message, "Error").ShowAsync().AsTask().Wait();
                    OpenServerSwitch.IsOn = false;
                    return;
                }
                ws.Start();
            }
            else
            {
                //ws.Stop();
                if (ws != null)
                {
                    ws.Dispose();
                }
                ws = null;
            }
        }
        private void ServerAddressTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                OpenServerSwitch.IsOn = false;
                OpenServerSwitch_Toggled(sender, e);
                OpenServerSwitch.IsOn = true;
            }
        }
        private void Editor_TextChanged(object sender, RoutedEventArgs e)
        {
            if (ws != null)
            {
                editor.Document.GetText(TextGetOptions.FormatRtf, out var text);
                foreach (var client in ws.ListClients())
                {
                    ws.SendAsync(client.Guid, Encoding.UTF8.GetBytes(text));
                }
            }
        }
    }
}
