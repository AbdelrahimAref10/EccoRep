## Overview

This document outlines the complete implementation plan for the Order Cycle System, including customer-facing APIs, admin dashboard functionality, payment processing, cancellation handling, refunds, and treasury management following Clean Architecture, CQRS, and DDD principles.

## Key Requirements

1. **Removed Reject State**: Only Cancel state exists. Both customer and admin can cancel orders using the same CancelOrderCommand
2. **OrderCode**: Added to Order entity - randomly generated unique code when order is created
3. **OrderSubTotal**: Price * VehiclesCount (simple calculation)
4. **OrderTotal**: Full calculation with ServiceFees as percentage
5. **Cancel Endpoints**: Two endpoints (customer and admin) calling the same CancelOrderCommand
6. **Admin Search**: GetAllOrdersQuery supports filtering by OrderState and OrderCode
7. **Frontend**: Admin Order Cycle UI implementation after backend completion
8. **Reports**: All suggested reports implemented in admin

## Order States

- **Pending**: Initial state when customer creates order
- **Confirmed**: Admin assigns vehicles and confirms order
- **OnWay**: Admin marks vehicle as dispatched
- **CustomerReceived**: Admin marks that customer received vehicle
- **Completed**: Admin confirms vehicle returned successfully

**Note**: No Reject state. Cancellation is handled separately.

## Database Schema Changes

### City Entity Updates (`Domain/Models/City.cs`)

- Add `DeliveryFees` (decimal, nullable) - Fixed fee per vehicle
- Add `UrgentDelivery` (decimal, nullable) - Fixed fee if urgent delivery selected  
- Add `ServiceFees` (decimal, nullable) - **Percentage value** (e.g., 5.0 means 5%, stored as decimal)
- Add `CancellationFees` (decimal, nullable) - Fixed fee for cancellation
- Update domain methods to handle new properties

### Vehicle Entity Updates (`Domain/Models/Vehicle.cs`)

- Add `VehicleCode` (string, required) - Unique code for each vehicle
- Update factory method and domain methods

### New Domain Entities

#### Order Entity (`Domain/Models/Order.cs`)

**Properties:**
- `OrderId` (int, PK)
- `OrderCode` (string, unique, required) - Generated randomly on creation
- `CustomerId` (int, FK to Customer)
- `SubCategoryId` (int, FK to SubCategory)
- `CityId` (int, FK to City)
- `ReservationDateFrom` (DateTime)
- `ReservationDateTo` (DateTime)
- `VehiclesCount` (int)
- `OrderSubTotal` (decimal) = SubCategory.Price * VehiclesCount
- `OrderTotal` (decimal) = Full calculation with all fees
- `Notes` (string, nullable)
- `PassportImage` (string, base64, required)
- `HotelName` (string, required)
- `HotelAddress` (string, required)
- `HotelPhone` (string, nullable)
- `IsUrgent` (bool)
- `PaymentMethodId` (int, PaymentMethod enum)
- `OrderState` (OrderState enum)

**Calculation Formula:**
- SubTotal = SubCategoryPrice * VehiclesCount
- ServiceFeesAmount = ServiceFees * SubTotal / 100 (percentage calculation)
- OrderTotal = SubTotal + (DeliveryFees * VehiclesCount) + ServiceFeesAmount + UrgentFees (if urgent)

**Domain Methods:**
- `Create()` - Factory method with OrderCode generation
- `Confirm()` - Admin confirms order
- `MarkOnWay()` - Admin marks as dispatched
- `MarkCustomerReceived()` - Admin marks customer received
- `Complete()` - Admin completes order
- `Cancel()` - Cancel order (used by both customer and admin)

#### OrderVehicle Entity (`Domain/Models/OrderVehicle.cs`)

**Properties:**
- `OrderId` (int, FK to Order)
- `VehicleId` (int, FK to Vehicle)
- Navigation: `Order`, `Vehicle`
- Composite PK: (OrderId, VehicleId)

#### ReservedVehiclesPerDays Entity (`Domain/Models/ReservedVehiclesPerDays.cs`)

