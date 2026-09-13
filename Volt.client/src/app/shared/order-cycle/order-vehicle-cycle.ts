import { OrderState } from '../../core/services/clientAPI';

export type VehicleLifecycleStep =
  | 'receivedFromOwner'
  | 'deliveredToCustomer'
  | 'receivedFromCustomer'
  | 'deliveredToOwner';

export interface VehicleCycleFlags {
  receivedFromOwner: boolean;
  deliveredToCustomer: boolean;
  receivedFromCustomer: boolean;
  deliveredToOwner: boolean;
  deliveryFailed: boolean;
}

export const VEHICLE_LIFECYCLE_STEPS: Array<{ id: VehicleLifecycleStep; labelKey: string }> = [
  { id: 'receivedFromOwner', labelKey: 'orders.cyclePickup' },
  { id: 'deliveredToCustomer', labelKey: 'orders.cycleDeliveredToCustomer' },
  { id: 'receivedFromCustomer', labelKey: 'orders.cycleReceivedFromCustomer' },
  { id: 'deliveredToOwner', labelKey: 'orders.cycleReturnedToOwner' }
];

export function isStepDone(vehicle: VehicleCycleFlags, step: VehicleLifecycleStep): boolean {
  switch (step) {
    case 'receivedFromOwner':
      return !!vehicle.receivedFromOwner;
    case 'deliveredToCustomer':
      return !!vehicle.deliveredToCustomer;
    case 'receivedFromCustomer':
      return !!vehicle.receivedFromCustomer;
    case 'deliveredToOwner':
      return !!vehicle.deliveredToOwner;
  }
}

export function canMarkReceivedFromOwner(
  orderState: OrderState,
  vehicle: VehicleCycleFlags,
  blocked: boolean
): boolean {
  if (blocked || vehicle.deliveryFailed || vehicle.receivedFromOwner) return false;
  return orderState === OrderState.DeliveryAssigned || orderState === OrderState.OnWay;
}

export function canMarkDeliveredToCustomer(
  orderState: OrderState,
  vehicle: VehicleCycleFlags,
  blocked: boolean
): boolean {
  if (blocked || vehicle.deliveryFailed || !vehicle.receivedFromOwner || vehicle.deliveredToCustomer) {
    return false;
  }
  return orderState === OrderState.OnWay || orderState === OrderState.CustomerReceived;
}

export function canMarkReceivedFromCustomer(
  orderState: OrderState,
  vehicle: VehicleCycleFlags,
  blocked: boolean
): boolean {
  if (blocked || vehicle.deliveryFailed || !vehicle.deliveredToCustomer || vehicle.receivedFromCustomer) {
    return false;
  }
  return orderState === OrderState.OnWay || orderState === OrderState.CustomerReceived;
}

export function canMarkDeliveredToOwner(
  orderState: OrderState,
  vehicle: VehicleCycleFlags,
  blocked: boolean
): boolean {
  if (blocked || vehicle.deliveryFailed || !vehicle.receivedFromCustomer || vehicle.deliveredToOwner) {
    return false;
  }
  return orderState === OrderState.OnWay
    || orderState === OrderState.CustomerReceived
    || orderState === OrderState.Completed;
}

export function canMarkVehicleNotReceived(
  orderState: OrderState,
  vehicle: VehicleCycleFlags,
  blocked: boolean
): boolean {
  if (blocked || vehicle.deliveryFailed || !vehicle.receivedFromOwner || vehicle.deliveredToCustomer) {
    return false;
  }
  return orderState === OrderState.OnWay;
}

export function canMarkOrderNotDelivered(
  orderState: OrderState,
  vehicles: VehicleCycleFlags[],
  orderDeliveryFailed: boolean,
  blocked: boolean
): boolean {
  if (blocked || orderDeliveryFailed || !vehicles.length) return false;
  if (orderState !== OrderState.DeliveryAssigned && orderState !== OrderState.OnWay) return false;
  return vehicles.every(v => v.receivedFromOwner);
}

export function hasAnyReceivedFromOwner(vehicles: VehicleCycleFlags[]): boolean {
  return vehicles.some(v => v.receivedFromOwner);
}

export function nextLifecycleAction(
  orderState: OrderState,
  vehicle: VehicleCycleFlags,
  blocked: boolean
): VehicleLifecycleStep | null {
  if (canMarkReceivedFromOwner(orderState, vehicle, blocked)) return 'receivedFromOwner';
  if (canMarkDeliveredToCustomer(orderState, vehicle, blocked)) return 'deliveredToCustomer';
  if (canMarkReceivedFromCustomer(orderState, vehicle, blocked)) return 'receivedFromCustomer';
  if (canMarkDeliveredToOwner(orderState, vehicle, blocked)) return 'deliveredToOwner';
  return null;
}
