namespace NQueen.GUI.Infrastructure;

public class SaveFileDialogService : ISaveFileDialogService
{
    public string? ShowSaveFileDialog()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog();
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public void SaveContent(string content)
    {
        var filePath = ShowSaveFileDialog();
        if (!string.IsNullOrEmpty(filePath))
        {
            System.IO.File.WriteAllText(filePath, content);
        }
    }
}