**Properties:**
- `Id` (int, PK)
- `VehicleId` (int, FK to Vehicle)
- `SubCategoryId` (int, FK to SubCategory)
- `VehicleCode` (string)
- `OrderId` (int, FK to Order)
- `DateFrom` (DateTime)
- `DateTo` (DateTime)
- `State` (ReservedVehicleState enum: StillBooked, Cancelled)
- Navigation: `Vehicle`, `SubCategory`, `Order`

#### OrderPayment Entity (`Domain/Models/OrderPayment.cs`)

**Properties:**
- `Id` (int, PK)
- `OrderId` (int, FK to Order)
- `PaymentMethodId` (int, PaymentMethod enum)
- `Total` (decimal)
- `State` (PaymentState enum: Pending, Paid, Failed, Refunded)
- Navigation: `Order`

#### OrderCancellationFee Entity (`Domain/Models/OrderCancellationFee.cs`)

**Properties:**
- `Id` (int, PK)
- `CustomerId` (int, FK to Customer)
- `OrderId` (int, FK to Order)
- `Amount` (decimal)
- `State` (CancellationFeeState enum: NotYet, Paid)
- Navigation: `Customer`, `Order`

#### RefundablePaypalAmount Entity (`Domain/Models/RefundablePaypalAmount.cs`)

**Properties:**
- `Id` (int, PK)
- `CustomerId` (int, FK to Customer)
- `OrderId` (int, FK to Order)
- `OrderTotal` (decimal)
- `CancellationFees` (decimal)
- `RefundableAmount` (decimal)
- `State` (RefundState enum: Pending, Success, Failed)
- Navigation: `Customer`, `Order`

#### CompanyTreasury Entity (`Domain/Models/CompanyTreasury.cs`)

**Properties:**
- `Id` (int, PK)
- `TotalRevenue` (decimal) - Sum of all completed order payments
- `TotalCancellationFees` (decimal) - Sum of all paid cancellation fees
- `LastUpdated` (DateTime)

**Domain Methods:**
- `AddRevenue()` - Add completed order payment to treasury
- `AddCancellationFee()` - Add paid cancellation fee to treasury
- `GetBalance()` - Get current treasury balance

### New Enums (`Domain/Enums/`)

- `OrderState.cs`: Pending, Confirmed, OnWay, CustomerReceived, Completed
- `PaymentMethod.cs`: Cash, PayPal
- `PaymentState.cs`: Pending, Paid, Failed, Refunded
- `RefundState.cs`: Pending, Success, Failed
- `ReservedVehicleState.cs`: StillBooked, Cancelled
- `CancellationFeeState.cs`: NotYet, Paid

## API Endpoints

### CustomerOrderController

1. **GET /api/CustomerOrder/CityFees**
   - Get customer's city fees data (CityId, ServiceFees, DeliveryFees, UrgentFees, CancellationFees)
   - Returns fees for logged-in customer's city
   - Used by mobile app after customer login
   - Query: `GetCityFeesQuery`

2. **GET /api/CustomerOrder/ReservedVehiclePerSubCategory?subCategoryId={id}**
   - Get reserved dates for a subcategory
   - Returns list of dates that are booked (from ReservedVehiclesPerDays where State = StillBooked and Order.OrderState != Completed && != Cancelled)
   - Query: `GetReservedVehiclePerSubCategoryQuery`

3. **POST /api/CustomerOrder**
   - Create new order (generates OrderCode)
   - Validates total calculation (tolerance: 0.50)
   - Validates date range doesn't conflict
   - Validates payment method (check CashBlock for Cash)
   - Command: `CreateOrderCommand`

4. **GET /api/CustomerOrder/MyOrders**
   - Get all orders for logged-in customer
   - Returns paginated list of OrderDto
   - Query: `GetCustomerOrdersQuery`

5. **POST /api/CustomerOrder/{orderId}/Cancel**
   - Customer cancels order
   - Validates 4 days policy
   - Handles cancellation fees
   - Processes refunds if PayPal
   - Command: `CancelOrderCommand` (shared with admin)

### AdminOrderController

1. **GET /api/AdminOrder?state={state}&orderCode={code}&page={page}&pageSize={size}**
   - Get all orders with filtering by state and orderCode
   - Returns paginated list of OrderDto
   - Query: `GetAllOrdersQuery`

2. **GET /api/AdminOrder/{id}**
   - Get order details with all related data
   - Returns OrderDetailDto
   - Query: `GetOrderByIdQuery`

