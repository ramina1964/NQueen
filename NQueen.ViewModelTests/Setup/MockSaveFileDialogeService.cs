namespace NQueen.ViewModelTests.Setup;

public class MockSaveFileDialogService : ISaveFileDialogService
{
    public bool WasCalled { get; private set; }

    public string? SavedContent { get; private set; }

    public string? ShowSaveFileDialog()
    {
        WasCalled = true;
        return "mockedFilePath.txt";
    }

    public void SaveContent(string content) =>
        SavedContent = content;
}
