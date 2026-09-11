using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using System.Globalization;

namespace Domain.Models
{
    public class Order : IAuditable
    {
        // Private setters for encapsulation
        public int OrderId { get; private set; }
        public string OrderCode { get; private set; } = string.Empty;
        public int CustomerId { get; private set; }
        public int SubCategoryId { get; private set; }
        public int CityId { get; private set; }
        public DateTime ReservationDateFrom { get; private set; }
        public DateTime ReservationDateTo { get; private set; }
        public int VehiclesCount { get; private set; }
        public decimal OrderSubTotal { get; private set; }
        public decimal OrderTotal { get; private set; }
        public string? Notes { get; private set; }
        public string PassportImage { get; private set; } = string.Empty; // Base64 string
        public string HotelName { get; private set; } = string.Empty;
        public string HotelAddress { get; private set; } = string.Empty;
        public string? HotelPhone { get; private set; }
        public bool IsUrgent { get; private set; }
        public int PaymentMethodId { get; private set; } // PaymentMethod enum
        public OrderState OrderState { get; private set; } = OrderState.Pending;
        /// <summary>Outstanding cancellation fees collected with this order (مديونية سابقة).</summary>
        public decimal PreviousDebt { get; private set; }
        /// <summary>True when refund was completed (cash cancel is immediate; PayPal after admin confirms).</summary>
        public bool MoneyRefunded { get; private set; }
        public FaultParty? ReceiptFaultParty { get; private set; }
        public string? ReceiptRejectNote { get; private set; }

        // Navigation properties
        public Customer Customer { get; private set; } = null!;
        public SubCategory SubCategory { get; private set; } = null!;
        public City City { get; private set; } = null!;
        public ICollection<OrderVehicle> OrderVehicles { get; private set; } = new List<OrderVehicle>();
        public ICollection<OrderPayment> OrderPayments { get; private set; } = new List<OrderPayment>();
        public ICollection<ReservedVehiclesPerDays> ReservedVehiclesPerDays { get; private set; } = new List<ReservedVehiclesPerDays>();
        public ICollection<MerchantOrder> MerchantOrders { get; private set; } = new List<MerchantOrder>();
        public ICollection<MerchantOrderPaymentDetail> MerchantOrderPaymentDetails { get; private set; } = new List<MerchantOrderPaymentDetail>();
        public ICollection<DeliveryMenOrder> DeliveryMenOrders { get; private set; } = new List<DeliveryMenOrder>();
        public ICollection<DeliveryOrderPaymentDetail> DeliveryOrderPaymentDetails { get; private set; } = new List<DeliveryOrderPaymentDetail>();
        public ICollection<OrderJournal> OrderJournals { get; private set; } = new List<OrderJournal>();

        // Audit properties
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        // Private constructor for EF Core
        private Order() { }
        public static OrderPricingBreakdown CalculatePricing(
            decimal subCategoryUnitPrice,
            int vehiclesCount,
            City city,
            bool isUrgent,
            int reservationDays,
            decimal previousDebt = 0)
        {
            if (subCategoryUnitPrice < 0)
                throw new ArgumentException("SubCategory unit price cannot be negative", nameof(subCategoryUnitPrice));

            if (vehiclesCount <= 0)
                throw new ArgumentException("Vehicles count must be greater than zero", nameof(vehiclesCount));

            if (city == null)
                throw new ArgumentNullException(nameof(city));

            if (reservationDays <= 0)
                throw new ArgumentException("Reservation days must be greater than zero", nameof(reservationDays));

            if (previousDebt < 0)
                throw new ArgumentException("Previous debt cannot be negative", nameof(previousDebt));

            // Rental subtotal = unit price × vehicles × inclusive reservation days
            var subTotal = subCategoryUnitPrice * vehiclesCount * reservationDays;
            var tieredDiscountPercentage = city.CalculateTieredDiscount(reservationDays);
            // Delivery is charged once per vehicle for the reservation (not per day)
            var deliveryFees = (city.DeliveryFees ?? 0) * vehiclesCount;
            var serviceFees = city.ServiceFees ?? 0;
            var urgentFees = (isUrgent && city.UrgentDelivery.HasValue) ? city.UrgentDelivery.Value : 0;
            var tieredDiscountAmount = tieredDiscountPercentage > 0
                ? tieredDiscountPercentage * subTotal / 100
                : 0;
            var rentalTotal = subTotal + deliveryFees + serviceFees + urgentFees - tieredDiscountAmount;
            var total = rentalTotal + previousDebt;

            return new OrderPricingBreakdown(
                unitPrice: subCategoryUnitPrice,
                vehiclesCount: vehiclesCount,
                subTotal: subTotal,
                deliveryFees: deliveryFees,
                serviceFees: serviceFees,
                urgentFees: urgentFees,
                tieredDiscountPercentage: tieredDiscountPercentage,
                tieredDiscountAmount: tieredDiscountAmount,
                rentalTotal: rentalTotal,
                previousDebt: previousDebt,
                total: total);
        }

