using Application.Features.Order.Services;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Domain.Models;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Order.Command.OrderVehicleLifecycleCommands
{
    public static class VehicleSettlementSnapshotLoader
    {
        public static async Task<Result<VehicleSettlementSnapshot>> LoadAsync(
            DatabaseContext context,
            int orderId,
            int vehicleId,
            CancellationToken cancellationToken)
        {
            var merchantDetail = await context.MerchantOrderPaymentDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.OrderId == orderId && p.VehicleId == vehicleId, cancellationToken);

            if (merchantDetail == null)
                return Result.Failure<VehicleSettlementSnapshot>("Merchant payment detail not found for vehicle. Confirm order first.");

            var deliveryDetail = await context.DeliveryOrderPaymentDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.OrderId == orderId && p.VehicleId == vehicleId, cancellationToken);

            if (deliveryDetail == null)
                return Result.Failure<VehicleSettlementSnapshot>("Delivery payment detail not found for vehicle. Assign delivery first.");

            var merchant = await context.Merchants
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MerchantId == merchantDetail.MerchantId, cancellationToken);

            if (merchant == null)
                return Result.Failure<VehicleSettlementSnapshot>("Merchant not found");

            var orderServiceFees = await context.OrderTotals
                .AsNoTracking()
                .Where(t => t.OrderId == orderId)
                .Select(t => t.ServiceFees)
                .FirstOrDefaultAsync(cancellationToken);

            return Result.Success(new VehicleSettlementSnapshot
            {
                VehicleId = vehicleId,
                MerchantId = merchantDetail.MerchantId,
                DeliveryId = deliveryDetail.DeliveryId,
                VehicleRental = merchantDetail.VehicleRental,
                OrderServiceFees = orderServiceFees,
                DeliveryFeeShare = deliveryDetail.DeliveryFeeShare,
                MerchantCashOnReceive = merchant.CashOnReceive
            });
        }

        public static async Task<Result<List<VehicleSettlementSnapshot>>> LoadAllAsync(
            DatabaseContext context,
            int orderId,
            CancellationToken cancellationToken)
        {
            var vehicleIds = await context.OrderVehicles
                .AsNoTracking()
                .Where(ov => ov.OrderId == orderId)
                .Select(ov => ov.VehicleId)
                .ToListAsync(cancellationToken);

            var list = new List<VehicleSettlementSnapshot>();
            foreach (var vehicleId in vehicleIds)
            {
                var loaded = await LoadAsync(context, orderId, vehicleId, cancellationToken);
                if (loaded.IsFailure)
                    return Result.Failure<List<VehicleSettlementSnapshot>>(loaded.Error);
                list.Add(loaded.Value);
            }

            return Result.Success(list);
        }
    }

    public record MarkVehicleReceivedFromOwnerCommand : IRequest<Result<bool>>
    {
        public int OrderId { get; set; }
        public int VehicleId { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class MarkVehicleReceivedFromOwnerCommandHandler
        : IRequestHandler<MarkVehicleReceivedFromOwnerCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IImageService _imageService;
        private readonly IOrderRealtimeNotifier _realtime;

        public MarkVehicleReceivedFromOwnerCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IImageService imageService,
            IOrderRealtimeNotifier realtime)
        {
            _context = context;
            _userSession = userSession;
            _imageService = imageService;
            _realtime = realtime;
        }

        public async Task<Result<bool>> Handle(MarkVehicleReceivedFromOwnerCommand request, CancellationToken cancellationToken)
        {
            var order = await LoadOrderAsync(request.OrderId, cancellationToken);
            if (order == null)
                return Result.Failure<bool>($"Order {request.OrderId} not found");

            var snapshotResult = await VehicleSettlementSnapshotLoader.LoadAsync(
                _context, request.OrderId, request.VehicleId, cancellationToken);
            if (snapshotResult.IsFailure)
                return Result.Failure<bool>(snapshotResult.Error);

            var imagePath = PersistOptionalImage(request.ImageUrl, "order-vehicles");
            var actor = _userSession.UserName ?? "System";

            try
            {
                order.MarkVehicleReceivedFromOwner(snapshotResult.Value, imagePath, actor);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                return Result.Failure<bool>(ex.Message);
            }

            var assignment = await _context.DeliveryMenOrders
                .AsTracking()
                .FirstOrDefaultAsync(d => d.OrderId == request.OrderId && d.VehicleId == request.VehicleId, cancellationToken);
            assignment?.MarkReceivedFromMerchant(actor);

            await _context.SaveChangesAsync(cancellationToken);

            await _realtime.NotifyAsync(
                request.OrderId,
                "Vehicle received from owner",
                $"Vehicle {request.VehicleId} was received from owner.",
                NotificationType.OrderUpdated,
                cancellationToken: cancellationToken);

            return Result.Success(true);
        }

        private Task<Domain.Models.Order?> LoadOrderAsync(int orderId, CancellationToken ct) =>
            _context.Orders
                .AsTracking()
                .Include(o => o.OrderVehicles)
                .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

        private string? PersistOptionalImage(string? imageUrl, string folder)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return null;
            if (_imageService.IsBase64String(imageUrl))
                return _imageService.SaveBase64Image(imageUrl, folder);
            return imageUrl;
        }
    }

    public record MarkVehicleDeliveredToCustomerCommand : IRequest<Result<bool>>
    {
        public int OrderId { get; set; }
        public int VehicleId { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class MarkVehicleDeliveredToCustomerCommandHandler
        : IRequestHandler<MarkVehicleDeliveredToCustomerCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IImageService _imageService;
        private readonly IOrderRealtimeNotifier _realtime;

        public MarkVehicleDeliveredToCustomerCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IImageService imageService,
            IOrderRealtimeNotifier realtime)
        {
            _context = context;
            _userSession = userSession;
            _imageService = imageService;
            _realtime = realtime;
        }

        public async Task<Result<bool>> Handle(MarkVehicleDeliveredToCustomerCommand request, CancellationToken cancellationToken)
        {
            var order = await _context.Orders
                .AsTracking()
                .Include(o => o.OrderVehicles)
                .Include(o => o.OrderPayments)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
                return Result.Failure<bool>($"Order {request.OrderId} not found");

            var snapshotResult = await VehicleSettlementSnapshotLoader.LoadAsync(
                _context, request.OrderId, request.VehicleId, cancellationToken);
            if (snapshotResult.IsFailure)
                return Result.Failure<bool>(snapshotResult.Error);

            var imagePath = string.IsNullOrWhiteSpace(request.ImageUrl)
                ? null
                : (_imageService.IsBase64String(request.ImageUrl)
                    ? _imageService.SaveBase64Image(request.ImageUrl, "order-vehicles")
                    : request.ImageUrl);

            var actor = _userSession.UserName ?? "System";

            try
            {
                order.MarkVehicleDeliveredToCustomer(snapshotResult.Value, imagePath, actor);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                return Result.Failure<bool>(ex.Message);
            }

            if (order.PaymentMethodId == (int)PaymentMethod.Cash
                && order.OrderState == OrderState.CustomerReceived)
            {
                var orderPayment = order.OrderPayments.FirstOrDefault();
                if (orderPayment != null && orderPayment.State == PaymentState.Pending)
                    orderPayment.MarkAsPaid(actor);
            }

            await _context.SaveChangesAsync(cancellationToken);

            await _realtime.NotifyAsync(
                request.OrderId,
                "Vehicle delivered to customer",
                $"Vehicle {request.VehicleId} was delivered to the customer.",
                NotificationType.OrderUpdated,
                cancellationToken: cancellationToken);

            return Result.Success(true);
        }
    }

    public record MarkVehicleReceivedFromCustomerCommand : IRequest<Result<bool>>
    {
        public int OrderId { get; set; }
        public int VehicleId { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class MarkVehicleReceivedFromCustomerCommandHandler
        : IRequestHandler<MarkVehicleReceivedFromCustomerCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IImageService _imageService;
        private readonly IOrderRealtimeNotifier _realtime;

        public MarkVehicleReceivedFromCustomerCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IImageService imageService,
            IOrderRealtimeNotifier realtime)
        {
            _context = context;
            _userSession = userSession;
            _imageService = imageService;
            _realtime = realtime;
        }

        public async Task<Result<bool>> Handle(MarkVehicleReceivedFromCustomerCommand request, CancellationToken cancellationToken)
        {
            var order = await _context.Orders
                .AsTracking()
                .Include(o => o.OrderVehicles)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
                return Result.Failure<bool>($"Order {request.OrderId} not found");

            var imagePath = string.IsNullOrWhiteSpace(request.ImageUrl)
                ? null
                : (_imageService.IsBase64String(request.ImageUrl)
                    ? _imageService.SaveBase64Image(request.ImageUrl, "order-vehicles")
                    : request.ImageUrl);

            var actor = _userSession.UserName ?? "System";
            try
            {
                order.MarkVehicleReceivedFromCustomer(request.VehicleId, imagePath, actor);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                return Result.Failure<bool>(ex.Message);
            }

            await _context.SaveChangesAsync(cancellationToken);

            await _realtime.NotifyAsync(
                request.OrderId,
                "Vehicle received from customer",
                $"Vehicle {request.VehicleId} was received from the customer.",
                NotificationType.OrderUpdated,
                cancellationToken: cancellationToken);

            return Result.Success(true);
        }
    }

    public record MarkVehicleDeliveredToOwnerCommand : IRequest<Result<bool>>
    {
        public int OrderId { get; set; }
        public int VehicleId { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class MarkVehicleDeliveredToOwnerCommandHandler
        : IRequestHandler<MarkVehicleDeliveredToOwnerCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IImageService _imageService;
        private readonly IOrderRealtimeNotifier _realtime;

        public MarkVehicleDeliveredToOwnerCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IImageService imageService,
            IOrderRealtimeNotifier realtime)
        {
            _context = context;
            _userSession = userSession;
            _imageService = imageService;
            _realtime = realtime;
        }

        public async Task<Result<bool>> Handle(MarkVehicleDeliveredToOwnerCommand request, CancellationToken cancellationToken)
        {
            var order = await _context.Orders
                .AsTracking()
                .Include(o => o.OrderVehicles)
                    .ThenInclude(ov => ov.Vehicle)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
                return Result.Failure<bool>($"Order {request.OrderId} not found");

            var imagePath = string.IsNullOrWhiteSpace(request.ImageUrl)
                ? null
                : (_imageService.IsBase64String(request.ImageUrl)
                    ? _imageService.SaveBase64Image(request.ImageUrl, "order-vehicles")
                    : request.ImageUrl);

            var actor = _userSession.UserName ?? "System";
            try
            {
                order.MarkVehicleDeliveredToOwner(request.VehicleId, imagePath, actor);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                return Result.Failure<bool>(ex.Message);
            }

            if (order.OrderState == OrderState.Completed)
            {
                foreach (var ov in order.OrderVehicles)
                    ov.Vehicle.UpdateStatus(VehicleStatus.Available, actor);
            }

            await _context.SaveChangesAsync(cancellationToken);

            await _realtime.NotifyAsync(
                request.OrderId,
                "Vehicle delivered to owner",
                $"Vehicle {request.VehicleId} was returned to the owner.",
                NotificationType.OrderUpdated,
                cancellationToken: cancellationToken);

            return Result.Success(true);
        }
    }

    public record MarkVehicleNotReceivedByCustomerCommand : IRequest<Result<bool>>
    {
        public int OrderId { get; set; }
        public int VehicleId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public FaultParty FaultParty { get; set; }
    }

    public class MarkVehicleNotReceivedByCustomerCommandHandler
        : IRequestHandler<MarkVehicleNotReceivedByCustomerCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IOrderRealtimeNotifier _realtime;

        public MarkVehicleNotReceivedByCustomerCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IOrderRealtimeNotifier realtime)
        {
            _context = context;
            _userSession = userSession;
            _realtime = realtime;
        }

        public async Task<Result<bool>> Handle(MarkVehicleNotReceivedByCustomerCommand request, CancellationToken cancellationToken)
        {
            var order = await _context.Orders
                .AsTracking()
                .Include(o => o.OrderVehicles)
                    .ThenInclude(ov => ov.Vehicle)
                .Include(o => o.ReservedVehiclesPerDays)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
                return Result.Failure<bool>($"Order {request.OrderId} not found");

            var actor = _userSession.UserName ?? "System";
            try
            {
                order.CancelVehicleNotReceived(request.VehicleId, actor);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                return Result.Failure<bool>(ex.Message);
            }

            var reservations = order.ReservedVehiclesPerDays
                .Where(r => r.VehicleId == request.VehicleId)
                .ToList();
            foreach (var reservation in reservations)
                reservation.Cancel(actor);

            var link = order.OrderVehicles.FirstOrDefault(ov => ov.VehicleId == request.VehicleId);
            if (link?.Vehicle != null)
            {
                var stillBookedElsewhere = await _context.ReservedVehiclesPerDays
                    .AsNoTracking()
                    .AnyAsync(rv =>
                        rv.VehicleId == request.VehicleId
                        && rv.OrderId != request.OrderId
                        && rv.State == ReservedVehicleState.StillBooked
                        && rv.Order.OrderState != OrderState.Completed
                        && rv.Order.OrderState != OrderState.Cancelled,
                        cancellationToken);

                if (!stillBookedElsewhere)
                    link.Vehicle.UpdateStatus(VehicleStatus.Available, actor);
            }

            if (order.OrderState == OrderState.Cancelled)
            {
                var leftover = order.ReservedVehiclesPerDays.Where(r => r.State == ReservedVehicleState.StillBooked).ToList();
                foreach (var reservation in leftover)
                    reservation.Cancel(actor);
            }

            await _context.SaveChangesAsync(cancellationToken);

            await _realtime.NotifyAsync(
                request.OrderId,
                "Vehicle cancelled",
                $"Vehicle {request.VehicleId} was cancelled on the order.",
                NotificationType.OrderUpdated,
                cancellationToken: cancellationToken);

            return Result.Success(true);
        }
    }
}
