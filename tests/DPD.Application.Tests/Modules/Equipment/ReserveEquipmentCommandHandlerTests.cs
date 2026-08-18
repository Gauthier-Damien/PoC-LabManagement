using DPD.Application.Common.Interfaces;
using DPD.Application.Modules.Equipment.Commands.ReserveEquipment;
using DPD.Domain.Entities;
using DPD.Domain.Enums;
using FluentAssertions;
using Moq;

namespace DPD.Application.Tests.Modules.Equipment;

public class ReserveEquipmentCommandHandlerTests
{
    private readonly Mock<IEquipmentRepository> _equipments = new();
    private readonly Mock<IResourceRepository> _resources = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ReserveEquipmentCommandHandler _handler;

    public ReserveEquipmentCommandHandlerTests()
    {
        _handler = new ReserveEquipmentCommandHandler(_equipments.Object, _resources.Object, _unitOfWork.Object);
    }

    private static Domain.Entities.Equipment CreateEquipment(EquipmentStatus status = EquipmentStatus.Available) => new()
    {
        Name = "HPLC",
        SerialNumber = "SN-1",
        Status = status,
        InstallationDate = DateOnly.FromDateTime(DateTime.UtcNow),
        CommissioningDate = DateOnly.FromDateTime(DateTime.UtcNow),
        WarrantyEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))
    };

    [Fact]
    public async Task Handle_ValidReservation_CreatesReservation()
    {
        var equipment = CreateEquipment();
        var resourceId = Guid.NewGuid();

        _equipments.Setup(e => e.FindAsync(equipment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(equipment);
        _resources.Setup(r => r.ExistsAsync(resourceId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _equipments.Setup(e => e.HasReservationConflictAsync(equipment.Id, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var command = new ReserveEquipmentCommand(equipment.Id, resourceId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(2), "Test run");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        _equipments.Verify(e => e.AddReservationAsync(It.IsAny<EquipmentReservation>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EquipmentInMaintenance_ThrowsBusinessException()
    {
        var equipment = CreateEquipment(EquipmentStatus.Maintenance);
        _equipments.Setup(e => e.FindAsync(equipment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(equipment);
        _resources.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var command = new ReserveEquipmentCommand(equipment.Id, Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), "Test");
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DPD.Application.Common.Exceptions.BusinessException>().WithMessage("*maintenance*");
    }

    [Fact]
    public async Task Handle_TimeSlotConflict_ThrowsBusinessException()
    {
        var equipment = CreateEquipment();
        _equipments.Setup(e => e.FindAsync(equipment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(equipment);
        _resources.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _equipments.Setup(e => e.HasReservationConflictAsync(equipment.Id, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var command = new ReserveEquipmentCommand(equipment.Id, Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), "Test");
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DPD.Application.Common.Exceptions.BusinessException>().WithMessage("*conflict*");
    }

    [Fact]
    public async Task Handle_EquipmentNotFound_ThrowsNotFoundException()
    {
        _equipments.Setup(e => e.FindAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.Equipment?)null);

        var command = new ReserveEquipmentCommand(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), "Test");
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DPD.Application.Common.Exceptions.NotFoundException>();
    }
}
