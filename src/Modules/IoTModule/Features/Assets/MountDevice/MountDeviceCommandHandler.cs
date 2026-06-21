using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniPulse.Modules.IoTModule.Infrastructure.Persistence;

namespace OmniPulse.Modules.IoTModule.Features.Assets.MountDevice;

public class MountDeviceCommandHandler(IoTDbContext dbContext)
    : IRequestHandler<MountDeviceCommand, MountDeviceResponse>
{
    public async Task<MountDeviceResponse> Handle(MountDeviceCommand request, CancellationToken cancellationToken)
    {
        // 1. Cihazı getir (Kiracı filtresi devrede)
        var device = await dbContext.Devices
            .FirstOrDefaultAsync(d => d.Id == request.DeviceId, cancellationToken);

        if (device == null)
        {
            throw new KeyNotFoundException(
                $"Montaj yapılacak cihaz [{request.DeviceId}] bulunamadı veya bu kiracıya ait değil!");
        }

        string message;

        // 2. Eğer varlık belirtilmişse varlığı getir ve doğrula
        if (request.AssetId.HasValue)
        {
            var asset = await dbContext.Assets
                .FirstOrDefaultAsync(a => a.Id == request.AssetId.Value, cancellationToken);

            if (asset == null)
            {
                throw new KeyNotFoundException(
                    $"Montaj yapılacak varlık [{request.AssetId.Value}] bulunamadı veya bu kiracıya ait değil!");
            }

            // Cihazı varlığa ata
            device.AssignToAsset(asset.Id);
            message = $"[{device.Name}] cihazı başarıyla [{asset.Name}] varlığına monte edildi! 🔌🌡️";
        }
        else
        {
            // Cihazı varlıktan sök (demonte et)
            device.AssignToAsset(null);
            message = $"[{device.Name}] cihazı varlıktan başarıyla söküldü (demonte edildi).";
        }

        dbContext.Devices.Update(device);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new MountDeviceResponse(
            DeviceId: device.Id,
            AssetId: device.AssetId,
            Message: message
        );
    }
}
