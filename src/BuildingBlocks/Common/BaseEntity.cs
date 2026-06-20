using System;

namespace OmniPulse.BuildingBlocks.Common;

/// <summary>
/// Tüm domain nesnelerimizin kimlik kartı olan temel sınıf! 💳
/// </summary>
public abstract class BaseEntity<TId>
{
    // Nesnenin benzersiz kimliği (Primary Key)
    public TId Id { get; protected init; } = default!;
}

/// <summary>
/// Varsayılan olarak Guid Id kullanan temel sınıf!
/// </summary>
public abstract class BaseEntity : BaseEntity<Guid>
{
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
    }
}
