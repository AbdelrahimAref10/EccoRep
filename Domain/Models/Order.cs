using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Domain.Events;
using System.Globalization;

namespace Domain.Models
{
    public class Order : AggregateRoot, IAuditable
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

        /// <summary>Cash: order total debit already posted to the first delivery. PayPal: company debit at capture.</summary>
        public bool OrderTotalDebitedToCompany { get; private set; }

        /// <summary>Full order service fee already credited to company (once, on first customer delivery).</summary>
        public bool CompanyServiceFeeAccrued { get; private set; }

        /// <summary>Admin marked whole order as not delivered (journals posted as delivered + fault debit).</summary>
        public bool OrderDeliveryFailed { get; private set; }
        public string? OrderDeliveryFailureReason { get; private set; }
        public FaultParty? OrderDeliveryFailureFaultParty { get; private set; }

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
            IReadOnlyCollection<decimal> vehicleDailyPrices,
            City city,
            bool isUrgent,
            int reservationDays,
            decimal previousDebt = 0)
        {
            if (vehicleDailyPrices == null || vehicleDailyPrices.Count == 0)
                throw new ArgumentException("At least one vehicle price is required", nameof(vehicleDailyPrices));

            if (vehicleDailyPrices.Any(price => price < 0))
                throw new ArgumentException("Vehicle price cannot be negative", nameof(vehicleDailyPrices));

            if (city == null)
                throw new ArgumentNullException(nameof(city));

            if (reservationDays <= 0)
                throw new ArgumentException("Reservation days must be greater than zero", nameof(reservationDays));

            if (previousDebt < 0)
                throw new ArgumentException("Previous debt cannot be negative", nameof(previousDebt));

            var vehiclesCount = vehicleDailyPrices.Count;
            // Daily rental of the selected fleet, then multiplied by inclusive reservation days
            var dailyRentalTotal = vehicleDailyPrices.Sum();
            var subTotal = dailyRentalTotal * reservationDays;
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
                unitPrice: dailyRentalTotal,
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

        public static int InclusiveReservationDays(DateTime from, DateTime to)
            => Math.Max(1, (int)(to.Date - from.Date).TotalDays + 1);

        public decimal CalculateVehicleRental(decimal dailyPrice)
        {
            if (dailyPrice < 0)
                throw new ArgumentException("Vehicle price cannot be negative", nameof(dailyPrice));

            return dailyPrice * InclusiveReservationDays(ReservationDateFrom, ReservationDateTo);
        }

        /// <summary>
        /// Single place that writes VehiclesCount / OrderSubTotal / OrderTotal / PreviousDebt.
        /// Create, update, replacement, and remove all go through this.
        /// </summary>
        private void ApplyPricing(OrderPricingBreakdown pricing, string? modifiedBy = null)
        {
            if (pricing == null)
                throw new ArgumentNullException(nameof(pricing));

            if (pricing.VehiclesCount <= 0)
                throw new ArgumentException("Vehicles count must be greater than zero", nameof(pricing));

            VehiclesCount = pricing.VehiclesCount;
            OrderSubTotal = pricing.SubTotal;
            OrderTotal = pricing.Total;
            PreviousDebt = pricing.PreviousDebt;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Recalculates payable totals from the vehicles currently on the order and city fees.
        /// Call after replacement / remove / any fleet change before Confirmed.
        /// </summary>
        public OrderPricingBreakdown RecalculateTotals(City city, string? modifiedBy = null)
        {
            if (OrderState is not (OrderState.Pending or OrderState.MerchantPending or OrderState.MerchantConfirmed))
                throw new InvalidOperationException($"Cannot recalculate pricing in {OrderState} state.");

            var vehicles = OrderVehicles?.Select(ov => ov.Vehicle).ToList() ?? new List<Vehicle>();
            if (vehicles.Count == 0 || vehicles.Any(v => v == null))
                throw new InvalidOperationException("Assigned vehicles with prices are required to recalculate totals.");

            var days = InclusiveReservationDays(ReservationDateFrom, ReservationDateTo);
            var pricing = CalculatePricing(
                vehicles.Select(v => v.Price).ToList(),
                city,
                IsUrgent,
                days,
                PreviousDebt);

            ApplyPricing(pricing, modifiedBy);
            return pricing;
        }

        // Factory method for creating orders
        public static Order Create(
            int customerId,
            int subCategoryId,
            int cityId,
            DateTime reservationDateFrom,
            DateTime reservationDateTo,
            OrderPricingBreakdown pricing,
            string passportImage,
            string hotelName,
            string hotelAddress,
            int paymentMethodId,
            bool isUrgent,
            string orderCode,
            string? hotelPhone = null,
            string? notes = null,
            string? createdBy = null)
        {
            if (pricing == null)
                throw new ArgumentNullException(nameof(pricing));

            if (customerId <= 0)
                throw new ArgumentException("Customer ID must be greater than zero", nameof(customerId));

            if (subCategoryId <= 0)
                throw new ArgumentException("SubCategory ID must be greater than zero", nameof(subCategoryId));

            if (cityId <= 0)
                throw new ArgumentException("City ID must be greater than zero", nameof(cityId));

            if (reservationDateFrom.Date > reservationDateTo.Date)
                throw new ArgumentException("Reservation date from must be on or before reservation date to", nameof(reservationDateFrom));

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

            var order = new Order
            {
                OrderCode = orderCode,
                CustomerId = customerId,
                SubCategoryId = subCategoryId,
                CityId = cityId,
                ReservationDateFrom = reservationDateFrom,
                ReservationDateTo = reservationDateTo,
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

            order.ApplyPricing(pricing, createdBy);
            return order;
        }

        public static bool CanCancelInState(OrderState state) =>
            state == OrderState.Pending
            || state == OrderState.MerchantPending
            || state == OrderState.MerchantConfirmed
            || state == OrderState.Confirmed
            || state == OrderState.DeliveryAssigned;

        /// <summary>
        /// Cancel allowed until the first vehicle is received from owner (that moves order to OnWay).
        /// </summary>
        public bool CanCancel() =>
            OrderState != OrderState.Cancelled
            && CanCancelInState(OrderState)
            && !OrderVehicles.Any(v => v.ReceivedFromOwner);

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

            if (!CanCancel())
                throw new InvalidOperationException(
                    $"Cannot cancel order in {OrderState} state. Cancel stops once any vehicle is received from the merchant.");

            OrderState = OrderState.Cancelled;

            // Cash: money is considered refunded immediately. PayPal: admin marks refund later.
            MoneyRefunded = PaymentMethodId == (int)PaymentMethod.Cash;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        /// <summary>Marks company OrderTotal debit as posted (cash first delivery or PayPal capture).</summary>
        public void MarkOrderTotalDebitedToCompany(string? modifiedBy = null)
        {
            if (OrderTotalDebitedToCompany)
                return;

            OrderTotalDebitedToCompany = true;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void MarkCompanyServiceFeeAccrued(string? modifiedBy = null)
        {
            if (CompanyServiceFeeAccrued)
                return;

            CompanyServiceFeeAccrued = true;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// PayPal capture: company debit OrderTotal once. Returns ledger lines (no DB write).
        /// </summary>
        public IReadOnlyList<OrderLedgerLine> BuildOnlinePaymentCapturedLedgerLines(string? createdBy = null)
        {
            if (PaymentMethodId != (int)PaymentMethod.PayPal)
                throw new InvalidOperationException("Online payment ledger is only for PayPal orders.");

            if (OrderTotalDebitedToCompany || OrderTotal <= 0)
                return Array.Empty<OrderLedgerLine>();

            MarkOrderTotalDebitedToCompany(createdBy);

            var lines = new[]
            {
                new OrderLedgerLine(
                    OrderId,
                    vehicleId: null,
                    LedgerPartyType.Company,
                    partyId: null,
                    JournalDirection.Debit,
                    OrderTotal,
                    OrderJournalEntryKind.OrderTotalDebitedToCompany,
                    $"order:{OrderId}:order-total-debit:paypal",
                    note: "PayPal payment captured — order total debit to company")
            };

            RaiseDomainEvent(new OrderLedgerPostsRequested(OrderId, lines, createdBy));
            return lines;
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
        /// Money always comes from <see cref="ApplyPricing"/>.
        /// </summary>
        public void Update(
            int customerId,
            int subCategoryId,
            int cityId,
            DateTime reservationDateFrom,
            DateTime reservationDateTo,
            OrderPricingBreakdown pricing,
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
            PassportImage = NormalizePassportImage(passportImage);
            HotelName = hotelName.Trim();
            HotelAddress = hotelAddress.Trim();
            HotelPhone = hotelPhone?.Trim();
            IsUrgent = isUrgent;
            PaymentMethodId = paymentMethodId;
            Notes = notes;
            ApplyPricing(pricing, modifiedBy);
        }

        // ─── Per-vehicle lifecycle (domain owns sequencing + ledger instructions) ───

        public void MarkVehicleReceivedFromOwner(
            VehicleSettlementSnapshot snapshot,
            string? imageUrl,
            string? modifiedBy = null)
        {
            EnsureOperationalStateForPickup();
            var ov = RequireOrderVehicle(snapshot.VehicleId);

            var wasFirst = !OrderVehicles.Any(v => v.ReceivedFromOwner);
            ov.MarkReceivedFromOwner(imageUrl, modifiedBy);

            var lines = new List<OrderLedgerLine>();
            if (snapshot.MerchantCashOnReceive && snapshot.VehicleRental > 0)
            {
                lines.Add(new OrderLedgerLine(
                    OrderId,
                    snapshot.VehicleId,
                    LedgerPartyType.Merchant,
                    snapshot.MerchantId,
                    JournalDirection.Debit,
                    snapshot.VehicleRental,
                    OrderJournalEntryKind.MerchantPaidByDeliveryCashOnReceive,
                    $"order:{OrderId}:vehicle:{snapshot.VehicleId}:cash-on-receive",
                    note: "Cash on receive — merchant debit vehicle rental"));
            }

            if (wasFirst && OrderState == OrderState.DeliveryAssigned)
                MarkOnWay(modifiedBy);

            RaiseDomainEvent(new OrderVehicleReceivedFromOwnerEvent(
                OrderId,
                snapshot.VehicleId,
                becameOnWay: wasFirst,
                merchantCashOnReceive: snapshot.MerchantCashOnReceive,
                lines,
                modifiedBy));
            Touch(modifiedBy);
        }

        public void MarkVehicleDeliveredToCustomer(
            VehicleSettlementSnapshot snapshot,
            string? imageUrl,
            string? modifiedBy = null)
        {
            EnsureOperationalStateForCustomerDelivery();
            var ov = RequireOrderVehicle(snapshot.VehicleId);
            ov.MarkDeliveredToCustomer(imageUrl, modifiedBy);

            var lines = BuildCustomerDeliveryAccrualLines(snapshot, modifiedBy);

            if (OrderVehicles.All(v => v.DeliveredToCustomer) && OrderState == OrderState.OnWay)
                MarkCustomerReceived(modifiedBy);

            RaiseDomainEvent(new OrderVehicleDeliveredToCustomerEvent(
                OrderId,
                snapshot.VehicleId,
                becameCustomerReceived: OrderState == OrderState.CustomerReceived,
                lines,
                modifiedBy));
            Touch(modifiedBy);
        }

        public void MarkVehicleReceivedFromCustomer(int vehicleId, string? imageUrl, string? modifiedBy = null)
        {
            if (OrderState is not (OrderState.CustomerReceived or OrderState.OnWay or OrderState.Completed))
            {
                // Allow receive-from-customer once vehicle was delivered; order may still be OnWay if not all delivered.
                var ovEarly = RequireOrderVehicle(vehicleId);
                if (!ovEarly.DeliveredToCustomer)
                    throw new InvalidOperationException($"Cannot receive from customer in order state {OrderState}.");
            }

            var ov = RequireOrderVehicle(vehicleId);
            ov.MarkReceivedFromCustomer(imageUrl, modifiedBy);
            Touch(modifiedBy);
        }

        public void MarkVehicleDeliveredToOwner(int vehicleId, string? imageUrl, string? modifiedBy = null)
        {
            var ov = RequireOrderVehicle(vehicleId);
            ov.MarkDeliveredToOwner(imageUrl, modifiedBy);

            if (OrderVehicles.All(v => v.DeliveredToOwner) && OrderState == OrderState.CustomerReceived)
                Complete(modifiedBy);

            Touch(modifiedBy);
        }

        /// <summary>
        /// Vehicle not received by customer: post delivery accruals as if delivered, then debit fault party
        /// for vehicle rental + delivery fee.
        /// </summary>
        public void MarkVehicleNotReceivedByCustomer(
            VehicleSettlementSnapshot snapshot,
            string reason,
            FaultParty faultParty,
            string? modifiedBy = null)
        {
            EnsureOperationalStateForCustomerDelivery();
            var ov = RequireOrderVehicle(snapshot.VehicleId);
            ov.MarkDeliveryFailed(reason, faultParty, modifiedBy);

            var lines = BuildCustomerDeliveryAccrualLines(snapshot, modifiedBy);
            var faultAmount = snapshot.VehicleRental + snapshot.DeliveryFeeShare;
            if (faultAmount > 0)
            {
                lines.Add(BuildNonDeliveryFaultLine(
                    snapshot.VehicleId,
                    faultAmount,
                    faultParty,
                    snapshot.MerchantId,
                    snapshot.DeliveryId,
                    $"order:{OrderId}:vehicle:{snapshot.VehicleId}:non-delivery-fault",
                    reason));
            }

            if (OrderVehicles.All(v => v.DeliveredToCustomer) && OrderState == OrderState.OnWay)
                MarkCustomerReceived(modifiedBy);

            if (lines.Count > 0)
                RaiseDomainEvent(new OrderLedgerPostsRequested(OrderId, lines, modifiedBy));

            Touch(modifiedBy);
        }

        /// <summary>
        /// Whole order not delivered: post accruals for every vehicle, cash/paypal order-total rules,
        /// then one fault debit chosen by admin.
        /// </summary>
        public void MarkOrderNotDelivered(
            IReadOnlyList<VehicleSettlementSnapshot> snapshots,
            string reason,
            FaultParty faultParty,
            string? modifiedBy = null)
        {
            if (snapshots == null || snapshots.Count == 0)
                throw new ArgumentException("At least one vehicle settlement is required", nameof(snapshots));

            if (OrderState is not (OrderState.DeliveryAssigned or OrderState.OnWay))
                throw new InvalidOperationException(
                    $"Cannot mark order not delivered in {OrderState}. Order must be DeliveryAssigned or OnWay.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Failure reason is required", nameof(reason));

            if (faultParty is FaultParty.None or FaultParty.Customer)
                throw new ArgumentException("Fault party must be Merchant, Delivery, or Company", nameof(faultParty));

            var lines = new List<OrderLedgerLine>();
            foreach (var snapshot in snapshots)
            {
                var ov = RequireOrderVehicle(snapshot.VehicleId);
                if (!ov.ReceivedFromOwner)
                    throw new InvalidOperationException($"Vehicle {snapshot.VehicleId} must be received from owner first.");

                ov.MarkDeliveryFailed(reason, faultParty, modifiedBy);
                lines.AddRange(BuildCustomerDeliveryAccrualLines(snapshot, modifiedBy));
            }

            var faultTotal = snapshots.Sum(s => s.VehicleRental + s.DeliveryFeeShare);
            if (faultTotal > 0)
            {
                int? partyId = faultParty switch
                {
                    FaultParty.Delivery => snapshots[0].DeliveryId,
                    FaultParty.Merchant => snapshots[0].MerchantId,
                    _ => null
                };

                lines.Add(BuildNonDeliveryFaultLine(
                    vehicleId: null,
                    faultTotal,
                    faultParty,
                    snapshots[0].MerchantId,
                    snapshots[0].DeliveryId,
                    $"order:{OrderId}:order-non-delivery-fault",
                    reason,
                    partyIdOverride: partyId));
            }

            OrderDeliveryFailed = true;
            OrderDeliveryFailureReason = reason.Trim();
            OrderDeliveryFailureFaultParty = faultParty;

            if (OrderState == OrderState.DeliveryAssigned)
                MarkOnWay(modifiedBy);

            if (OrderState == OrderState.OnWay)
                MarkCustomerReceived(modifiedBy);

            if (lines.Count > 0)
                RaiseDomainEvent(new OrderLedgerPostsRequested(OrderId, lines, modifiedBy));

            Touch(modifiedBy);
        }

        private List<OrderLedgerLine> BuildCustomerDeliveryAccrualLines(
            VehicleSettlementSnapshot snapshot,
            string? modifiedBy)
        {
            var lines = new List<OrderLedgerLine>();
            var vid = snapshot.VehicleId;

            if (snapshot.VehicleRental > 0)
            {
                lines.Add(new OrderLedgerLine(
                    OrderId,
                    vid,
                    LedgerPartyType.Merchant,
                    snapshot.MerchantId,
                    JournalDirection.Credit,
                    snapshot.VehicleRental,
                    OrderJournalEntryKind.MerchantRentalAccrued,
                    $"order:{OrderId}:vehicle:{vid}:merchant-rental",
                    note: "Merchant vehicle rental credit on customer delivery"));
            }

            // Service fee is Ecco profit only — never split or deducted from merchant rent.
            // Post the full order amount once on the first customer delivery (cash or online).
            if (!CompanyServiceFeeAccrued && snapshot.OrderServiceFees > 0)
            {
                MarkCompanyServiceFeeAccrued(modifiedBy);
                lines.Add(new OrderLedgerLine(
                    OrderId,
                    vehicleId: null,
                    LedgerPartyType.Company,
                    null,
                    JournalDirection.Credit,
                    snapshot.OrderServiceFees,
                    OrderJournalEntryKind.CompanyServiceFeeAccrued,
                    $"order:{OrderId}:company-service-fee",
                    note: "Company service fee credit on first customer delivery"));
            }

            if (snapshot.DeliveryFeeShare > 0)
            {
                lines.Add(new OrderLedgerLine(
                    OrderId,
                    vid,
                    LedgerPartyType.Delivery,
                    snapshot.DeliveryId,
                    JournalDirection.Credit,
                    snapshot.DeliveryFeeShare,
                    OrderJournalEntryKind.DeliveryFeeAccrued,
                    $"order:{OrderId}:vehicle:{vid}:delivery-fee",
                    note: "Delivery fee credit on customer delivery"));
            }

            // Cash: the courier collected the full order amount from the customer.
            // Debit that delivery once on first customer handoff. Online orders debit
            // the company at payment capture — never here.
            if (PaymentMethodId == (int)PaymentMethod.Cash
                && !OrderTotalDebitedToCompany
                && OrderTotal > 0)
            {
                if (snapshot.DeliveryId <= 0)
                    throw new InvalidOperationException("Delivery ID is required to debit cash collected from the customer.");

                MarkOrderTotalDebitedToCompany(modifiedBy);
                lines.Add(new OrderLedgerLine(
                    OrderId,
                    vehicleId: null,
                    LedgerPartyType.Delivery,
                    snapshot.DeliveryId,
                    JournalDirection.Debit,
                    OrderTotal,
                    OrderJournalEntryKind.CashCollectedFromCustomer,
                    $"order:{OrderId}:order-total-debit:cash",
                    note: "Cash — order total debit to delivery on first customer delivery"));
            }

            return lines;
        }

        private OrderLedgerLine BuildNonDeliveryFaultLine(
            int? vehicleId,
            decimal amount,
            FaultParty faultParty,
            int merchantId,
            int deliveryId,
            string idempotencyKey,
            string reason,
            int? partyIdOverride = null)
        {
            var (partyType, partyId) = faultParty switch
            {
                FaultParty.Merchant => (LedgerPartyType.Merchant, partyIdOverride ?? merchantId),
                FaultParty.Delivery => (LedgerPartyType.Delivery, partyIdOverride ?? deliveryId),
                FaultParty.Company => (LedgerPartyType.Company, (int?)null),
                _ => throw new ArgumentException("Unsupported fault party", nameof(faultParty))
            };

            return new OrderLedgerLine(
                OrderId,
                vehicleId,
                partyType,
                partyId,
                JournalDirection.Debit,
                amount,
                OrderJournalEntryKind.NonDeliveryFaultDebit,
                idempotencyKey,
                note: reason,
                faultParty: faultParty);
        }

        private OrderVehicle RequireOrderVehicle(int vehicleId)
        {
            var ov = OrderVehicles.FirstOrDefault(v => v.VehicleId == vehicleId);
            if (ov == null)
                throw new InvalidOperationException($"Vehicle {vehicleId} is not on this order.");
            return ov;
        }

        private void EnsureOperationalStateForPickup()
        {
            if (OrderState is not (OrderState.DeliveryAssigned or OrderState.OnWay))
                throw new InvalidOperationException(
                    $"Cannot receive from owner in {OrderState}. Order must be DeliveryAssigned (or already OnWay).");
        }

        private void EnsureOperationalStateForCustomerDelivery()
        {
            if (OrderState is not (OrderState.OnWay or OrderState.DeliveryAssigned or OrderState.CustomerReceived))
                throw new InvalidOperationException(
                    $"Cannot deliver to customer in {OrderState}. Order must be OnWay after pickup.");

            // Strict sequence: order-level customer delivery accruals require OnWay (first pickup done),
            // except order-not-delivered may force OnWay first.
            if (OrderState == OrderState.DeliveryAssigned)
                throw new InvalidOperationException(
                    "Cannot deliver to customer before at least one vehicle is received from owner (OnWay).");
        }

        private void Touch(string? modifiedBy)
        {
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

