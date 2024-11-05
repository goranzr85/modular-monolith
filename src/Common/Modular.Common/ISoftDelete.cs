namespace Modular.Common;
public interface ISoftDelete
{
    bool IsDeleted { get; }
    DateTime UtcDeletedOn { get; }
}