        /// <summary>
        /// Shared availability rule for selected vehicles (admin create / confirm / totals).
        /// A vehicle is bookable when it is not UnderMaintenance and has no StillBooked
        /// reservation overlapping the requested inclusive date range.
        /// </summary>
        public static Result EnsureSelectedVehiclesAreAvailable(
            IReadOnlyCollection<Vehicle> vehicles,
            int subCategoryId,
            DateTime reservationDateFrom,
            DateTime reservationDateTo,
            IReadOnlyCollection<(int VehicleId, DateTime DateFrom, DateTime DateTo)> stillBookedOverlaps)
        {
            if (vehicles == null || vehicles.Count == 0)
            {
                return Result.Failure("At least one vehicle must be selected");
            }

            var from = reservationDateFrom.Date;
            var to = reservationDateTo.Date;
            if (from > to)
            {
                return Result.Failure("Reservation date from must be on or before reservation date to");
            }

            var reservedVehicleIds = new HashSet<int>(
                stillBookedOverlaps
                    .Where(r => RangesOverlap(r.DateFrom, r.DateTo, from, to))
                    .Select(r => r.VehicleId));

            foreach (var vehicle in vehicles)
            {
                if (string.IsNullOrWhiteSpace(vehicle.VehicleCode))
                {
                    return Result.Failure($"Vehicle ID {vehicle.VehicleId} does not have a vehicle code assigned");
                }

                if (vehicle.SubCategoryId != subCategoryId)
                {
                    return Result.Failure($"Vehicle {vehicle.VehicleCode} does not belong to the selected subcategory");
                }

                if (vehicle.Status == VehicleStatus.UnderMaintenance)
                {
                    return Result.Failure($"Vehicle {vehicle.VehicleCode} is under maintenance and cannot be reserved");
                }

                if (reservedVehicleIds.Contains(vehicle.VehicleId))
                {
                    return Result.Failure($"Vehicle {vehicle.VehicleCode} is already reserved in the selected date range");
                }
            }

            return Result.Success();
        }

        /// <summary>
        /// Shared capacity rule for mobile create (count-based, no specific vehicle IDs yet).
        /// For each day in the inclusive range, bookable fleet size minus StillBooked units
        /// must be &gt;= requiredVehiclesCount.
        /// </summary>
        public static Result EnsureSubCategoryHasCapacity(
            int requiredVehiclesCount,
            int bookableVehiclesCount,
            DateTime reservationDateFrom,
            DateTime reservationDateTo,
            IReadOnlyCollection<(DateTime DateFrom, DateTime DateTo)> stillBookedOverlaps)
        {
            if (requiredVehiclesCount <= 0)
            {
                return Result.Failure("Vehicles count must be greater than zero");
            }

            var from = reservationDateFrom.Date;
            var to = reservationDateTo.Date;
            if (from > to)
            {
                return Result.Failure("Reservation date from must be on or before reservation date to");
            }

            if (bookableVehiclesCount < requiredVehiclesCount)
            {
                return Result.Failure(
                    $"Not enough vehicles in this subcategory. Available fleet: {bookableVehiclesCount}, required: {requiredVehiclesCount}");
            }

            var reservedCountByDate = new Dictionary<DateTime, int>();
            foreach (var (dateFrom, dateTo) in stillBookedOverlaps)
            {
                var current = dateFrom.Date;
                if (current < from) current = from;
                var end = dateTo.Date;
                if (end > to) end = to;

                while (current <= end)
                {
                    reservedCountByDate.TryGetValue(current, out var count);
                    reservedCountByDate[current] = count + 1;
                    current = current.AddDays(1);
                }
            }

            var unavailableDates = new List<(DateTime Date, int Available)>();
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                var reservedCount = reservedCountByDate.TryGetValue(date, out var c) ? c : 0;
                var available = bookableVehiclesCount - reservedCount;
                if (available < requiredVehiclesCount)
                {
                    unavailableDates.Add((date, Math.Max(0, available)));
                }
            }