3. **POST /api/AdminOrder/{orderId}/Confirm**
   - Admin assigns vehicles and confirms order
   - Input: List of VehicleIds
   - Validates vehicles are Available
   - Validates vehicles belong to order's SubCategory
   - Validates vehicles not reserved in date range
   - Creates OrderVehicle and ReservedVehiclesPerDays records
   - Updates Vehicle.Status to "Rented"
   - Command: `ConfirmOrderCommand`

4. **POST /api/AdminOrder/{orderId}/UpdateState**
   - Admin updates order state (OnWay, CustomerReceived, Completed)
   - State Transitions:
     - Confirmed → OnWay
     - OnWay → CustomerReceived
     - CustomerReceived → Completed
   - On Completed: Updates Vehicle.Status to "Available", updates CompanyTreasury
   - Command: `UpdateOrderStateCommand`

5. **POST /api/AdminOrder/{orderId}/Cancel**
   - Admin cancels order (same command as customer)
   - Command: `CancelOrderCommand` (shared)

6. **POST /api/AdminOrder/{orderId}/Payment/UpdateState**
   - Admin updates payment state (for Cash payments)
   - Updates OrderPayment.State and OrderCancellationFee.State if applicable
   - Command: `UpdatePaymentStateCommand`

7. **POST /api/AdminOrder/{orderId}/Refund**
   - Admin manually processes PayPal refund
   - Updates RefundablePaypalAmount.State
   - Updates OrderPayment.State to Refunded
   - Command: `ProcessRefundCommand`

### Reports Endpoints (AdminOrderController)

1. **GET /api/AdminOrder/Reports/OrdersByState**
   - Count orders by state
   - Query: `OrdersByStateReportQuery`

2. **GET /api/AdminOrder/Reports/OrdersByDateRange?from={date}&to={date}**
   - Orders within date range
   - Query: `OrdersByDateRangeReportQuery`

3. **GET /api/AdminOrder/Reports/Revenue?period={month|quarter|year}**
   - Total revenue by period
   - Query: `RevenueReportQuery`

4. **GET /api/AdminOrder/Reports/Cancellations**
   - Cancelled orders with fees
   - Query: `CancellationReportQuery`

5. **GET /api/AdminOrder/Reports/VehicleUtilization**
   - Vehicle usage statistics from ReservedVehiclesPerDays
   - Query: `VehicleUtilizationReportQuery`

6. **GET /api/AdminOrder/Reports/CustomerOrderHistory?customerId={id}**
   - Orders per customer
   - Query: `CustomerOrderHistoryReportQuery`

7. **GET /api/AdminOrder/Reports/TreasuryBalance**
   - Current treasury balance and breakdown
   - Query: `TreasuryBalanceReportQuery`

8. **GET /api/AdminOrder/Reports/RevenueByPeriod?period={month|quarter|year}**
   - Revenue breakdown by period
   - Query: `RevenueByPeriodReportQuery`

9. **GET /api/AdminOrder/Reports/CancellationFees**
   - Cancellation fees collected
   - Query: `CancellationFeesReportQuery`

## Domain Services

### OrderCalculationService (`Domain/Services/OrderCalculationService.cs`)

**Methods:**
- `CalculateOrderSubTotal()`: SubCategoryPrice * VehiclesCount
- `CalculateOrderTotal()`: Full calculation with all fees
  - SubTotal = SubCategoryPrice * VehiclesCount
  - ServiceFeesAmount = ServiceFees * SubTotal / 100
  - OrderTotal = SubTotal + (DeliveryFees * VehiclesCount) + ServiceFeesAmount + UrgentFees (if urgent)
- `ValidateTotalMatch()`: Compare backend total with mobile total (tolerance: 0.50)
- `CalculateCancellationFee()`: Calculate fee based on city and order age (4 days policy)

### TreasuryService (`Domain/Services/TreasuryService.cs`)

**Methods:**
- `AddOrderRevenue()`: Add completed order payment to treasury
- `AddCancellationFee()`: Add paid cancellation fee to treasury
- `GetTreasuryBalance()`: Get current treasury balance

## Shared CancelOrderCommand

