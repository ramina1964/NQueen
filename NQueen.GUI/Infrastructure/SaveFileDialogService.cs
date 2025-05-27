namespace NQueen.GUI.Infrastructure;

public class SaveFileDialogService : ISaveFileDialogService
{
    public string? ShowSaveFileDialog()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog();
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public void SaveContent(string filePath, string content)
    {
        if (string.IsNullOrEmpty(filePath) == false)
        {
            System.IO.File.WriteAllText(filePath, content);
        }
    }
}
