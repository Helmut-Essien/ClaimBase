/**
 * Client field limits for rates.
 * Must match `RateFieldLimits` and the Shared DTO amount scale.
 */
export const RATE_FIELD_LIMITS = {
  amountScale: 2,
} as const;

/** One hourly teaching rate from `GET /api/rates/teaching`. */
export interface TeachingRate {
  /** Rate id. */
  id: string;
  /** Position title id. */
  positionTitleId: string;
  /** Position title name. */
  positionTitleName: string;
  /** Qualification id. */
  qualificationId: string;
  /** Qualification name. */
  qualificationName: string;
  /** Cedis per hour. */
  amount: number;
  /** First day included (`yyyy-MM-dd`). */
  effectiveFrom: string;
  /** First day excluded. Null keeps the amount in force through later semesters. */
  effectiveTo: string | null;
}

/** Body for `POST /api/rates/teaching`. */
export interface CreateTeachingRateBody {
  /** Shared position title. */
  positionTitleId: string;
  /** Course qualification. */
  qualificationId: string;
  /** Cedis per hour. */
  amount: number;
  /** First day included. */
  effectiveFrom: string;
  /** First day excluded, or null to leave the row open. */
  effectiveTo: string | null;
}

/** One transport amount from `GET /api/rates/transport`. */
export interface TransportRate {
  /** Rate id. */
  id: string;
  /** Cedis paid once per teaching day. */
  amount: number;
  /** First day included (`yyyy-MM-dd`). */
  effectiveFrom: string;
  /** First day excluded. Null keeps the amount in force. */
  effectiveTo: string | null;
}

/** Body for `POST /api/rates/transport`. */
export interface CreateTransportRateBody {
  /** Cedis per teaching day. */
  amount: number;
  /** First day included. */
  effectiveFrom: string;
  /** First day excluded, or null to leave the row open. */
  effectiveTo: string | null;
}
