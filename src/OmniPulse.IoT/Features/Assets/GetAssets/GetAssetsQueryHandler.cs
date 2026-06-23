using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.IoT.Infrastructure.Persistence;

namespace OmniPulse.IoT.Features.Assets.GetAssets;

public class GetAssetsQueryHandler(IoTDbContext dbContext)
    : IRequestHandler<GetAssetsQuery, IEnumerable<AssetDto>>
{
    public async Task<IEnumerable<AssetDto>> Handle(GetAssetsQuery request, CancellationToken cancellationToken)
    {
        // ─────────────────────────────────────────────────────────────────────
        // MOD 1: Recursive CTE — RootAssetId verilmiş ve IncludeDescendants açık
        //
        // PostgreSQL WITH RECURSIVE sözdizimi:
        //   1. "Anchor" (Başlangıç): Kök varlığı bul
        //   2. "Recursive" (Tekrarlayan): Her turda bir önceki turun çocuklarını bul
        //   3. PostgreSQL, hiç çocuk kalmayıncaya kadar kendi kendine devam eder
        //
        // Örnek: RootAssetId = "Fabrika-A" verildiğinde;
        //   Tur 1: [Fabrika-A]
        //   Tur 2: [Bant-1, Bant-2]       ← Fabrika-A'nın çocukları
        //   Tur 3: [Eksen-1, Motor-A, Soğutucu-7]  ← Bantların çocukları
        //   Tur 4: Çocuk yok → DUR. Tek sorguda tüm ağaç geldi. ✅
        // ─────────────────────────────────────────────────────────────────────
        // ─────────────────────────────────────────────────────────────────────
        // MOD 1: Tek kökten recursive — verilen RootAssetId'nin tüm alt ağacı
        // ─────────────────────────────────────────────────────────────────────
        if (request.RootAssetId.HasValue && request.IncludeDescendants)
        {
            return await FetchDescendantsWithRecursiveCte(
                request.RootAssetId.Value,
                request.TypeFilter,
                cancellationToken);
        }

        // ─────────────────────────────────────────────────────────────────────
        // MOD 3: Çok-köklü recursive — bir kullanıcının BÜTÜN sorumlu ağaçları
        //
        // Senaryo: Hüseyin Bey hem Bant-1 hem Bant-2'nin şefi.
        //   ANCHOR → [Bant-1, Bant-2]                    (ikisi de Hüseyin'in)
        //   TUR 1  → [Eksen-1, Motor-A, Soğutucu-7]      (ikisinin çocukları)
        //   TUR 2  → daha derin nesiller varsa...
        //   SONUÇ  → tümü tek sorguda gelir ✅
        // ─────────────────────────────────────────────────────────────────────
        if (request.ResponsibleUserId.HasValue && request.IncludeDescendants)
        {
            return await FetchByResponsibleUserWithRecursiveCte(
                request.ResponsibleUserId.Value,
                request.TypeFilter,
                cancellationToken);
        }

        // ─────────────────────────────────────────────────────────────────────
        // MOD 2: Standart LINQ sorgusu (eski davranış korundu)
        // ─────────────────────────────────────────────────────────────────────
        var query = dbContext.Assets
            .Include(a => a.Devices)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.TypeFilter))
            query = query.Where(a => a.Type == request.TypeFilter);

        if (request.ParentAssetId.HasValue)
            query = query.Where(a => a.ParentAssetId == request.ParentAssetId.Value);

        var items = await query
            .OrderBy(a => a.Name)
            .Select(a => new AssetDto(
                a.Id,
                a.Name,
                a.Type,
                a.Type,
                a.ParentAssetId,
                a.ResponsibleUserId,
                a.MetadataJson,
                a.Devices.Count,
                Depth: 0
            ))
            .ToListAsync(cancellationToken);

        return items;
    }

    /// <summary>
    /// PostgreSQL WITH RECURSIVE CTE kullanarak verilen kök varlıktan başlayıp
    /// tüm alt nesilleri (children, grandchildren, ...) tek bir SQL round-trip ile çeker.
    ///
    /// Neden EF Core LINQ değil de ham SQL?
    /// EF Core 8 itibarıyla hiyerarşik/recursive sorguları doğrudan LINQ olarak
    /// ifade edemezsiniz. FROM SQL Raw + DTO projeksiyonu burada en temiz yol.
    /// </summary>
    /// <summary>
    /// MOD 1 — Tek kök, tüm torunlar.
    /// Verilen rootAssetId'den başlayıp tüm alt nesilleri tek SQL round-trip ile çeker.
    /// </summary>
    private async Task<IEnumerable<AssetDto>> FetchDescendantsWithRecursiveCte(
        Guid rootAssetId,
        string? typeFilter,
        CancellationToken cancellationToken)
    {
        // TypeFilter isteğe bağlı WHERE koşulu
        // SQL injection riski yok çünkü değeri parametre olarak geçiyoruz,
        // yalnızca koşulun varlığını string interpolation ile belirliyoruz.
        var typeFilterClause = string.IsNullOrWhiteSpace(typeFilter)
            ? string.Empty
            : "AND a.\"Type\" = {1}";

        // typeFilterClause boşsa tek parametre {0}, doluysa {0} ve {1} kullanılır.
        var sql = @"
            WITH RECURSIVE asset_tree AS (

                -- ANCHOR: Kök varlığı bul (1 kez çalışır)
                SELECT
                    a.""Id"",
                    a.""Name"",
                    a.""Type"",
                    a.""ParentAssetId"",
                    a.""ResponsibleUserId"",
                    a.""MetadataJson"",
                    0 AS ""Depth""
                FROM ""Assets"" a
                WHERE a.""Id"" = {0}
                  AND a.""IsDeleted"" = FALSE
                  " + typeFilterClause + @"

                UNION ALL

                -- RECURSIVE: Her turda bir önceki neslin çocuklarını bul
                SELECT
                    child.""Id"",
                    child.""Name"",
                    child.""Type"",
                    child.""ParentAssetId"",
                    child.""ResponsibleUserId"",
                    child.""MetadataJson"",
                    parent.""Depth"" + 1
                FROM ""Assets"" child
                INNER JOIN asset_tree parent
                    ON child.""ParentAssetId"" = parent.""Id""
                WHERE child.""IsDeleted"" = FALSE
                  " + typeFilterClause + @"
            )

            -- FINAL: CTE sonucunu cihaz sayısıyla birleştir
            SELECT
                t.""Id"",
                t.""Name"",
                t.""Type"",
                t.""ParentAssetId"",
                t.""ResponsibleUserId"",
                t.""MetadataJson"",
                t.""Depth"",
                COUNT(d.""Id"") AS ""DeviceCount""
            FROM asset_tree t
            LEFT JOIN ""Devices"" d
                ON d.""AssetId"" = t.""Id""
               AND d.""IsDeleted"" = FALSE
            GROUP BY
                t.""Id"", t.""Name"", t.""Type"",
                t.""ParentAssetId"", t.""ResponsibleUserId"",
                t.""MetadataJson"", t.""Depth""
            ORDER BY t.""Depth"", t.""Name"";
            ";

        // EF Core FromSqlRaw → DbSet yönlendirmesi yapamayız çünkü sonuç AssetDto,
        // keyless bir tip. Bu yüzden DbContext.Database.SqlQueryRaw<T> kullanıyoruz.
        var sqlParams = string.IsNullOrWhiteSpace(typeFilter)
            ? new object[] { rootAssetId }
            : new object[] { rootAssetId, typeFilter };

        // EF Core 8 — Database.SqlQuery<T> keyless result sets destekler
        var results = await dbContext.Database
            .SqlQueryRaw<AssetTreeRow>(sql, sqlParams)
            .ToListAsync(cancellationToken);

        return results.Select(r => new AssetDto(
            r.Id,
            r.Name,
            r.Type,
            r.Type,
            r.ParentAssetId,
            r.ResponsibleUserId,
            r.MetadataJson,
            (int)r.DeviceCount,
            r.Depth
        ));
    }

    /// <summary>
    /// MOD 3 — Çok köklü recursive CTE.
    ///
    /// ANCHOR: "Hüseyin'in sorumlu olduğu tüm varlıkları bul" — birden fazla satır döner.
    /// RECURSIVE: Her kökün altındaki nesilleri tara.
    /// PostgreSQL birden fazla ANCHOR satırını aynı anda paralel olarak işler.
    ///
    /// Bir kullanıcı N farklı kolun şefi olsa bile, N ayrı sorgu değil, tek bir WITH RECURSIVE yeterli.
    /// </summary>
    private async Task<IEnumerable<AssetDto>> FetchByResponsibleUserWithRecursiveCte(
        Guid responsibleUserId,
        string? typeFilter,
        CancellationToken cancellationToken)
    {
        var typeFilterClause = string.IsNullOrWhiteSpace(typeFilter)
            ? string.Empty
            : "AND a.\"Type\" = {1}";

        var sql = @"
            WITH RECURSIVE asset_tree AS (

                -- ANCHOR: Kullanıcının DOĞRUDAN sorumlu olduğu tüm varlıklar (veya izinli olduğu yeni atamalar)
                -- Bant-1 ve Bant-2 burada ikisi birden gelir — çok köklü başlangıç!
                SELECT
                    a.""Id"",
                    a.""Name"",
                    a.""Type"",
                    a.""ParentAssetId"",
                    a.""ResponsibleUserId"",
                    a.""MetadataJson"",
                    0 AS ""Depth""
                FROM ""Assets"" a
                WHERE (
                    a.""ResponsibleUserId"" = {0}
                    OR a.""Id"" IN (
                        SELECT ap.""AssetId""
                        FROM ""AssetPermissions"" ap
                        WHERE ap.""UserId"" = {0} AND ap.""IsDeleted"" = FALSE
                    )
                )
                  AND a.""IsDeleted"" = FALSE
                  " + typeFilterClause + @"

                UNION ALL

                -- RECURSIVE: Her kökün (Bant-1, Bant-2) çocuklarını aynı anda tara
                -- PostgreSQL ikisini birlikte işler, 2 ayrı sorgu değil
                SELECT
                    child.""Id"",
                    child.""Name"",
                    child.""Type"",
                    child.""ParentAssetId"",
                    child.""ResponsibleUserId"",
                    child.""MetadataJson"",
                    parent.""Depth"" + 1
                FROM ""Assets"" child
                INNER JOIN asset_tree parent
                    ON child.""ParentAssetId"" = parent.""Id""
                WHERE child.""IsDeleted"" = FALSE
                  " + typeFilterClause + @"
            )

            SELECT
                t.""Id"",
                t.""Name"",
                t.""Type"",
                t.""ParentAssetId"",
                t.""ResponsibleUserId"",
                t.""MetadataJson"",
                t.""Depth"",
                COUNT(d.""Id"") AS ""DeviceCount""
            FROM asset_tree t
            LEFT JOIN ""Devices"" d
                ON d.""AssetId"" = t.""Id""
               AND d.""IsDeleted"" = FALSE
            GROUP BY
                t.""Id"", t.""Name"", t.""Type"",
                t.""ParentAssetId"", t.""ResponsibleUserId"",
                t.""MetadataJson"", t.""Depth""
            ORDER BY t.""Depth"", t.""Name"";
            ";

        var sqlParams = string.IsNullOrWhiteSpace(typeFilter)
            ? new object[] { responsibleUserId }
            : new object[] { responsibleUserId, typeFilter };

        var results = await dbContext.Database
            .SqlQueryRaw<AssetTreeRow>(sql, sqlParams)
            .ToListAsync(cancellationToken);

        return results.Select(r => new AssetDto(
            r.Id,
            r.Name,
            r.Type,
            r.Type,
            r.ParentAssetId,
            r.ResponsibleUserId,
            r.MetadataJson,
            (int)r.DeviceCount,
            r.Depth
        ));
    }

    /// <summary>
    /// EF Core'un SqlQueryRaw dönüşü için iç DTO (yalnızca bu handler içinde kullanılır).
    /// Public API'ye AssetDto ile dönüyoruz.
    /// </summary>
    private sealed record AssetTreeRow(
        Guid Id,
        string Name,
        string Type,
        Guid? ParentAssetId,
        Guid? ResponsibleUserId,
        string? MetadataJson,
        int Depth,
        long DeviceCount
    );
}
