export type ResourceStatus = "Online" | "Maintenance" | "Offline" | "Archived";

export interface Me {
  id: string;
  email: string;
  roles: string[];
}

export interface UserRef {
  userId: string;
  email: string;
}

export interface AppUser {
  id: string;
  fullName: string;
  email: string;
}

export interface UserFormData {
  firstName: string;
  lastName: string;
  email: string;
  password?: string;
  newPassword?: string | null;
}

export interface ResourceRef {
  resourceId: string;
  name: string;
  description: string;
}

export interface Resource {
  id: string;
  name: string;
  description: string;
  resourceTypeId: string;
  createdAt: string;
  status: ResourceStatus;
}

export interface ResourceType {
  id: string;
  name: string;
  createdAt: string;
  maxDurationMinutes: number;
  maxAdvanceDays: number;
}

export interface Booking {
  id: string;
  user: UserRef;
  resource: ResourceRef;
  startsAt: string;
  endsAt: string;
  createdAt: string;
  cancelledAt?: string | null;
}

export interface AvailabilitySlot {
  startUtc: string;
  endUtc: string;
  isAvailable: boolean;
}

export interface AvailabilityResponse {
  resourceId: string;
  slots: AvailabilitySlot[];
  closedForRestOfToday: boolean;
}

export type CurrentOccupancy = {
  totalPercentage: number;
  byResourceType: {
    resourceTypeId: string;
    name: string;
    bookedCount: number;
    totalCount: number;
    percentage: number;
  }[];
};

export type RangeOccupancy = {
  totalPercentage: number;
  byResourceType: {
    resourceTypeId: string;
    name: string;
    bookedHours: number;
    availableHours: number;
    percentage: number;
  }[];
};

export type Sensor = {
  id: string;
  serial: string;
  model: string;
  status: string;
  roomId: string | null;
  roomName: string;
  roomType: string;
};

export interface MeasurementReceived {
  tenantSlug: string;
  deviceId: string;
  type: string;
  value: number;
  time: string;
}

export interface SensorReading {
  temperature?: number;
  co2?: number;
  time?: string;
}
