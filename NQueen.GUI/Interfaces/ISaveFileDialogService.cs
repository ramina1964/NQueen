namespace NQueen.GUI.Interfaces;

public interface ISaveFileDialogService
{
    string? ShowSaveFileDialog();

    void SaveContent(string filePath, string content);
}
