using System;
using System.Collections.Generic;

namespace Application.Features.AdminHome.DTOs
{
    public class HomeSummaryDto
    {
        public decimal Revenue { get; set; }
        public decimal RevenueChangePercent { get; set; }
        public int OrdersCount { get; set; }
        public decimal OrdersChangePercent { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int ActiveRentals { get; set; }
        public int PendingOrders { get; set; }
        public int AvailableVehicles { get; set; }
        public int TotalVehicles { get; set; }
        public decimal VehicleUtilizationPercent { get; set; }
        public int NewCustomers { get; set; }
        public decimal NewCustomersChangePercent { get; set; }
        public decimal TreasuryBalance { get; set; }
        public int UnpaidCancellationFeesCount { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }

    public class HomeChartPointDto
    {
        public string Period { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public decimal Value { get; set; }
        public int Count { get; set; }
    }

    public class HomeRevenueTrendDto
    {
        public string Granularity { get; set; } = "day";
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public List<HomeChartPointDto> Series { get; set; } = new();
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }

    public class HomeOrderStateBucketDto
    {
        public int State { get; set; }
        public string StateName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class HomeOrderPipelineDto
    {
        public List<HomeOrderStateBucketDto> ByState { get; set; } = new();
        public int CreatedInRange { get; set; }
        public int CompletedInRange { get; set; }
        public int UrgentInRange { get; set; }
        public int ActiveRentals { get; set; }
        public int PendingOrders { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }

    public class HomeCustomerGrowthDto
    {
        public string Granularity { get; set; } = "day";
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int InactiveCustomers { get; set; }
        public int BlockedCustomers { get; set; }
        public int IndividualCustomers { get; set; }
        public int InstitutionCustomers { get; set; }
        public int NewInRange { get; set; }
        public int CashBlockedCustomers { get; set; }
        public List<HomeChartPointDto> Series { get; set; } = new();
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }

    public class HomePaymentMethodSliceDto
    {
        public int MethodId { get; set; }
        public string MethodName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class HomePaymentsMixDto
    {
        public decimal TotalPaidAmount { get; set; }
        public int PaidCount { get; set; }
        public int PendingCount { get; set; }
        public int FailedCount { get; set; }
        public int RefundedCount { get; set; }
        public decimal RefundedAmount { get; set; }
        public List<HomePaymentMethodSliceDto> Methods { get; set; } = new();
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }

    public class HomeTreasuryMovementPointDto
    {
        public string Period { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Net { get; set; }
    }

    public class HomeTreasurySnapshotDto
    {
        public decimal Balance { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public DateTime? LastUpdated { get; set; }
        public List<HomeTreasuryMovementPointDto> Series { get; set; } = new();
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }

    public class HomeCancellationsDto
    {
        public int CancelledOrders { get; set; }
        public decimal TotalFees { get; set; }
        public decimal PaidFees { get; set; }
        public decimal UnpaidFees { get; set; }
        public List<HomeChartPointDto> Series { get; set; } = new();
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }

    public class HomeTopItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int OrdersCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class HomeTopPerformersDto
    {
        public List<HomeTopItemDto> Categories { get; set; } = new();
        public List<HomeTopItemDto> SubCategories { get; set; } = new();
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }

    public class HomeCityPerformanceItemDto
    {
        public int CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public int OrdersCount { get; set; }
        public decimal Revenue { get; set; }
        public int VehiclesCount { get; set; }
        public int AvailableVehicles { get; set; }
        public int NewCustomers { get; set; }
    }

    public class HomeCityPerformanceDto
    {
        public List<HomeCityPerformanceItemDto> Cities { get; set; } = new();
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }

    public class HomeRecentOrderDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string SubCategoryName { get; set; } = string.Empty;
        public string CityName { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public int OrderState { get; set; }
        public string OrderStateName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class HomeRecentActivityDto
    {
        public List<HomeRecentOrderDto> RecentOrders { get; set; } = new();
    }
}
