using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hat.Views.Controls;

public partial class ChatArea : UserControl
{
    public ChatArea()
    {
        InitializeComponent();
        DataContext = App.AssistantVM;
    }

    private async void Send_Click(object sender, RoutedEventArgs e)
    {
        await App.AssistantVM.SendManualMessageAsync();
        ChatScrollViewer.ScrollToEnd();
    }

    private async void ChatInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
        {
            e.Handled = true;
            await App.AssistantVM.SendManualMessageAsync();
            ChatScrollViewer.ScrollToEnd();
        }
    }

    private void ChatInput_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void ChatInput_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
        // TODO: Process dropped files as attachments
        foreach (var file in files)
        {
            System.Diagnostics.Debug.WriteLine($"Dropped file: {file}");
        }
    }
}
