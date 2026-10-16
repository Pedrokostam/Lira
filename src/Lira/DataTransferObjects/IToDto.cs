namespace Lira.DataTransferObjects;

public interface IToDto<TDto>
{
    TDto ToDto();
}
