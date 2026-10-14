/** One refuelling row, owned by a motorcycle (see `GET /api/fuel-ups`). */
export interface FuelUp {
  id: string;
  motorcycleId: string;
  fueledAt: string;
  odometerKm: number;
  litres: number;
  totalCost: number;
  pricePerLitre: number;
  isFullTank: boolean;
  station?: string | null;
  notes?: string | null;
  createdAt: string;
}

/** Create/update payload for `POST /api/fuel-ups` and `PUT /api/fuel-ups/{id}`. */
export interface SaveFuelUpDTO {
  motorcycleId: string;
  fueledAt: string;
  odometerKm: number;
  litres: number;
  totalCost: number;
  isFullTank: boolean;
  station?: string | null;
  notes?: string | null;
}

/**
 * One full-to-full consumption segment. Partial fills are folded into the
 * segment closed by the next full tank.
 */
export interface FuelConsumptionPoint {
  fueledAt: string;
  odometerKm: number;
  distanceKm: number;
  litres: number;
  cost: number;
  kmPerLitre?: number | null;
}

/** Derived consumption/cost metrics for one motorcycle. */
export interface FuelConsumption {
  motorcycleId: string;
  averageKmPerLitre?: number | null;
  fuelCostPerKm?: number | null;
  costPerKm?: number | null;
  totalFuelCost: number;
  totalMaintenanceCost: number;
  totalCostOfOwnership: number;
  totalLitres: number;
  totalDistanceKm: number;
  latestKmPerLitre?: number | null;
  baselineKmPerLitre?: number | null;
  isConsumptionDrop: boolean;
  points: FuelConsumptionPoint[];
}
