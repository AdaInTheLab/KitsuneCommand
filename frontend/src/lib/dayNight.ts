/**
 * Converts between human-friendly day/night minutes and 7D2D's raw
 * DayNightLength / DayLightLength config values.
 *
 * 7D2D stores:
 *   DayNightLength = real-time minutes per in-game 24h cycle (10–240)
 *   DayLightLength = in-game hours of daylight per day (1–23)
 *
 * Operators think in "40 min day / 20 min night" instead.
 *
 * Ported from the KitsuneDen dashboard's `lib/day-night.ts`.
 */

export interface DayNightInput {
  dayMinutes: number
  nightMinutes: number
}

export interface DayNightConfig {
  DayNightLength: number
  DayLightLength: number
}

const MIN_CYCLE = 10
const MAX_CYCLE = 240
const MIN_DAYLIGHT = 1
const MAX_DAYLIGHT = 23

function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, value))
}

/**
 * Convert friendly day/night minutes into raw 7D2D config values.
 */
export function resolveDayNightConfig({ dayMinutes, nightMinutes }: DayNightInput): DayNightConfig {
  if (dayMinutes <= 0) throw new Error('dayMinutes must be positive')
  if (nightMinutes <= 0) throw new Error('nightMinutes must be positive')

  const total = clamp(dayMinutes + nightMinutes, MIN_CYCLE, MAX_CYCLE)
  const daylightHours = clamp(
    Math.round((dayMinutes / (dayMinutes + nightMinutes)) * 24),
    MIN_DAYLIGHT,
    MAX_DAYLIGHT,
  )

  return { DayNightLength: total, DayLightLength: daylightHours }
}

/**
 * Convert raw 7D2D config values back into friendly day/night minutes.
 */
export function reverseDayNightConfig(dayNightLength: number, dayLightLength: number): DayNightInput {
  const total = clamp(dayNightLength, MIN_CYCLE, MAX_CYCLE)
  const hours = clamp(dayLightLength, MIN_DAYLIGHT, MAX_DAYLIGHT)

  let dayMinutes = Math.round((hours / 24) * total)
  let nightMinutes = total - dayMinutes

  if (dayMinutes <= 0) {
    dayMinutes = 1
    nightMinutes = total - 1
  }
  if (nightMinutes <= 0) {
    nightMinutes = 1
    dayMinutes = total - 1
  }

  return { dayMinutes, nightMinutes }
}