- Single command used by both customer and admin endpoints
- Validates cancellation rules (4 days policy)
- If order creation date exceeds 4 days: Create OrderCancellationFee
- If PayPal payment: Create RefundablePaypalAmount record, process refund
- Updates ReservedVehiclesPerDays state to Cancelled
- Updates Vehicle.Status back to "Available" if vehicles were assigned

## Infrastructure Layer

### Mapping Configurations (`Infrastructure/MappingConfiguration/`)

All new entities require EF Core mapping configurations:

- `OrderConfiguration.cs` - Configure Order table, relationships, indexes
- `OrderVehicleConfiguration.cs` - Configure many-to-many relationship
- `ReservedVehiclesPerDaysConfiguration.cs` - Configure reservation tracking
- `OrderPaymentConfiguration.cs` - Configure payment records
- `OrderCancellationFeeConfiguration.cs` - Configure cancellation fees
- `RefundablePaypalAmountConfiguration.cs` - Configure refund tracking
- `CompanyTreasuryConfiguration.cs` - Configure treasury table
- Update `CityConfiguration.cs` - Add new fee columns
- Update `VehicleConfiguration.cs` - Add VehicleCode column

### DatabaseContext Updates (`Infrastructure/DatabaseContext.cs`)

Add DbSets for all new entities:
- `DbSet<Order> Orders`
- `DbSet<OrderVehicle> OrderVehicles`
- `DbSet<ReservedVehiclesPerDays> ReservedVehiclesPerDays`
- `DbSet<OrderPayment> OrderPayments`
- `DbSet<OrderCancellationFee> OrderCancellationFees`
- `DbSet<RefundablePaypalAmount> RefundablePaypalAmounts`
- `DbSet<CompanyTreasury> CompanyTreasuries`

## Application Layer - DTOs

### Customer DTOs (`Application/Features/Order/DTOs/`)

- `CityFeesDto.cs` - CityId, ServiceFees, DeliveryFees, UrgentFees, CancellationFees
- `ReservedDateDto.cs` - Date range for calendar display
- `CreateOrderCommandDto.cs` - Input for creating order
- `OrderDto.cs` - Basic order information for list views
- `OrderDetailDto.cs` - Full order details with all related entities

### Admin DTOs

- `OrderDto.cs` - Order list item with search fields
- `OrderDetailDto.cs` - Complete order information
- `OrderVehicleDto.cs` - Vehicle assignment information
- `OrderPaymentDto.cs` - Payment details
- `ReportDto.cs` - Base report DTO
- `OrdersByStateReportDto.cs` - Orders count by state
- `RevenueReportDto.cs` - Revenue statistics
- `TreasuryReportDto.cs` - Treasury balance and breakdown
- `VehicleUtilizationReportDto.cs` - Vehicle usage statistics

## Business Rules

### Order Creation Rules

1. **Total Validation**: Backend calculates total and compares with mobile total (tolerance: 0.50)
   - If difference > 0.50: Return error "There is a mistake in calculation"
   - If difference ≤ 0.50: Accept mobile total

2. **Date Conflict Validation**: Check ReservedVehiclesPerDays for date conflicts
   - Query: SubCategoryId matches AND date ranges overlap AND State = StillBooked AND Order.OrderState != Completed

3. **Payment Method Validation**: 
   - If Cash: Check Customer.CashBlock - if true, reject order
   - If PayPal: Process payment (placeholder for integration)

4. **OrderCode Generation**: Generate unique random code (e.g., "ORD-20260111-ABC123")

### Cancellation Rules

1. **4 Days Policy**: 
   - If order created within 4 days: Free cancellation
   - If order created > 4 days ago: Apply CancellationFees from City

2. **Cancellation Fee Calculation**:
   - Amount = City.CancellationFees
   - Create OrderCancellationFee with State = NotYet

3. **Refund Processing**:
   - If PaymentMethod = PayPal: Create RefundablePaypalAmount
   - RefundableAmount = OrderTotal - CancellationFees (if applicable)
   - Process refund via PayPal API (placeholder)

4. **Vehicle Status Update**:
   - If vehicles assigned: Update Vehicle.Status to "Available"
   - Update ReservedVehiclesPerDays.State to Cancelled

### Order State Transition Rules

1. **Pending → Confirmed**: 
   - Admin must assign vehicles
   - Vehicles must be Available
   - Vehicles must belong to SubCategory
   - No date conflicts

