# Zones & delivery fees

Delivery fee is no longer a city-level amount. It is the sum of **per-vehicle** directed rates: merchant zone → order destination zone. Missing matrix cells are **0**. Same-zone cells are allowed (default **0**).

Zones and groups are **seeded in the database** (including latitude/longitude). There is no admin API to create zones.

## Model

| Table | Role |
| --- | --- |
| `VO_ZoneGroup` | Shared set of zones; a city binds to **one** group |
| `VO_Zone` | Named zone + lat/lng for Haversine sort |
| `VO_ZoneDeliveryRate` | Directed fee `FromZoneId` → `ToZoneId` in that group |

Also:

- `VO_City.ZoneGroupId` (nullable until assigned). **`DeliveryFees` column is removed.**
- `Merchant` / `Delivery` / `Customer` require `ZoneId` in the city’s group.
- `VO_Order.DestinationZoneId` — required; can differ from the customer profile zone.
- `VO_OrderVehicle.DeliveryFee` — snapshot at create / update / fleet recalc.

## Pricing

`Order.CalculatePricing(..., deliveryFees, previousDebt)` takes the **summed** vehicle delivery fees, not `City.DeliveryFees`.

`Order.RecalculateTotals(city, rates, actor)` (Pending / MerchantPending / MerchantConfirmed only) rewrites each `OrderVehicle.DeliveryFee` from merchant zone → `DestinationZoneId`, then reapplies totals.

Assign delivery uses **`OrderVehicle.DeliveryFee`**, not `DeliveryFees / VehiclesCount`.

## APIs

### Lookups

- `GET api/Customer/GetZonesByCity?cityId=` (anonymous) — register / profile.
- `GET api/AdminOrder/ZonesByCity?cityId=`
- `GET api/City/{cityId}/Zones`
- `GET api/City/ZoneGroups`
- `GET api/City/DeliveryMatrix?fromZoneId=` — from-zone → all to-zones (including self), missing = 0.
- `PUT api/City/DeliveryRates` — save the full from-zone row (`FromZoneId` + `Rates[]`).

City add/update: `ZoneGroupId` only (no city delivery fee).

### Orders

Create (mobile + admin) and admin update require `DestinationZoneId` in the city group. Each vehicle gets a fee snapshot.

Admin create-order UI should default destination zone to **customer.ZoneId**.

### Available vehicles (existing endpoints)

`GET api/CustomerOrder/AvailableVehicles` and `GET api/AdminOrder/AvailableVehicles` require `DestinationZoneId`. Results are sorted by Haversine (merchant zone vs destination). Same zone first (distance 0). **No new vehicle-list API.**

### Fees preview

- Mobile: `POST api/CustomerOrder/DeliveryFees` `{ destinationZoneId, vehicleIds }` → `{ deliveryFees }` **total only**.
- Admin: existing `POST api/AdminOrder/CalculateTotals` still returns **per-vehicle** `DeliveryFees` plus order total.

## Migration notes (run yourself)

Do **not** generate migrations from this work. After you migrate:

1. Drop `VO_City.DeliveryFees`.
2. Add zone tables + FKs, `ZoneGroupId`, entity `ZoneId`s, `DestinationZoneId`, `OrderVehicle.DeliveryFee`.
3. Seed groups, zones (with lat/lng), bind each city to a group, backfill `ZoneId` on existing people, backfill order destination + vehicle fees.
4. `dotnet build` Volt.Server so NSwag regenerates the Angular client.

## Frontend (after client generate)

- City form: zone group dropdown, remove city delivery-fee field.
- City rates screen: pick from-zone, edit all to-zone fees (include same zone).
- Register / admin merchant-delivery-customer: zone dropdown from city’s group.
- Order create/update: destination zone (admin default = customer zone); pass `DestinationZoneId` into AvailableVehicles and totals.
