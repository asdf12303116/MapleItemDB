using MapleItemDB.Application.Contracts;

namespace MapleItemDB.Application.UseCases;

public interface IUpdateSnDataUseCase
{
    Task<UpdateSnDataResult> ExecuteAsync(UpdateSnDataRequest request);
}