            if (unavailableDates.Count == 0)
            {
                return Result.Success();
            }

            var details = string.Join("; ", unavailableDates.Select(x =>
                $"{x.Date.ToString("d/M/yyyy", CultureInfo.InvariantCulture)} ({x.Available} available)"));

            var message = unavailableDates.Count == 1
                ? $"On ({details}) there is not enough vehicles."
                : $"In days ({details}) there are not enough vehicles.";

            return Result.Failure(message);
        }

        public static bool RangesOverlap(DateTime leftFrom, DateTime leftTo, DateTime rightFrom, DateTime rightTo)
        {
            return leftFrom.Date <= rightTo.Date && leftTo.Date >= rightFrom.Date;
        }

        // Factory method for creating orders
        public static Order Create(
            int customerId,
            int subCategoryId,
            int cityId,
            DateTime reservationDateFrom,
            DateTime reservationDateTo,
            int vehiclesCount,
            decimal orderSubTotal,
            decimal orderTotal,
            string passportImage,
            string hotelName,
            string hotelAddress,
            int paymentMethodId,
            bool isUrgent,
            string orderCode,
            string? hotelPhone = null,
            string? notes = null,
            string? createdBy = null,
            decimal previousDebt = 0)
        {
            if (customerId <= 0)
                throw new ArgumentException("Customer ID must be greater than zero", nameof(customerId));

            if (subCategoryId <= 0)
                throw new ArgumentException("SubCategory ID must be greater than zero", nameof(subCategoryId));

            if (cityId <= 0)
                throw new ArgumentException("City ID must be greater than zero", nameof(cityId));

            if (reservationDateFrom.Date > reservationDateTo.Date)
                throw new ArgumentException("Reservation date from must be on or before reservation date to", nameof(reservationDateFrom));

            if (vehiclesCount <= 0)
                throw new ArgumentException("Vehicles count must be greater than zero", nameof(vehiclesCount));

            if (orderSubTotal < 0)
                throw new ArgumentException("Order sub total cannot be negative", nameof(orderSubTotal));

            if (orderTotal < 0)
                throw new ArgumentException("Order total cannot be negative", nameof(orderTotal));

            if (previousDebt < 0)
                throw new ArgumentException("Previous debt cannot be negative", nameof(previousDebt));

            if (string.IsNullOrWhiteSpace(passportImage))
                throw new ArgumentException("Passport image is required", nameof(passportImage));

            if (string.IsNullOrWhiteSpace(hotelName))
                throw new ArgumentException("Hotel name is required", nameof(hotelName));

            if (string.IsNullOrWhiteSpace(hotelAddress))
                throw new ArgumentException("Hotel address is required", nameof(hotelAddress));

            if (string.IsNullOrWhiteSpace(orderCode))
                throw new ArgumentException("Order code is required", nameof(orderCode));

            if (!Enum.IsDefined(typeof(PaymentMethod), paymentMethodId))
                throw new ArgumentException("Invalid payment method", nameof(paymentMethodId));

            return new Order
            {
                OrderCode = orderCode,
                CustomerId = customerId,
                SubCategoryId = subCategoryId,
                CityId = cityId,
                ReservationDateFrom = reservationDateFrom,
                ReservationDateTo = reservationDateTo,
                VehiclesCount = vehiclesCount,
                OrderSubTotal = orderSubTotal,
                OrderTotal = orderTotal,
                PreviousDebt = previousDebt,
                MoneyRefunded = false,
                PassportImage = NormalizePassportImage(passportImage),
                HotelName = hotelName.Trim(),
                HotelAddress = hotelAddress.Trim(),
                HotelPhone = hotelPhone?.Trim(),
                IsUrgent = isUrgent,
                PaymentMethodId = paymentMethodId,
                OrderState = OrderState.Pending,
                Notes = notes,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }

        public static bool CanCancelInState(OrderState state) =>
            state == OrderState.Pending
            || state == OrderState.MerchantPending
            || state == OrderState.MerchantConfirmed;

        public static bool IsTerminalOrPastConfirm(OrderState state) =>
            state == OrderState.Confirmed
            || state == OrderState.DeliveryAssigned
            || state == OrderState.OnWay
            || state == OrderState.CustomerReceived
            || state == OrderState.CustomerRejectedReceipt
            || state == OrderState.Completed
            || state == OrderState.Cancelled;

        // Domain methods
        public void MarkMerchantPending(string? modifiedBy = null)
        {
            if (OrderState != OrderState.Pending
                && OrderState != OrderState.MerchantPending
                && OrderState != OrderState.MerchantConfirmed)
                throw new InvalidOperationException($"Cannot send to merchants in {OrderState} state.");

            OrderState = OrderState.MerchantPending;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void MarkMerchantConfirmed(string? modifiedBy = null)
        {
            if (OrderState != OrderState.MerchantPending)
                throw new InvalidOperationException($"Cannot mark merchant confirmed in {OrderState} state. Order must be in MerchantPending state.");

            OrderState = OrderState.MerchantConfirmed;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        /// <summary>MerchantConfirmed → Confirmed. Vehicles were already assigned at create.</summary>
        public void Confirm(string? modifiedBy = null)
        {
            if (OrderState != OrderState.MerchantConfirmed)
                throw new InvalidOperationException($"Cannot confirm order in {OrderState} state. Order must be in MerchantConfirmed state.");

            OrderState = OrderState.Confirmed;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        /// <summary>Confirmed → DeliveryAssigned after per-vehicle delivery assignment.</summary>
        public void MarkDeliveryAssigned(string? modifiedBy = null)
        {
            if (OrderState != OrderState.Confirmed && OrderState != OrderState.DeliveryAssigned)
                throw new InvalidOperationException($"Cannot mark delivery assigned in {OrderState} state. Order must be Confirmed.");

            OrderState = OrderState.DeliveryAssigned;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void MarkOnWay(string? modifiedBy = null)
        {
            if (OrderState != OrderState.DeliveryAssigned)
                throw new InvalidOperationException($"Cannot mark order as OnWay in {OrderState} state. Order must be in DeliveryAssigned state.");

            OrderState = OrderState.OnWay;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void MarkCustomerReceived(string? modifiedBy = null)
        {
            if (OrderState != OrderState.OnWay)
                throw new InvalidOperationException($"Cannot mark customer received in {OrderState} state. Order must be in OnWay state.");

            OrderState = OrderState.CustomerReceived;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void MarkCustomerRejectedReceipt(FaultParty faultParty, string? note = null, string? modifiedBy = null)
        {
            if (OrderState != OrderState.OnWay)
                throw new InvalidOperationException($"Cannot mark rejected receipt in {OrderState} state. Order must be in OnWay state.");

            if (faultParty == FaultParty.None)
                throw new ArgumentException("Fault party is required", nameof(faultParty));

            OrderState = OrderState.CustomerRejectedReceipt;
            ReceiptFaultParty = faultParty;
            ReceiptRejectNote = note?.Trim();
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void Complete(string? modifiedBy = null)
        {
            if (OrderState != OrderState.CustomerReceived)
                throw new InvalidOperationException($"Cannot complete order in {OrderState} state. Order must be in CustomerReceived state.");

            OrderState = OrderState.Completed;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void Cancel(string? modifiedBy = null)
        {
            if (OrderState == OrderState.Cancelled)
                throw new InvalidOperationException("Order is already cancelled.");

            if (!CanCancelInState(OrderState))
                throw new InvalidOperationException($"Cannot cancel order in {OrderState} state. Cancel is only allowed before Confirmed.");

            OrderState = OrderState.Cancelled;

            // Cash: money is considered refunded immediately. PayPal: admin marks refund later.
            MoneyRefunded = PaymentMethodId == (int)PaymentMethod.Cash;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void MarkMoneyRefunded(string? modifiedBy = null)
        {
            MoneyRefunded = true;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        /// <summary>Rental portion of the order total (excludes previous cancellation debt).</summary>
        public decimal GetRentalTotal() => OrderTotal - PreviousDebt;

        public void UpdateNotes(string? notes, string? modifiedBy = null)
        {
            Notes = notes;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates order details. Allowed only while the order is still Pending.
        /// </summary>
        public void Update(
            int customerId,
            int subCategoryId,
            int cityId,
            DateTime reservationDateFrom,
            DateTime reservationDateTo,
            int vehiclesCount,
            decimal orderSubTotal,
            decimal orderTotal,
            string passportImage,
            string hotelName,
            string hotelAddress,
            int paymentMethodId,
            bool isUrgent,
            string? hotelPhone = null,
            string? notes = null,
            string? modifiedBy = null)
        {
            if (OrderState != OrderState.Pending)
                throw new InvalidOperationException($"Cannot update order in {OrderState} state. Order must be in Pending state.");

            if (customerId <= 0)
                throw new ArgumentException("Customer ID must be greater than zero", nameof(customerId));

            if (subCategoryId <= 0)
                throw new ArgumentException("SubCategory ID must be greater than zero", nameof(subCategoryId));

            if (cityId <= 0)
                throw new ArgumentException("City ID must be greater than zero", nameof(cityId));

            if (reservationDateFrom.Date > reservationDateTo.Date)
                throw new ArgumentException("Reservation date from must be on or before reservation date to", nameof(reservationDateFrom));

            if (vehiclesCount <= 0)
                throw new ArgumentException("Vehicles count must be greater than zero", nameof(vehiclesCount));

            if (orderSubTotal < 0)
                throw new ArgumentException("Order sub total cannot be negative", nameof(orderSubTotal));

            if (orderTotal < 0)
                throw new ArgumentException("Order total cannot be negative", nameof(orderTotal));

            if (string.IsNullOrWhiteSpace(passportImage))
                throw new ArgumentException("Passport image is required", nameof(passportImage));

            if (string.IsNullOrWhiteSpace(hotelName))
                throw new ArgumentException("Hotel name is required", nameof(hotelName));

            if (string.IsNullOrWhiteSpace(hotelAddress))
                throw new ArgumentException("Hotel address is required", nameof(hotelAddress));

            if (!Enum.IsDefined(typeof(PaymentMethod), paymentMethodId))
                throw new ArgumentException("Invalid payment method", nameof(paymentMethodId));

            CustomerId = customerId;
            SubCategoryId = subCategoryId;
            CityId = cityId;
            ReservationDateFrom = reservationDateFrom;
            ReservationDateTo = reservationDateTo;
            VehiclesCount = vehiclesCount;
            OrderSubTotal = orderSubTotal;
            OrderTotal = orderTotal;
            PassportImage = NormalizePassportImage(passportImage);
            HotelName = hotelName.Trim();
            HotelAddress = hotelAddress.Trim();
            HotelPhone = hotelPhone?.Trim();
            IsUrgent = isUrgent;
            PaymentMethodId = paymentMethodId;
            Notes = notes;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Ensures passport images are stored/returned as data URIs (e.g. data:image/jpeg;base64,...).
        /// Mobile clients may send raw base64 without the prefix.
        /// </summary>
        public static string NormalizePassportImage(string passportImage)
        {
            if (string.IsNullOrWhiteSpace(passportImage))
                return passportImage;

            var trimmed = passportImage.Trim();

            if (trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                return trimmed;

            var mimeType = DetectImageMimeType(trimmed);
            return $"data:{mimeType};base64,{trimmed}";
        }

        private static string DetectImageMimeType(string base64)
        {
            if (base64.StartsWith("/9j/", StringComparison.Ordinal))
                return "image/jpeg";
            if (base64.StartsWith("iVBOR", StringComparison.Ordinal))
                return "image/png";
            if (base64.StartsWith("R0lGOD", StringComparison.Ordinal))
                return "image/gif";
            if (base64.StartsWith("UklGR", StringComparison.Ordinal))
                return "image/webp";

            return "image/jpeg";
        }
    }
}

