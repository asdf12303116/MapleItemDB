using MapleItemDB.Application.Contracts;

namespace MapleItemDB.Application.UseCases;

public interface IExtractAndImportUseCase
{
    Task<ExtractImportResult> ExecuteAsync(ExtractImportRequest request);
}
