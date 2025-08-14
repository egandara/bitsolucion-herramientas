public record Finding(
    string FileName,
    string FindingType,
    string Details,
    int? CellNumber = null,
    int? LineNumber = null,
    string? Content = null,
    string? CellSourceCode = null
);