2. **Confirmed → OnWay**: 
   - Admin marks vehicle as dispatched
   - No validation required

3. **OnWay → CustomerReceived**: 
   - Admin marks customer received vehicle
   - No validation required

4. **CustomerReceived → Completed**: 
   - Admin confirms vehicle returned
   - Update Vehicle.Status to "Available"
   - Update CompanyTreasury with order payment
   - Keep ReservedVehiclesPerDays.State as StillBooked (for reporting)

### Payment Rules

1. **Cash Payments**:
   - State starts as Pending
   - Admin can update to Paid
   - If cancellation fee exists, admin updates OrderCancellationFee.State to Paid

2. **PayPal Payments**:
   - State starts as Pending
   - On order confirmation: Update to Paid
   - On cancellation: Create refund record, update to Refunded

### Treasury Rules

1. **Revenue Addition**: Only when order state = Completed
2. **Cancellation Fee Addition**: Only when OrderCancellationFee.State = Paid
3. **Balance Calculation**: TotalRevenue + TotalCancellationFees

## Validation Rules

### CreateOrderCommand Validation

- ReservationDateFrom: Required, must be future date
- ReservationDateTo: Required, must be >= ReservationDateFrom
- SubCategoryId: Required, must exist and be active
- VehiclesCount: Required, must be > 0
- CityId: Required, must exist
- PaymentMethodId: Required, must be valid enum value
- PassportImage: Required, must be valid base64 string
- HotelName: Required, max length 256
- HotelAddress: Required, max length 500
- HotelPhone: Optional, max length 20
- MobileTotal: Required, must match backend calculation (tolerance: 0.50)

### CancelOrderCommand Validation

- OrderId: Required, must exist
- Order must be in valid state for cancellation (Pending or Confirmed)
- If vehicles assigned, must be able to release them

### ConfirmOrderCommand Validation

- OrderId: Required, must exist
- Order must be in Pending state
- VehicleIds: Required, must not be empty
- All vehicles must exist and be Available
- All vehicles must belong to order's SubCategory
- All vehicles must not be reserved in date range

## Error Handling

### Order Creation Errors

- "There is a mistake in calculation" - Total mismatch
- "Selected dates are not available" - Date conflict
- "Payment method not allowed" - CashBlock check failed
- "SubCategory not found or inactive" - Invalid subcategory

### Cancellation Errors

- "Order cannot be cancelled" - Invalid state
- "Vehicles already assigned" - Cannot cancel after confirmation (unless admin)

### Vehicle Assignment Errors

- "Vehicle not available" - Vehicle status not Available
- "Vehicle does not belong to subcategory" - Category mismatch
- "Vehicle already reserved" - Date conflict

## File Structure

