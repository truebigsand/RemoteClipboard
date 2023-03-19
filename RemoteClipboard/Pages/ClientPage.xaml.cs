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
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Storage;
using Microsoft.UI.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using RemoteClipboard.Helper;
using WatsonWebsocket;
using CommunityToolkit.Mvvm.Input;
using System.Text;
using System.Net.Sockets;
using System.Text.RegularExpressions;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace RemoteClipboard.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [ObservableObject]
    public sealed partial class ClientPage : Page
    {
        private WatsonWsClient ws = null;
        public ClientPage()
        {
            this.InitializeComponent();

            NavigationCacheMode = NavigationCacheMode.Enabled;
        }
        
        private void ServerAddressTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                ConnectButton_Click(sender, e);
            }
        }
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (ws != null)
            {
                ws.Dispose();
                ws = null;
            }
            try
            {
                ws = new WatsonWsClient(new Uri(ServerAddressTextBox.Text));
                ws.ServerConnected += (sender, e) =>
                {
                    Win32Helper.GetMessageDialog($"Connected to server!").ShowAsync().AsTask().Wait();
                };
                ws.MessageReceived += (sender, e) =>
                {
                    editor.DispatcherQueue.TryEnqueue(() =>
                    {
                        editor.Document.SetText(TextSetOptions.FormatRtf, Encoding.UTF8.GetString(e.Data));
                    });
                };
                ws.Start();
            }
            catch (Exception ex)
            {
                Win32Helper.GetMessageDialog(ex.Message, "Error").ShowAsync().AsTask().Wait();
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
    }
}
