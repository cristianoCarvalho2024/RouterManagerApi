using System;
using System.Collections.Generic;

namespace RouterManager.Api.Models.UpdateOrders;

public record UpdateActionDto(string ActionType, string Payload);

// Create/Update requests now include targeting criteria so updates can be matched by the device
public record CreateUpdateOrderRequest(
    string Name,
    int ProviderId,
    string ModelIdentifier,
    string? FirmwareVersion,
    string? SerialNumber,
    List<UpdateActionDto> Actions
);

public record UpdateUpdateOrderRequest(
    int Id,
    string Name,
    int ProviderId,
    string ModelIdentifier,
    string? FirmwareVersion,
    string? SerialNumber,
    List<UpdateActionDto> Actions
);

// Detail DTO returned to AdminWeb
public record UpdateOrderDetailDto(
    int Id,
    string Name,
    int ProviderId,
    string ModelIdentifier,
    string? FirmwareVersion,
    string? SerialNumber,
    string RequestPayload,
    DateTime CreatedAt
);