```
Domain/
  Models/
    Order.cs
    OrderVehicle.cs
    ReservedVehiclesPerDays.cs
    OrderPayment.cs
    OrderCancellationFee.cs
    RefundablePaypalAmount.cs
    CompanyTreasury.cs
  Enums/
    OrderState.cs
    PaymentMethod.cs
    PaymentState.cs
    RefundState.cs
    ReservedVehicleState.cs
    CancellationFeeState.cs
  Services/
    OrderCalculationService.cs
    TreasuryService.cs

Application/
  Features/
    Order/
      Command/
        CreateOrderCommand/
          CreateOrderCommand.cs
          CreateOrderCommandHandler.cs
          CreateOrderCommandValidator.cs
        CancelOrderCommand/
          CancelOrderCommand.cs
          CancelOrderCommandHandler.cs
          CancelOrderCommandValidator.cs
        ConfirmOrderCommand/
          ConfirmOrderCommand.cs
          ConfirmOrderCommandHandler.cs
          ConfirmOrderCommandValidator.cs
        UpdateOrderStateCommand/
          UpdateOrderStateCommand.cs
          UpdateOrderStateCommandHandler.cs
          UpdateOrderStateCommandValidator.cs
        UpdatePaymentStateCommand/
          UpdatePaymentStateCommand.cs
          UpdatePaymentStateCommandHandler.cs
          UpdatePaymentStateCommandValidator.cs
        ProcessRefundCommand/
          ProcessRefundCommand.cs
          ProcessRefundCommandHandler.cs
          ProcessRefundCommandValidator.cs
      Query/
        GetCityFeesQuery/
          GetCityFeesQuery.cs
          GetCityFeesQueryHandler.cs
        GetReservedVehiclePerSubCategoryQuery/
          GetReservedVehiclePerSubCategoryQuery.cs
          GetReservedVehiclePerSubCategoryQueryHandler.cs
        GetCustomerOrdersQuery/
          GetCustomerOrdersQuery.cs
          GetCustomerOrdersQueryHandler.cs
        GetAllOrdersQuery/
          GetAllOrdersQuery.cs
          GetAllOrdersQueryHandler.cs
        GetOrderByIdQuery/
          GetOrderByIdQuery.cs
          GetOrderByIdQueryHandler.cs
        Reports/
          OrdersByStateReportQuery/
          OrdersByDateRangeReportQuery/
          RevenueReportQuery/
          CancellationReportQuery/
          VehicleUtilizationReportQuery/
          CustomerOrderHistoryReportQuery/
          TreasuryBalanceReportQuery/
          RevenueByPeriodReportQuery/
          CancellationFeesReportQuery/
      DTOs/
        CityFeesDto.cs
        ReservedDateDto.cs
        CreateOrderCommandDto.cs
        OrderDto.cs
        OrderDetailDto.cs
        OrderVehicleDto.cs
        OrderPaymentDto.cs
        ReportDto.cs
        OrdersByStateReportDto.cs
        RevenueReportDto.cs
        TreasuryReportDto.cs
        VehicleUtilizationReportDto.cs

Infrastructure/
  MappingConfiguration/
    OrderConfiguration.cs
    OrderVehicleConfiguration.cs
    ReservedVehiclesPerDaysConfiguration.cs
    OrderPaymentConfiguration.cs
    OrderCancellationFeeConfiguration.cs
    RefundablePaypalAmountConfiguration.cs
    CompanyTreasuryConfiguration.cs

Volt.Server/
  Controllers/
    CustomerOrderController.cs
    AdminOrderController.cs
```

## Migration Strategy

1. **Create Migration**: 
   - Update City table (add DeliveryFees, UrgentDelivery, ServiceFees, CancellationFees)
   - Update Vehicle table (add VehicleCode)
   - Create Order table
   - Create OrderVehicle table
   - Create ReservedVehiclesPerDays table
   - Create OrderPayment table
   - Create OrderCancellationFee table
   - Create RefundablePaypalAmount table
   - Create CompanyTreasury table

2. **Seed Data**:
   - Create initial CompanyTreasury record with TotalRevenue = 0, TotalCancellationFees = 0

3. **Indexes**:
   - Order.OrderCode (unique)
   - Order.CustomerId
   - Order.OrderState
   - ReservedVehiclesPerDays.SubCategoryId, DateFrom, DateTo
   - ReservedVehiclesPerDays.State

## Implementation Steps

## Implementation Steps

### Phase 1: Domain Layer
1. Create all enums (OrderState, PaymentMethod, PaymentState, RefundState, ReservedVehicleState, CancellationFeeState)
2. Update City entity (add DeliveryFees, UrgentDelivery, ServiceFees, CancellationFees)
3. Update Vehicle entity (add VehicleCode)
4. Create all new domain entities with factory methods and domain logic:
   - Order
   - OrderVehicle
   - ReservedVehiclesPerDays
   - OrderPayment
   - OrderCancellationFee
   - RefundablePaypalAmount
   - CompanyTreasury
5. Create domain services:
   - OrderCalculationService (with percentage-based ServiceFees calculation)
   - TreasuryService

### Phase 2: Infrastructure Layer
1. Create EF Core mapping configurations for all new entities
2. Update CityConfiguration and VehicleConfiguration
3. Update DatabaseContext with new DbSets
4. Create and test migration
5. Seed initial CompanyTreasury record

### Phase 3: Application Layer - DTOs
1. Create all DTOs:
   - CityFeesDto
   - ReservedDateDto
   - CreateOrderCommandDto
   - OrderDto
   - OrderDetailDto
   - OrderVehicleDto
   - OrderPaymentDto
   - All report DTOs

