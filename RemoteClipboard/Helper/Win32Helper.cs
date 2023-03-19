using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.UI.Popups;

namespace RemoteClipboard.Helper
{
    public static class Win32Helper
    {
        public static FileOpenPicker GetFileOpenPicker()
        {
            // Open a text file.
            FileOpenPicker open = new FileOpenPicker();
            // Retrieve the window handle (HWND) of the current WinUI 3 window.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle((App.Current as App).Window);
            // Initialize the file picker with the window handle (HWND).
            WinRT.Interop.InitializeWithWindow.Initialize(open, hWnd);
            return open;
        }
        public static FileSavePicker GetFileSavePicker()
        {
            // Open a text file.
            FileSavePicker save = new FileSavePicker();
            // Retrieve the window handle (HWND) of the current WinUI 3 window.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle((App.Current as App).Window);
            // Initialize the file picker with the window handle (HWND).
            WinRT.Interop.InitializeWithWindow.Initialize(save, hWnd);
            return save;
        }
        public static MessageDialog GetMessageDialog(string content)
        {
            // Open a text file.
            MessageDialog dialog = new MessageDialog(content);
            // Retrieve the window handle (HWND) of the current WinUI 3 window.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle((App.Current as App).Window);
            // Initialize the file picker with the window handle (HWND).
            WinRT.Interop.InitializeWithWindow.Initialize(dialog, hWnd);
            return dialog;
        }
        public static MessageDialog GetMessageDialog(string content, string title)
        {
            // Open a text file.
            MessageDialog dialog = new MessageDialog(content, title);
            // Retrieve the window handle (HWND) of the current WinUI 3 window.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle((App.Current as App).Window);
            // Initialize the file picker with the window handle (HWND).
            WinRT.Interop.InitializeWithWindow.Initialize(dialog, hWnd);
            return dialog;
        }
    }
}
