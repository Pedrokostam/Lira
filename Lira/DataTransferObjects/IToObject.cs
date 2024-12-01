namespace Lira.DataTransferObjects;

public interface IToObject<TObject>
{
    TObject ToObject();
}