### Phase 4: Application Layer - Queries
1. Create GetCityFeesQuery and handler
2. Create GetReservedVehiclePerSubCategoryQuery and handler
3. Create GetCustomerOrdersQuery and handler
4. Create GetAllOrdersQuery (with state and orderCode filtering) and handler
5. Create GetOrderByIdQuery and handler
6. Create all report queries and handlers

### Phase 5: Application Layer - Commands
1. Create CreateOrderCommand with:
   - OrderCode generation
   - Total calculation validation (tolerance: 0.50)
   - Date conflict validation
   - Payment method validation
   - Handler and validator
2. Create CancelOrderCommand (shared) with:
   - 4 days policy validation
   - Cancellation fee calculation
   - Refund processing logic
   - Handler and validator
3. Create ConfirmOrderCommand with:
   - Vehicle assignment validation
   - ReservedVehiclesPerDays creation
   - Vehicle status update
   - Handler and validator
4. Create UpdateOrderStateCommand with:
   - State transition validation
   - Treasury update on completion
   - Handler and validator
5. Create UpdatePaymentStateCommand with handler and validator
6. Create ProcessRefundCommand with handler and validator

### Phase 6: Presentation Layer
1. Create CustomerOrderController with all endpoints:
   - GET /api/CustomerOrder/CityFees
   - GET /api/CustomerOrder/ReservedVehiclePerSubCategory
   - POST /api/CustomerOrder
   - GET /api/CustomerOrder/MyOrders
   - POST /api/CustomerOrder/{orderId}/Cancel
2. Create AdminOrderController with all endpoints:
   - GET /api/AdminOrder (with search)
   - GET /api/AdminOrder/{id}
   - POST /api/AdminOrder/{orderId}/Confirm
   - POST /api/AdminOrder/{orderId}/UpdateState
   - POST /api/AdminOrder/{orderId}/Cancel
   - POST /api/AdminOrder/{orderId}/Payment/UpdateState
   - POST /api/AdminOrder/{orderId}/Refund
   - All report endpoints

### Phase 7: Testing & Validation
1. Build backend project
2. Test all endpoints
3. Verify calculations
4. Test state transitions
5. Test cancellation scenarios
6. Test payment flows
7. Test treasury updates

### Phase 8: Frontend - Admin Order Cycle
1. Create order list component with:
   - Filtering by state and orderCode
   - Pagination
   - Order status badges
2. Create order detail component with:
   - Full order information display
   - Vehicle assignment interface
   - Payment status display
3. Create state management:
   - Confirm order button
   - Update state buttons (OnWay, CustomerReceived, Complete)
   - Cancel order button
4. Create reports dashboard:
   - Orders by state chart
   - Revenue charts
   - Treasury balance display
   - Vehicle utilization statistics
5. Create payment management interface
6. Create refund processing interface

## OrderCode Generation Strategy

Generate unique order codes using format: `ORD-{YYYYMMDD}-{RandomString}`

Example: `ORD-20260111-A3B7C9`

Implementation:
- Use DateTime.UtcNow for date part
- Generate random alphanumeric string (6-8 characters)
- Check uniqueness in database before saving
- Retry if duplicate found

## Date Range Expansion Logic

When querying ReservedVehiclesPerDays for calendar display:
1. Get all records where SubCategoryId matches
2. Filter by State = StillBooked
3. Filter by Order.OrderState != Completed && != Cancelled
4. Expand DateFrom to DateTo into individual dates
5. Return unique list of dates

Example:
- DateFrom: 2026-01-01
- DateTo: 2026-01-05
- Returns: [2026-01-01, 2026-01-02, 2026-01-03, 2026-01-04, 2026-01-05]

## ReservedVehiclesPerDays Creation Logic

When admin confirms order:
1. For each assigned vehicle:
2. For each day in date range (DateFrom to DateTo):
3. Create ReservedVehiclesPerDays record:
   - VehicleId
   - SubCategoryId
   - VehicleCode (from Vehicle entity)
   - OrderId
   - DateFrom = current day
   - DateTo = current day
   - State = StillBooked

This creates one record per vehicle per day for accurate tracking and reporting.

## Payment Processing Flow

### Cash Payment Flow
1. Order created → OrderPayment.State = Pending
2. Admin confirms order → OrderPayment.State remains Pending
3. Admin marks payment received → OrderPayment.State = Paid
4. If cancellation fee exists → Admin marks OrderCancellationFee.State = Paid

### PayPal Payment Flow
1. Order created → OrderPayment.State = Pending
2. Process PayPal payment (placeholder) → OrderPayment.State = Paid (on success) or Failed (on failure)
3. Admin confirms order → Verify payment is Paid
4. On cancellation → Create RefundablePaypalAmount, process refund → OrderPayment.State = Refunded

## Treasury Update Flow

### Revenue Addition
1. Order state changes to Completed
2. Check OrderPayment.State = Paid
3. Call TreasuryService.AddOrderRevenue(OrderPayment.Total)
4. Update CompanyTreasury.TotalRevenue
5. Update CompanyTreasury.LastUpdated

### Cancellation Fee Addition
1. OrderCancellationFee.State changes to Paid
2. Call TreasuryService.AddCancellationFee(OrderCancellationFee.Amount)
3. Update CompanyTreasury.TotalCancellationFees
4. Update CompanyTreasury.LastUpdated

## Testing Checklist

### Order Creation Tests
- [ ] Valid order creation with correct total
- [ ] Order creation with total tolerance (0.50)
- [ ] Order creation with total mismatch (> 0.50) - should fail
- [ ] Order creation with date conflict - should fail
- [ ] Order creation with CashBlock customer using Cash - should fail
- [ ] OrderCode generation uniqueness
- [ ] OrderSubTotal calculation (Price * Count)
- [ ] OrderTotal calculation with all fees

### Cancellation Tests
- [ ] Cancel order within 4 days - no fee
- [ ] Cancel order after 4 days - fee applied
- [ ] Cancel order with PayPal - refund created
- [ ] Cancel order with assigned vehicles - vehicles released
- [ ] Cancel order - ReservedVehiclesPerDays marked as Cancelled

### State Transition Tests
- [ ] Pending → Confirmed (with vehicle assignment)
- [ ] Confirmed → OnWay
- [ ] OnWay → CustomerReceived
- [ ] CustomerReceived → Completed (treasury updated)
- [ ] Invalid state transitions - should fail

### Vehicle Assignment Tests
- [ ] Assign available vehicles
- [ ] Assign unavailable vehicle - should fail
- [ ] Assign vehicle from different subcategory - should fail
- [ ] Assign vehicle with date conflict - should fail
- [ ] Vehicle status updated to "Rented" on confirmation
- [ ] Vehicle status updated to "Available" on completion

### Payment Tests
- [ ] Cash payment state updates
- [ ] PayPal payment processing (placeholder)
- [ ] Payment state validation
- [ ] Cancellation fee payment tracking

### Treasury Tests
- [ ] Revenue added on order completion
- [ ] Cancellation fee added when paid
- [ ] Treasury balance calculation
- [ ] Multiple orders completion - cumulative revenue

### Reports Tests
- [ ] Orders by state count
- [ ] Orders by date range
- [ ] Revenue by period
- [ ] Cancellation report
- [ ] Vehicle utilization
- [ ] Customer order history
- [ ] Treasury balance
- [ ] Revenue by period breakdown
- [ ] Cancellation fees collected

## Notes

1. **PayPal Integration**: Structure is ready but actual PayPal API integration is a placeholder. Implement actual PayPal SDK integration when ready.

2. **OrderCode Uniqueness**: Implement retry logic if duplicate OrderCode is generated (should be rare but handle gracefully).

3. **Date Handling**: All dates should be stored in UTC and converted to local time for display.

4. **Base64 Image**: PassportImage is stored as base64 string. Consider file storage system for production.

5. **Treasury Singleton**: CompanyTreasury should have only one record. Implement singleton pattern in service layer.

6. **Concurrency**: Handle concurrent order creation and vehicle assignment with proper locking mechanisms.

7. **Audit Trail**: All state changes should be logged for audit purposes (using existing audit properties).

8. **Performance**: Index ReservedVehiclesPerDays table properly for fast date range queries.

## Summary

This implementation plan provides a complete order cycle system with:
- Customer-facing APIs for order creation and management
- Admin dashboard for order management and vehicle assignment
- Payment processing (Cash and PayPal)
- Cancellation handling with fees
- Refund processing
- Treasury management
- Comprehensive reporting
- Frontend admin interface

All components follow Clean Architecture, CQRS, and DDD principles as established in the project architecture.